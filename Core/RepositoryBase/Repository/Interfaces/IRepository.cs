using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Core.RepositoryBase.Model;

namespace Core.RepositoryBase.Repository.Interfaces;

/// <summary>
/// Итерфейс репозитория
/// </summary>
public interface IRepository<TDal, TKey> where TDal : DalModelBase<TKey>
{
    /// <summary>
    /// Начало транзакции
    /// </summary>
    DbTransaction BeginTransaction();
    
    /// <summary>
    /// Вставка
    /// </summary>
    Task<TKey> InsertAsync(TDal model, DbTransaction transaction);

    /// <summary>
    /// Получить
    /// </summary>
    Task<TDal> GetAsync(TKey id, DbTransaction transaction);

    /// <summary>
    /// Удалить
    /// </summary>
    Task DeleteAsync(TKey id, DbTransaction transaction);

    /// <summary>
    /// Обновить
    /// </summary>
    Task UpdateAsync(TDal newModel, DbTransaction transaction);

    /// <summary>
    /// Получить все
    /// </summary>
    Task<List<TDal>> GetAllAsync(DbTransaction transaction);

    /// <summary>
    /// Получить сущости по полю(равенство)
    /// </summary>
    Task<List<TDal>> GetByFieldAsync(string fieldName, object fieldValue, DbTransaction transaction);

    public Task InsertManyAsync(List<TDal> insertList, DbTransaction transaction = null);
}