using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using RinhaBackend.API.Configurations;
using RinhaBackend.API.Domain.Entities;
using RinhaBackend.API.Domain.Enums;
using RinhaBackend.API.DTOs.Requests;
using RinhaBackend.API.Extensions;
using RinhaBackend.API.Repositories;
using RinhaBackend.API.Repositories.Interfaces;
using RinhaBackend.API.Services;
using RinhaBackend.API.Services.Interfaces;

var builder = WebApplication.CreateSlimBuilder(args);

builder.WebHost.UseKestrelHttpsConfiguration();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.WriteIndented = false;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

builder.Services.AddHttpClientServices(builder.Configuration);
builder.Services.AddRedis(builder.Configuration);
builder.Services.AddRepositories();
builder.Services.AddServices();
builder.Services.AddBackgroundServices();

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
            var scopedLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            var scopedPaymentProcessingService = scope.ServiceProvider.GetRequiredService<IPaymentProcessingService>();
            
            await scopedPaymentProcessingService.ProcessPayment(payment);
            
            scopedLogger.LogInformation("Payment processed successfully for correlation {CorrelationId}", 
                request.CorrelationId);
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