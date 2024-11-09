using Core.Migrations;
using Core.RepositoryBase.Connection;
using Core.RepositoryBase.Connection.Interfaces;
using Core.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Core;

/// <summary>
/// Добавление кора
/// </summary>
public static class CoreForStartup
{
    public static IServiceCollection AddCore(this IServiceCollection collection,
         IConfiguration configuration)
    {
        //collection.AddControllers().AddNewtonsoftJson();
        //collection.AddSignalR();
        collection.AddSwaggerGen();
        
        //collection.AddCoreSettings();

        collection.AddScoped<IDbConnectionFactory, DbConnectionFactory>();
        collection.AddMigrationRunner(configuration);
        collection.AddLogging(x => 
            x.ClearProviders().AddSimpleConsole(f =>
            {
                f.TimestampFormat = "[yyyy-MM-dd HH:mm:ss]";
                f.SingleLine = true;
            }));
        
        return collection;
    }
}