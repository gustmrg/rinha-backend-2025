using System.Data;
using Dapper;
using Npgsql;

namespace RinhaBackend.API.Configurations;

public class NpgsqlEnumTypeHandler<T> : SqlMapper.TypeHandler<T> where T : struct, Enum
{
    public override void SetValue(IDbDataParameter parameter, T value)
    {
        if (parameter is NpgsqlParameter npgsqlParam)
        {
            npgsqlParam.Value = Convert.ToInt32(value);
            npgsqlParam.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Integer;
        }
        else
        {
            parameter.Value = Convert.ToInt32(value);
        }
    }

    public override T Parse(object value)
    {
        return (T)Enum.ToObject(typeof(T), value);
    }
}

public class NpgsqlNullableEnumTypeHandler<T> : SqlMapper.TypeHandler<T?> where T : struct, Enum
{
    public override void SetValue(IDbDataParameter parameter, T? value)
    {
        if (parameter is NpgsqlParameter npgsqlParam)
        {
            if (value.HasValue)
            {
                npgsqlParam.Value = Convert.ToInt32(value.Value);
                npgsqlParam.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Integer;
            }
            else
            {
                npgsqlParam.Value = DBNull.Value;
            }
        }
        else
        {
            parameter.Value = value.HasValue ? Convert.ToInt32(value.Value) : DBNull.Value;
        }
    }

    public override T? Parse(object value)
    {
        if (value == null || value == DBNull.Value)
            return null;
        
        return (T)Enum.ToObject(typeof(T), value);
    }
}