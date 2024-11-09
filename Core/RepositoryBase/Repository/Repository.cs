using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.RepositoryBase.Connection.Interfaces;
using Core.RepositoryBase.Model;
using Core.RepositoryBase.Repository.Interfaces;
using Dapper;

namespace Core.RepositoryBase.Repository;

/// <inheritdoc cref="IRepository{TDal,TKey}"/>
public class Repository<TDal, TKey> : IRepository<TDal, TKey> where TDal : DalModelBase<TKey>
{
    private readonly IDbConnectionFactory _connectionFactory;

    protected DbConnection Connection => _connectionFactory.GetConnection();
    
    protected Repository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }
    
    /// <inheritdoc />
    public DbTransaction BeginTransaction()
    {
        return _connectionFactory.StartTransactionOrDefault();
    }

    /// <inheritdoc />
    public async Task<TKey> InsertAsync(TDal model, DbTransaction transaction)
    {
        var properties = DalHelper.GetNonIdProperties(model.GetType());
        var quotedProperties = properties.Select(x => $"\"{x}\"");
        var escapedProperties = properties.Select(x => $"{DalHelper.ParameterPrefix}{x}");
        
        var statement = $"INSERT INTO {DalHelper.TbName<TDal>()}( {string.Join(", ", quotedProperties)} ) " +
                        $"VALUES ( {string.Join(", ", escapedProperties)} ) " +
                        $"RETURNING {DalHelper.ColName<TDal>(x => x.Id)}";
        var id = await Connection.QuerySingleAsync<TKey>(statement, model, transaction);

        return id;
    }

    /// <inheritdoc />
    public async Task<TDal> GetAsync(TKey id, DbTransaction transaction)
    {
        var statement =
            $"SELECT * FROM {DalHelper.TbName<TDal>()} " +
            $"WHERE {DalHelper.ColName<TDal>(x => x.Id)} = {DalHelper.ParameterPrefix}{nameof(id)}";
        var result = await Connection.QuerySingleAsync<TDal>(statement, new { id }, transaction);
        
        return result;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(TKey id, DbTransaction transaction)
    {
        var statement =
            $"DELETE FROM {DalHelper.TbName<TDal>()} " +
            $"WHERE {DalHelper.ColName<TDal>(x => x.Id)} = {DalHelper.ParameterPrefix}{nameof(id)}";
        await Connection.ExecuteAsync(statement, new { id }, transaction);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(TDal newModel, DbTransaction transaction)
    {
        var statement = $"UPDATE {DalHelper.TbName<TDal>()} SET {DalHelper.GetFieldPart(typeof(TDal))} " +
                        $"WHERE {DalHelper.ColName<TDal>(x => x.Id, false)} = {DalHelper.ParameterPrefix}Id";

        await Connection.ExecuteAsync(statement, newModel, transaction);
    }

    /// <inheritdoc />
    public async Task<List<TDal>> GetAllAsync(DbTransaction transaction)
    {
        var statement = $"SELECT * FROM {DalHelper.TbName<TDal>()}";
        var result = await Connection.QueryAsync<TDal>(statement, transaction);
        return result.AsList();
    }

    /// <inheritdoc />
    public async Task<List<TDal>> GetByFieldAsync(string fieldName, object fieldValue, DbTransaction transaction)
    {
        var statement =
            $"SELECT * FROM {DalHelper.TbName<TDal>()} WHERE \"{fieldName}\" = {DalHelper.ParameterPrefix}{nameof(fieldValue)}";
        var result = await Connection.QueryAsync<TDal>(statement, new { fieldValue }, transaction);
        return result.AsList();
    }
    
    public async Task InsertManyAsync(List<TDal> insertList, DbTransaction transaction = null)
    {
        if (insertList.Count == 0)
        {
            return;
        }

        var properties = DalHelper.GetNonIdProperties(insertList.First().GetType());
        var quotedProperties = properties.Select(x => $"\"{x}\"");
        var escapedProperties = properties.Select(x => $"{DalHelper.ParameterPrefix}{x}");
        var statement = $"INSERT INTO {DalHelper.TbName<TDal>()}( {string.Join(", ", quotedProperties)} ) " +
                        $"VALUES ( {string.Join(", ", escapedProperties)} ) ";
        await Connection.ExecuteAsync(statement, insertList, transaction);
    }
}