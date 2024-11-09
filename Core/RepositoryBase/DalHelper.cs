using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Core.RepositoryBase.Model;

namespace Core.RepositoryBase;

public static class DalHelper
{
    private static readonly ConcurrentDictionary<Type, string> _cachedSetParts = new();

    public static string ParameterPrefix => "@";

    public static string TbName<T>()
    {
        return $"\"{typeof(T).Name.ToLower()}\"";
    }

    public static string ColName<T>(Expression<Func<T, object>> col, bool isAddTableName = true)
    {
        string colName;
        if (col.Body is MemberExpression exp)
        {
            colName = $"\"{exp.Member.Name}\"";
        }
        else if(col.Body is UnaryExpression unaryExpression)
        {
            colName = $"\"{((MemberExpression)unaryExpression.Operand).Member.Name}\"";
        }
        else
        {
            throw new ArgumentException();
        }

        if (isAddTableName)
        {
            return $"{TbName<T>()}.{colName}";
        }

        return colName;
    }

    public static string GetFieldPart(Type dal)
    {
        if (_cachedSetParts.ContainsKey(dal))
        {
            return _cachedSetParts[dal];
        }

        var properties = GetNonIdProperties(dal);
        properties = properties.Select(x => $"\"{x}\" = {ParameterPrefix}{x}").ToList();
        var setPart = $" {string.Join(", ", properties)} ";
        _cachedSetParts[dal] = setPart;
        return setPart;
    }

    public static List<string> GetNonIdProperties(Type dal)
    {
        var properties = dal.GetProperties()
            .Where(x => x.Name != nameof(DalModelBase<object>.Id)).Select(x => x.Name);

        return properties.ToList();
    }
}