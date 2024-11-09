using System.Threading.Tasks;
using Core.DatabaseInitialization;
using Core.DbTypeHandlers;
using Core.Settings;
using Microsoft.Extensions.Hosting;

namespace Core;

public static class OneTimeHostExecutingExtensions
{
    /// <summary>
    /// Запустить логику, которую требуется выполнить 1 раз
    /// </summary>
    public static async Task RunOneTimeLogicAsync(this IHost host)
    {
        host.ValidateSettings();
        await host.InitializeDatabaseAsync();
        TypeMappersForStartup.AddTypeMappers();
    }
}