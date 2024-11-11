using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Microsoft.Extensions.Logging;
using YoutubeExplode.Videos;

namespace DiscordIS.Modules;

public class PlayModule : ModuleBase<SocketCommandContext>
{
    private readonly ILoggerFactory _factory;
    private static readonly ConcurrentDictionary<ulong, AudioSession> _voiceChannels = new();

    public PlayModule(ILoggerFactory factory)
    {
        _factory = factory;
    }

    [Command("play", RunMode = RunMode.Async)]
    public async Task PlayAsync([Remainder] string uriString)
    {
        var vid = VideoId.TryParse(uriString);
        if (!vid.HasValue)
        {
            await ReplyAsync("Ссылка хуйня, давай другую.");
            return;
        }

        await ExecuteWithValidateAsync(
            async guildUser =>
            {
                var voiceId = guildUser.VoiceChannel.Id;
                if (_voiceChannels.TryGetValue(voiceId, out var session))
                {
                    await session.AddToQueueAsync(vid.Value);
                }
                else
                {
                    session = new AudioSession(guildUser.VoiceChannel, Context.Channel,
                        _factory.CreateLogger<AudioSession>());
                    if (!_voiceChannels.TryAdd(voiceId, session))
                    {
                        await session.DisposeAsync();
                        await _voiceChannels[voiceId].AddToQueueAsync(vid.Value);
                    }
                    else
                    {
                        session.OnDisposeAsync += () => Task.FromResult(_voiceChannels.Remove(voiceId, out _));
                        await session.ConnectAsync();
                        await session.AddToQueueAsync(vid.Value);
                        await session.PlayAsync();
                    }
                }
            },
            async () => await ReplyAsync("Ты не войсе, петушок."));
    }

    [Command("skip", RunMode = RunMode.Async)]
    public async Task SkipAsync(int count = 0)
    {
        if (count < 0)
        {
            await ReplyAsync("Кривое количество треков");
            return;
        }

        await ExecuteWithValidateAsync(async guildUser =>
        {
            var voiceId = guildUser.VoiceChannel.Id;
            if (_voiceChannels.TryGetValue(voiceId, out var value)) await value.SkipAsync(count);
        }, async () => await ReplyAsync("Ты не войсе, петушок."));
    }

    [Command("resume", RunMode = RunMode.Async)]
    public async Task ResumeAsync()
    {
        await ExecuteWithValidateAsync(async guildUser =>
        {
            var voiceId = guildUser.VoiceChannel.Id;
            if (_voiceChannels.TryGetValue(voiceId, out var value)) await value.PlayAsync();
        }, async () => await ReplyAsync("Ты не войсе, петушок."));
    }

    [Command("pause", RunMode = RunMode.Async)]
    public async Task PauseAsync()
    {
        await ExecuteWithValidateAsync(async guildUser =>
        {
            var voiceId = guildUser.VoiceChannel.Id;
            if (_voiceChannels.TryGetValue(voiceId, out var value)) await value.PauseAsync();
        }, async () => await ReplyAsync("Ты не войсе, петушок."));
    }

    [Command("stop", RunMode = RunMode.Async)]
    public async Task StopAsync()
    {
        await ExecuteWithValidateAsync(async guildUser =>
        {
            var voiceId = guildUser.VoiceChannel.Id;
            if (_voiceChannels.TryGetValue(voiceId, out var value)) await value.StopAsync();
        }, async () => await ReplyAsync("Ты не войсе, петушок."));
    }

    private async Task ExecuteWithValidateAsync(Func<IGuildUser, Task> success, Func<Task> failure)
    {
        if (Context.User is IGuildUser { VoiceChannel: not null } guildUser)
            await success(guildUser);
        else
            await failure();
    }
}