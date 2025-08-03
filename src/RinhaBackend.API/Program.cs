using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using RinhaBackend.API.Configurations;
using RinhaBackend.API.DTOs.Requests;
using RinhaBackend.API.Entities;
using RinhaBackend.API.Enums;
using RinhaBackend.API.Extensions;
using RinhaBackend.API.Services;
using RinhaBackend.API.Services.Interfaces;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.WriteIndented = false;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

builder.Services.AddRedis(builder.Configuration);

builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

builder.Services.AddHostedService<PaymentBackgroundService>();

var app = builder.Build();

app.MapPost("payments", async (
    [FromServices] ICacheService cache,
    [FromServices] IBackgroundTaskQueue queue,
    [FromServices] ILogger<Program> logger,
    [FromBody] CreatePaymentRequest request) =>
{
    var cacheKey = $"payment:{request.CorrelationId}";
    
    if (await cache.ExistsAsync(cacheKey))
    {
        return Results.Conflict("Payment with this correlation ID already exists.");
    }
    
    var payment = new Payment
    {
        Id = Guid.CreateVersion7(),
        CorrelationId = request.CorrelationId,
        Amount = request.Amount,
        Status = PaymentStatus.Created,
        RequestedAt = DateTime.UtcNow,
    };

    await cache.TryAddAsync(
        cacheKey,
        payment,
        TimeSpan.FromMinutes(5));

    try
    { 
        await queue.QueueBackgroundWorkItemAsync(async (cancellationToken, serviceProvider) =>
        {
            using var scope = serviceProvider.CreateScope();
            var scopedCache = scope.ServiceProvider.GetRequiredService<ICacheService>();
            var scopedLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            payment.Status = PaymentStatus.Succeeded;

            await cache.RemoveAsync(cacheKey);
            await scopedCache.TryAddAsync(cacheKey, payment, TimeSpan.FromMinutes(5));
            
            scopedLogger.LogInformation("Payment processed successfully for correlation {CorrelationId}", request.CorrelationId);
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to process payment for correlation {CorrelationId}", request.CorrelationId);
                
        await cache.RemoveAsync(cacheKey);
    }

    return Results.Accepted();
});

app.Run();