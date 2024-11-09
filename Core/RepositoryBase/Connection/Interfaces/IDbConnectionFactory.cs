using System;
using System.Data.Common;

namespace Core.RepositoryBase.Connection.Interfaces;

/// <summary>
/// Фибрика вещей связанных с подключением к бд
/// </summary>
public interface IDbConnectionFactory : IDisposable
{
    /// <summary>
    /// Создание подключения
    /// </summary>
    DbConnection GetConnection();

    /// <summary>
    /// Начать транзакцию
    /// </summary>
    DbTransaction StartTransaction();

    DbTransaction StartTransactionOrDefault();
}