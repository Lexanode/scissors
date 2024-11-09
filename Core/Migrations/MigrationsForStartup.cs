using System;
using FluentMigrator.Runner;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Migrations;

public static class MigrationsForStartup
{
    public static IServiceCollection AddMigrationRunner(this IServiceCollection collection,
         IConfiguration configuration)
    {
        var section = configuration.GetSection("DatabaseSettings");
        
        collection.AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(section.GetSection("ConnectionString").Value)
                .ScanIn(AppDomain.CurrentDomain.GetAssemblies()).For.Migrations())
            .AddLogging(lb => lb.AddFluentMigratorConsole());

        return collection;
    }
}