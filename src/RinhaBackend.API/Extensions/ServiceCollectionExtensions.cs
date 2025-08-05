using RinhaBackend.API.Repositories;
using RinhaBackend.API.Repositories.Interfaces;
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
    
    public static IServiceCollection AddHttpClientServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<DefaultPaymentClient>(client =>
        {
            var baseUrl = configuration["PROCESSOR_DEFAULT_URL"] ?? 
                          configuration["Processors:Default:BaseUrl"] ?? 
                          throw new InvalidOperationException(
                              "PROCESSOR_DEFAULT_URL environment variable or Processors:Default:BaseUrl must be set");
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromMilliseconds(500);
        });

        services.AddHttpClient<FallbackPaymentClient>(client =>
        {
            var baseUrl = configuration["PROCESSOR_FALLBACK_URL"] ?? 
                          configuration["Processors:Fallback:BaseUrl"] ?? 
                          throw new InvalidOperationException(
                              "PROCESSOR_FALLBACK_URL environment variable or Processors:Fallback:BaseUrl must be set");
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromMilliseconds(500);
        });

        return services;
    }
    
    public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
    {
        services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
        
        services.AddHostedService<PaymentBackgroundService>();

        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IPaymentRepository, PaymentRepository>();

        return services;
    }
    
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IPaymentProcessingService, PaymentProcessingService>();

        return services;
    }
}