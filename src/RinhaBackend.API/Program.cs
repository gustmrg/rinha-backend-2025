using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using RinhaBackend.API.DTOs.Requests;
using RinhaBackend.API.DTOs.Responses;
using RinhaBackend.API.Entities;
using RinhaBackend.API.Enums;
using RinhaBackend.API.Extensions;
using RinhaBackend.API.Factories;
using RinhaBackend.API.Interfaces;
using RinhaBackend.API.Services;
using RinhaBackend.API.Services.Interfaces;
using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services.AddDatabase(configuration);
builder.Services.AddMemoryCache();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.Services.AddHttpClient<DefaultPaymentProcessor>("DefaultProcessor", client =>
{
    var baseUrl = builder.Configuration["PROCESSOR_DEFAULT_URL"] ?? 
                  builder.Configuration["Processors:Default:BaseUrl"] ?? 
                  throw new InvalidOperationException(
                      "PROCESSOR_DEFAULT_URL environment variable or Processors:Default:BaseUrl in appsettings.json must be set");
    
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<FallbackPaymentProcessor>("FallbackProcessor", client =>
{
    var baseUrl = builder.Configuration["PROCESSOR_FALLBACK_URL"] ?? 
                  builder.Configuration["Processors:Fallback:BaseUrl"] ?? 
                  throw new InvalidOperationException(
                      "PROCESSOR_FALLBACK_URL environment variable or Processors:Fallback:BaseUrl in appsettings.json must be set");
    
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

builder.Services.AddScoped<IPaymentProcessorFactory, PaymentProcessorFactory>();
builder.Services.AddScoped<IPaymentProcessor, DefaultPaymentProcessor>();
builder.Services.AddScoped<IPaymentProcessor, FallbackPaymentProcessor>();
builder.Services.AddScoped<PaymentProcessingService>();
builder.Services.AddScoped<PaymentDuplicateService>();

builder.Services.AddHostedService<PaymentBackgroundService>();

builder.Services.AddOpenApi();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });;

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("payments", async (
    [FromServices] IMemoryCache localCache,
    [FromServices] IBackgroundTaskQueue backgroundTaskQueue,
    [FromServices] IPaymentRepository paymentRepository,
    [FromServices] ILogger<Program> logger,
    [FromBody] CreatePaymentRequest request) =>
{
    var cacheKey = $"payment_corr:{request.CorrelationId}";
    
    if (localCache.TryGetValue(cacheKey, out _))
        return Results.Conflict($"Payment with id {request.CorrelationId} already exists");
    
    localCache.Set(cacheKey, true, TimeSpan.FromMinutes(10));

    var paymentId = Guid.CreateVersion7();

    try
    {
        await backgroundTaskQueue.QueueBackgroundWorkItemAsync(async (token, serviceProvider) =>
        {
            var paymentRepository = serviceProvider.GetRequiredService<IPaymentRepository>();
            var paymentService = serviceProvider.GetRequiredService<PaymentProcessingService>();
            var logger = serviceProvider.GetRequiredService<ILogger<PaymentProcessingService>>();

            try
            {
                var payment = new Payment
                {
                    Id = paymentId,
                    Amount = request.Amount,
                    Status = PaymentStatus.Pending,
                    RequestedAt = DateTime.UtcNow,
                    CorrelationId = request.CorrelationId
                };
                
                await paymentRepository.CreatePaymentAsync(payment);
                
                await paymentService.ProcessPendingPaymentAsync(payment.Id);
                
                logger.LogInformation("Payment {PaymentId} processed successfully for correlation {CorrelationId}", 
                    paymentId, request.CorrelationId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process payment {PaymentId} for correlation {CorrelationId}", 
                    paymentId, request.CorrelationId);
                
                localCache.Remove(cacheKey);
            }
        });
        
        logger.LogInformation("Payment {PaymentId} for correlation {CorrelationId} queued", 
            paymentId, request.CorrelationId);
 
        return Results.Accepted();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to create payment for correlation {CorrelationId}", request.CorrelationId);
        return Results.InternalServerError(new { error = "Failed to create payment" });
    }
});

app.MapGet("/payments-summary", async (
    [FromServices] IPaymentRepository paymentRepository,
    [FromServices] ILogger<Program> logger,
    [FromQuery] DateTime from, DateTime to) =>
{
    var payments = (await paymentRepository.GetPaymentsAsync(from, to)).ToList();
    
    logger.LogInformation("Found {Count} payments. ProcessorNames: {ProcessorNames}", 
        payments.Count, 
        string.Join(", ", payments.Select(p => $"{p.Id}:{p.ProcessorName}").Distinct()));
    
    var defaultPayments = payments.Where(x => x.ProcessorName == PaymentProcessor.Default).ToList();
    var fallbackPayments = payments.Where(x => x.ProcessorName == PaymentProcessor.Fallback).ToList();
    
    logger.LogInformation("Default payments: {DefaultCount}, Fallback payments: {FallbackCount}", 
        defaultPayments.Count, fallbackPayments.Count);
    
    var response = new PaymentsSummaryResponse
    {
        Default = new PaymentSummary(defaultPayments.Count, defaultPayments.Sum(x => x.Amount)),
        Fallback = new PaymentSummary(fallbackPayments.Count, fallbackPayments.Sum(x => x.Amount)),
    };
    
    return Results.Ok(response);
});

app.Run();