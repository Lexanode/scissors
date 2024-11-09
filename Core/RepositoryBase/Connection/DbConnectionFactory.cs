using System;
using System.Data.Common;
using Core.RepositoryBase.Connection.Interfaces;
using Core.Settings.Models;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Core.RepositoryBase.Connection;

/// <inheritdoc cref="IDbConnectionFactory"/>
public class DbConnectionFactory : IDbConnectionFactory
{
    private DbConnection _connection;

    private DbTransaction _transaction;
    
    private readonly DbSettings _dbSettings;
    
    public DbConnectionFactory(IOptions<DbSettings> dbSettings)
    {
        _dbSettings = dbSettings.Value;
    }
    
    /// <inheritdoc />
    public DbConnection GetConnection()
    {
        if (_connection != null)
        {
            return _connection;
        }

        _connection = new NpgsqlConnection(_dbSettings.ConnectionString);
        _connection.Open();
        
        return _connection;
    }

    /// <inheritdoc />
    public DbTransaction StartTransaction()
    {
        if (_connection == null)
        {
            GetConnection();
        }
        
        return _connection.BeginTransaction();
    }

    public DbTransaction StartTransactionOrDefault()
    {
        if (_transaction != null)
        {
            try
            {
                var tr = _transaction.IsolationLevel;
                return _transaction;
            }
            catch (Exception e)
            {
                _transaction = StartTransaction();
                return _transaction;
            }
        }

        _transaction = StartTransaction();
        return _transaction;
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
        _transaction?.Dispose();
    }
}