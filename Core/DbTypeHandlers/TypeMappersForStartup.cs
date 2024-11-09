using Dapper;

namespace Core.DbTypeHandlers;

public static class TypeMappersForStartup
{
    public static void AddTypeMappers()
    {
        SqlMapper.AddTypeHandler(new JObjectHandler());
    }
}