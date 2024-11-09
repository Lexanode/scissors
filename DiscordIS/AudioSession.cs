using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using YoutubeExplode;
using YoutubeExplode.Exceptions;
using YoutubeExplode.Videos;

namespace DiscordIS;

public class AudioSession : IAsyncDisposable
{
    
    private enum State
    {
        Stopped,
        Playing,
        Disconnected,
        QueueEnd,
        Paused,
    }
    
    private State _botState;
    
    private CancellationTokenSource _leaveCts;
    private CancellationTokenSource _pauseCts;
    private Process _ffmpeg;
    
    private readonly ConcurrentQueue<VideoId> _videoQueue = new();

    private readonly ISocketMessageChannel _responseChannel;
    private readonly ILogger<AudioSession> _logger;
    private readonly IVoiceChannel _voiceChannel;
    
    private readonly YoutubeClient _youtubeClient;

    private AudioOutStream _outStream;
    private Stream _youtubeStream;

    private IAudioClient _audioClient;
    
    private readonly SemaphoreSlim _sem = new(1, 1);

    private CancellationTokenSource _cts;

    private bool IsDisposed { get; set; }
    
    public AudioSession(IVoiceChannel voiceChannel, ISocketMessageChannel responseChannel, ILogger<AudioSession> logger)
    {
        _voiceChannel = voiceChannel;
        _responseChannel = responseChannel;
        _logger = logger;
        _youtubeClient = new YoutubeClient();
        _botState = State.Disconnected;
    }
    
    public async Task ConnectAsync()
    {
        if (IsDisposed || _botState != State.Disconnected)
        {
            return;
        }
        await _sem.WaitAsync();
        try
        {
            _logger.LogInformation($"Start connecting to {_voiceChannel.Id}");
            _audioClient = await _voiceChannel.ConnectAsync();
            _audioClient.Disconnected += async ex =>
            {
                _logger.LogError(ex, $"Disconnected from {_voiceChannel.Id}");
                await _responseChannel.SendMessageAsync("Меня выкинуло, идите нахуй, я обиделся");
                await DisposeAsync();
            };
            _botState = State.Stopped;
            _logger.LogInformation($"Connected successfully in {_voiceChannel.Id}");
        }
        catch(Exception e)
        {
            _logger.LogError(e, $"Error while connecting to {_voiceChannel.Id}");
        }
        finally
        {
            _sem.Release();
        }
    }

    public async Task PlayAsync()
    {
        if(IsDisposed || _botState == State.Disconnected)
        {
            return;
        }
        
        await _sem.WaitAsync();
        try
        {
            if (_botState == State.Stopped || _botState == State.Paused || _botState == State.Stopped)
            {
                _logger.LogInformation($"Try change state to playing in {_voiceChannel.Id}");
                await TryStartPlaybackAsync();
                _logger.LogInformation($"State changed in {_voiceChannel.Id}");
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error while changing playback state in {_voiceChannel.Id}");
        }
        finally
        {
            _sem.Release();
        }
    }

    public async Task AddToQueueAsync(VideoId videoId)
    {
        if (IsDisposed)
        {
            throw new ObjectDisposedException("Диспоузед");
        }
        await _sem.WaitAsync();
        try
        {
            _logger.LogInformation($"Enqueue video {videoId} in {_voiceChannel.Id}");
            _videoQueue.Enqueue(videoId);
            await _responseChannel.SendMessageAsync("Track enqueued");
            if (_leaveCts is { IsCancellationRequested: false })
            {
                await _leaveCts.CancelAsync();
            }

            if (_botState == State.QueueEnd || _botState == State.Disconnected)
            {
                _logger.LogInformation($"Try change state to playing in {_voiceChannel.Id}");
                await TryStartPlaybackAsync();
                _logger.LogInformation($"State changed in {_voiceChannel.Id}");
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error while enqueue {_voiceChannel.Id}");
        }
        finally
        {
            _sem.Release();
        }
    }
    
    public async Task SkipAsync(int count = 0)
    {
        if (IsDisposed)
        {
            throw new ObjectDisposedException("Диспоузед");
        }
        
        if (count < 0)
        {
            throw new ArgumentException();
        }
        
        await _sem.WaitAsync();
        try
        {
            _logger.LogInformation($"Skipping {count} tracks in {_voiceChannel.Id}");
            while (count > 0)
            {
                _videoQueue.TryDequeue(out _);
                count -= 1;
            }

            if (_botState == State.Playing)
            {
                if (_cts != null)
                {
                    _logger.LogInformation($"Stop current track in {_voiceChannel.Id}");
                    await _cts.CancelAsync();
                    _cts.Dispose();
                }
                
                await _responseChannel.SendMessageAsync($"Skipped {count}");
                switch (_botState)
                {
                    case State.Playing:
                        _logger.LogInformation($"Try change state to playing in {_voiceChannel.Id}");
                        await TryStartPlaybackAsync();
                        _logger.LogInformation($"State changed in {_voiceChannel.Id}");
                        break;
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error while skip {_voiceChannel.Id}");
        }
        finally
        {
            _sem.Release();
        }
    }

    public async Task PauseAsync()
    {
        if (IsDisposed)
        {
            return;
        }
        await _sem.WaitAsync();
        try
        {
            if (_botState == State.Playing)
            {
                _logger.LogInformation($"Change state to paused in {_voiceChannel.Id}");
                _botState = State.Paused;
                await _pauseCts.CancelAsync();
                await SetupLeaveAsync();
                await _responseChannel.SendMessageAsync("Paused");
                _logger.LogInformation($"State changed in {_voiceChannel.Id}");
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error while pausing {_voiceChannel.Id}");
        }
        finally
        {
            _sem.Release();
        }
    }
    
    public async Task StopAsync()
    {
        if (IsDisposed)
        {
            return;
        }
        await _sem.WaitAsync();
        try
        {
            if (_botState is State.Disconnected or State.Stopped)
            {
                return;
            }

            _logger.LogInformation($"Stop session in {_voiceChannel.Id}");
            if (_cts != null)
            {
                
                await _cts.CancelAsync();
                _cts.Dispose();
            }

            if (_pauseCts != null)
            {
                await _pauseCts.CancelAsync();
                _pauseCts.Dispose();
            }
            
            await _responseChannel.SendMessageAsync("Playback stopped");
            await SetupLeaveAsync();
            _botState = State.Stopped;
            _logger.LogInformation($"Playback stopped in {_voiceChannel.Id}");
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error while stopping {_voiceChannel.Id}");
        }
        finally
        {
            _sem.Release();
        }
    }
    
    /// <summary>
    /// Не запускать без семафора
    /// </summary>
    private async Task TryStartPlaybackAsync()
    {
        while (true)
        {
            using var scope = _logger.BeginScope($"Try start playback in {_voiceChannel} {{0}}");
            if (_videoQueue.IsEmpty && (_cts is null || _cts.IsCancellationRequested))
            {
                _logger.LogInformation("queue empty");
                await _responseChannel.SendMessageAsync("Queue empty");
                await SetupLeaveAsync();
                _botState = State.QueueEnd;
                return;
            }

            if (_leaveCts is { IsCancellationRequested: false })
            {
                _logger.LogInformation("cancel leave");
                try
                {
                    await _leaveCts.CancelAsync();
                    _leaveCts.Dispose();
                }
                catch (ObjectDisposedException) { }
            }

            //TODO: play stop add resume crash
            VideoId videoDataRecord = default;
            if (!(_pauseCts?.IsCancellationRequested ?? false)
                && !_videoQueue.TryDequeue(out videoDataRecord))
            {
                continue;
            }
            
            if (_outStream is null || !_outStream.CanWrite)
            {
                _logger.LogInformation("creating pcm stream");
                _outStream = _audioClient.CreatePCMStream(AudioApplication.Music, 48000);
            }
            
            if (_cts == null || _cts.IsCancellationRequested)
            {
                _logger.LogInformation("cancelling playback session");
                _cts?.Dispose();
                _cts = new CancellationTokenSource();
                
                try
                {
                    _logger.LogInformation("try get manifest via explode");
                    var manifest = await _youtubeClient.Videos.Streams.GetManifestAsync(videoDataRecord);
                    var streamInfo = manifest.GetAudioStreams()
                        .MinBy(x => x.Bitrate.BitsPerSecond);
                    if (streamInfo == null)
                    {
                        continue;
                    }
                    _youtubeStream = await _youtubeClient.Videos.Streams.GetAsync(streamInfo, _cts.Token);
                
                    _logger.LogInformation($"Stream info len:{_youtubeStream.Length} pos:{_youtubeStream.Position}");
                }
                catch (VideoUnplayableException e) when (e.Message.Contains("Please sign in"))
                {
                    _logger.LogInformation("getting failed create stream through dlp");
                    var root = OperatingSystem.IsWindows() ? "yt-dlp.exe" : "yt-dlp_linux";
                    var processStart = new ProcessStartInfo(root)
                    {
                        Arguments = $"-o - --username=oauth --password=\"\" -q \"https://www.youtube.com/watch?v={videoDataRecord}\"",
                        RedirectStandardOutput = true,
                    };
                    var proc = Process.Start(processStart);
                    proc.Start();
                    _youtubeStream = proc.StandardOutput.BaseStream;
                    _cts.Token.Register(() => proc.Kill(true));
                }
                _logger.LogInformation("starting conversion in ffmpeg");

                _ffmpeg = Process.Start(new ProcessStartInfo(OperatingSystem.IsWindows() ? "ffmpeg.exe" : "/usr/bin/ffmpeg")
                {
                    Arguments = "-hide_banner -loglevel panic -f mp4 -i pipe: -ac 2 -f s16le -ar 48000 pipe:1",
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                });
                _cts.Token.Register(() =>
                {
                    if (_ffmpeg != null)
                    {
                        _ffmpeg.Kill();
                        _ffmpeg.Dispose();
                    }
                });
                _youtubeStream.CopyToAsync(_ffmpeg.StandardInput.BaseStream, _cts.Token).ContinueWith(t =>
                {
                    try
                    {
                        if (_cts is { IsCancellationRequested: false })
                        {
                            _cts.Cancel();
                        }
                    }
                    catch (ObjectDisposedException) { }
                    
                    _ffmpeg.Dispose();
                    return TryStartPlaybackAsync();
                }, TaskContinuationOptions.NotOnCanceled);
                
                Task.Run(async () =>
                {
                    while (true)
                    {
                        var str = await _ffmpeg.StandardError.ReadLineAsync(_cts.Token);
                        _logger.LogError(str);
                    }
                });
            }
            
            var isResume = true;
            if (_pauseCts != null)
            {
                try
                {
                    _pauseCts.Dispose();
                    isResume = false;
                }
                catch (ObjectDisposedException) { }
            }
            else
            {
                isResume = false;
            }

            _pauseCts = new CancellationTokenSource();
            _ffmpeg.StandardOutput.BaseStream.CopyToAsync(_outStream, _pauseCts.Token);

            _botState = State.Playing;
            if (isResume)
            {
                await _responseChannel.SendMessageAsync("Resumed");
            }
            else
            {
                await _responseChannel.SendMessageAsync($"""
                                                         Playing: https://www.youtube.com/watch?v={videoDataRecord}
                                                         """);
            }
            return;
        }
    }

    private async Task SetupLeaveAsync()
    {
        const int leaveInterval = 2;
        _logger.LogInformation($"Setup leave in {_voiceChannel.Id} ");
        _leaveCts?.Cancel();
        _leaveCts = new CancellationTokenSource();
        Task.Delay(TimeSpan.FromMinutes(leaveInterval), _leaveCts.Token).ContinueWith(x =>
        {
            if (!x.IsCompletedSuccessfully)
            {
                return;
            }
            _responseChannel.SendMessageAsync("Bye").Wait();
            DisposeAsync().GetAwaiter().GetResult();
        });
        _logger.LogInformation($"Leave task was set up {_voiceChannel.Id}");
    }
    
    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation($"Disposing {_voiceChannel.Id}");
        
        if (IsDisposed)
        {
            return;
        }
        
        await _sem.WaitAsync();
        IsDisposed = true;
        _sem.Release();
        
        if (_cts != null)
        {
            try
            {
                await _cts.CancelAsync();
                _cts.Dispose();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        if (_audioClient != null)
        {
            await _audioClient.StopAsync();
            _audioClient.Dispose();
            _audioClient = null;
        }

        if (_voiceChannel != null)
        {
            await _voiceChannel.DisconnectAsync();
        }

        if (_pauseCts != null)
        {
            try
            {
                await _pauseCts.CancelAsync();
                _pauseCts.Dispose();
            }
            catch (ObjectDisposedException) { }
        }
        
        _leaveCts?.Dispose();
        if (_youtubeStream != null)
        {
            await _youtubeStream.DisposeAsync();
        }

        if (_outStream != null)
        {
            await _outStream.DisposeAsync();
        }

        if (_ffmpeg != null)
        {
            try
            {
                _ffmpeg.Kill();
                _ffmpeg.Dispose();
            }
            catch { }
        }
        _sem.Dispose();
        
        if (OnDisposeAsync != null)
        {
            await OnDisposeAsync();
        }
    }

    public event Func<Task> OnDisposeAsync;
}