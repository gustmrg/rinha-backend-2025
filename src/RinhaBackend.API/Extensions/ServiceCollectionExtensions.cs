using RinhaBackend.API.Services;
using RinhaBackend.API.Services.Interfaces;
using StackExchange.Redis;

namespace RinhaBackend.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var connectionString = configuration["REDIS_CONNECTION_STRING"] ?? 
                                   configuration.GetConnectionString("Redis") ?? 
                                   throw new InvalidOperationException("Redis connection string is not configured.");
            return ConnectionMultiplexer.Connect(connectionString);
        });

        services.AddScoped<IDatabase>(provider =>
        {
            var multiplexer = provider.GetRequiredService<IConnectionMultiplexer>();
            return multiplexer.GetDatabase();
        });

        services.AddScoped<ICacheService, RedisCacheService>();

        return services;
    }
}