using System.Data;
using Dapper;
using Newtonsoft.Json.Linq;

namespace Core.DbTypeHandlers;

public class JObjectHandler : SqlMapper.TypeHandler<JObject>
{
    public override void SetValue(IDbDataParameter parameter, JObject value)
    {
        parameter.Value = value.ToString();
    }

    public override JObject Parse(object value)
    {
        return JObject.Parse(value.ToString() ?? "{}");
    }
}