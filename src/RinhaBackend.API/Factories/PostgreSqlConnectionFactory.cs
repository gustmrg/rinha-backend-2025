using System.Data;
using Npgsql;
using RinhaBackend.API.Factories.Interfaces;

namespace RinhaBackend.API.Factories;

public class PostgreSqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public PostgreSqlConnectionFactory(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PostgreSqlConnection");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            var host = configuration["DB_HOST"] ?? throw new InvalidOperationException("DB_HOST environment variable is required when connection string is not configured.");
            var port = configuration["DB_PORT"] ?? throw new InvalidOperationException("DB_PORT environment variable is required when connection string is not configured.");
            var database = configuration["DB_NAME"] ?? throw new InvalidOperationException("DB_NAME environment variable is required when connection string is not configured.");
            var username = configuration["DB_USER"] ?? throw new InvalidOperationException("DB_USER environment variable is required when connection string is not configured.");
            var password = configuration["DB_PASSWORD"] ?? throw new InvalidOperationException("DB_PASSWORD environment variable is required when connection string is not configured.");
            
            connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password}";
        }
        
        // Configure connection pool parameters from environment variables
        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Pooling = true,
            MaxPoolSize = int.Parse(configuration["DB_MAX_POOL_SIZE"] ?? "15"),
            MinPoolSize = int.Parse(configuration["DB_MIN_POOL_SIZE"] ?? "2"),
            Timeout = int.Parse(configuration["DB_CONNECTION_TIMEOUT"] ?? "30"),
            CommandTimeout = int.Parse(configuration["DB_COMMAND_TIMEOUT"] ?? "30"),
            ConnectionIdleLifetime = int.Parse(configuration["DB_CONNECTION_IDLE_LIFETIME"] ?? "300"),
            NoResetOnClose = true,
            ApplicationName = "RinhaBackend"
        };
        
        _connectionString = builder.ToString();    
    }
    
    public async Task<IDbConnection> CreateConnectionAsync()
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }

    public IDbConnection CreateConnection()
    {
        var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        return connection;
    }
}