using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscordIS;

public class MainHostedService : BackgroundService
{
    private readonly IServiceProvider _provider;
    private readonly CredentialSettings _settings;
    private readonly DiscordSocketClient _client;
    private readonly CommandService _commandService;
    
    public MainHostedService(IOptions<CredentialSettings> options, ILogger<MainHostedService> logger, IServiceProvider provider)
    {
        _provider = provider;
        _settings = options.Value;
        _client = new DiscordSocketClient(new DiscordSocketConfig()
        {
            GatewayIntents = GatewayIntents.MessageContent | GatewayIntents.AllUnprivileged
        });
        _client.Log += msg =>
        {
            logger.Log(LogLevel.Trace, msg.Exception, msg.Message);
            return Task.CompletedTask;
        };
        _commandService = new CommandService(new CommandServiceConfig()
            { CaseSensitiveCommands = false, SeparatorChar = ' ' });
        _commandService.AddModulesAsync(GetType().Assembly, _provider).Wait();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _client.MessageReceived += MessageReceived;
        //_client.UserVoiceStateUpdated += HandleChangeVoiceState;
        await _client.LoginAsync(TokenType.Bot, _settings.DiscordToken);
        await _client.StartAsync();
        await Task.Delay(-1, stoppingToken);
    }
    
    //private async Task HandleChangeVoiceState(SocketUser user, SocketVoiceState statePrev, SocketVoiceState stateCurrent)
    //{
    //    statePrev.VoiceChannel.
    //}
    
    private async Task MessageReceived(SocketMessage msg)
    {
        if (msg is not SocketUserMessage userMessage)
        {
            return;
        }   
        
        var argPos = 0;

        if (!(userMessage.HasCharPrefix('!', ref argPos) 
              || userMessage.HasMentionPrefix(_client.CurrentUser, ref argPos))
            || userMessage.Author.IsBot)
        {
            return;
        }
            
        var context = new SocketCommandContext(_client, userMessage);

        var res = await _commandService.ExecuteAsync(context, argPos, _provider);
        
        if (res.ErrorReason == "Unknown command.")
        {
            msg.Channel.SendMessageAsync("Не по сезону шелестишь. Говори понятнее, чухан");
        }
    }
}