using System.Data;

namespace RinhaBackend.API.Factories.Interfaces;

public interface IDbConnectionFactory
{
    Task<IDbConnection> CreateConnectionAsync();
    IDbConnection CreateConnection();
}