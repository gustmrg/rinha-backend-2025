using RinhaBackend.API.Factories;
using RinhaBackend.API.Interfaces;
using RinhaBackend.API.Repositories;

namespace RinhaBackend.API.Extensions;

public static class DatabaseServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IDbConnectionFactory, PostgreSqlConnectionFactory>();

        services.AddScoped<IPaymentRepository, PaymentRepository>();

        return services;
    }
}