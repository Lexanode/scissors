using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Core.Settings.OptionsStartup;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YoutubeExplode;
using YoutubeExplode.Videos;

namespace DiscordIS;

public class Program
{
    public static async Task Main(string[] args)
    {
        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux()) throw new PlatformNotSupportedException();
        var hostBuilder = Host.CreateDefaultBuilder().ConfigureDefaults(args)
#if DEBUG
            .ConfigureAppConfiguration(x => x.AddJsonFile("appsettings.Development.json"))
#endif
            .ConfigureServices(x =>
            {
                x.AddSettings<CredentialSettings>();
                x.AddHostedService<MainHostedService>();
                x.AddLogging(y => y.ClearProviders().AddSimpleConsole(f =>
                    {
                        f.TimestampFormat = "[yyyy-MM-dd HH:mm:ss]";
                        f.SingleLine = true;
                    }));
            });
        var host = hostBuilder.Build();
        await host.RunAsync();
    }
}