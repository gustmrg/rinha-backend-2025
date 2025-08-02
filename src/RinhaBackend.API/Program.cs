using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
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

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

// Configure named HttpClients for each payment processor
builder.Services.AddHttpClient<DefaultPaymentProcessor>("DefaultProcessor", client =>
{
    var baseUrl = builder.Configuration["PROCESSOR_DEFAULT_URL"] ?? builder.Configuration["Processors:Default:BaseUrl"];
    if (!string.IsNullOrEmpty(baseUrl))
    {
        client.BaseAddress = new Uri(baseUrl);
    }
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<FallbackPaymentProcessor>("FallbackProcessor", client =>
{
    var baseUrl = builder.Configuration["PROCESSOR_FALLBACK_URL"] ?? builder.Configuration["Processors:Fallback:BaseUrl"];
    if (!string.IsNullOrEmpty(baseUrl))
    {
        client.BaseAddress = new Uri(baseUrl);
    }
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

builder.Services.AddScoped<DefaultPaymentProcessor>();
builder.Services.AddScoped<FallbackPaymentProcessor>();

builder.Services.AddScoped<IPaymentProcessorFactory, PaymentProcessorFactory>();
builder.Services.AddScoped<IProcessorHealthMonitor, ProcessorHealthMonitor>();
builder.Services.AddScoped<IPaymentProcessor, DefaultPaymentProcessor>();
builder.Services.AddScoped<IPaymentProcessor, FallbackPaymentProcessor>();
builder.Services.AddScoped<PaymentProcessingService>();

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
    [FromServices] IBackgroundTaskQueue backgroundTaskQueue,
    [FromServices] PaymentProcessingService paymentProcessingService,
    [FromServices] IPaymentRepository paymentRepository,
    [FromServices] ILogger logger,
    [FromBody] CreatePaymentRequest request) =>
{
    var payment = new Payment
    {
        Id = Guid.CreateVersion7(),
        Amount = request.Amount,
        Status = PaymentStatus.Pending,
        CreatedAt = DateTime.UtcNow,
        CorrelationId = request.CorrelationId
    };

    try
    {
        await paymentRepository.CreatePaymentAsync(payment);
    
        await backgroundTaskQueue.QueueBackgroundWorkItemAsync(async token =>
        {
            await paymentProcessingService.ProcessPendingPaymentAsync(payment.Id);
        });
 
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
    [FromQuery] DateTime from, DateTime to) =>
{
    var payments = await paymentRepository.GetPaymentsAsync(from, to);
    
    var defaultPayments = payments.Where(x => x.ProcessorName == PaymentProcessor.Default).ToList();
    var fallbackPayments = payments.Where(x => x.ProcessorName == PaymentProcessor.Fallback).ToList();
    
    var response = new PaymentsSummaryResponse
    {
        Default = new PaymentSummary(defaultPayments.Count, defaultPayments.Sum(x => x.Amount)),
        Fallback = new PaymentSummary(fallbackPayments.Count, fallbackPayments.Sum(x => x.Amount)),
    };
    
    return Results.Ok(response);
});

app.Run();