using System;
using System.Threading.Tasks;
using Core.Settings.Models;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Core.DatabaseInitialization;

public static class InitializeDatabaseExtensions
{
    public static async Task InitializeDatabaseAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var connectionOptions = scope.ServiceProvider.GetRequiredService<IOptions<DbSettings>>();
        
        var builderString = new NpgsqlConnectionStringBuilder(connectionOptions.Value.ConnectionString);
        var dbName = builderString.Database;
        builderString.Database = null;
        
        var connectionString = builderString.ConnectionString;
        
        var isDbExist = await IsDatabaseExist(connectionString, dbName);

        if (!isDbExist)
        {
            await CreateDatabase(connectionString, dbName);
        }
        
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        try
        {
            runner.MigrateUp();
        }
        catch (MissingMigrationsException e)
        {
            Console.WriteLine(e);
        }
    }

    private static async Task<bool> IsDatabaseExist(string connectionString, string dbName)
    {
        var sql = $"SELECT 'CREATE DATABASE {dbName}' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = '{dbName}')";
        try
        {
            var i = await ExecuteScalarSqlAsync(connectionString, sql);
            if (i != null)
                return false;
            return true;
        }
        catch (Exception e) {
            Console.WriteLine(e.ToString());
            throw;
        }
    }

    private static async Task CreateDatabase(string connectionString, string dbName)
    {
        var sql = $"CREATE DATABASE \"{dbName}\";";
        await ExecuteScalarSqlAsync(connectionString, sql);
    }

    private static async Task<object> ExecuteScalarSqlAsync(string connectionString, string sql)
    {
        var builderString = new NpgsqlConnectionStringBuilder(connectionString);
        await using var conn = new NpgsqlConnection(builderString.ConnectionString);
        await using var command = new NpgsqlCommand(sql, conn);
        conn.Open();
        var i = await command.ExecuteScalarAsync();
        await conn.CloseAsync();
        return i;
    }
}