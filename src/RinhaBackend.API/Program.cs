using System.Text.Json;
using Microsoft.AspNetCore.Http.Json;
using RinhaBackend.API.DTOs.Requests;
using RinhaBackend.API.Entities;
using RinhaBackend.API.Enums;
using RinhaBackend.API.Extensions;
using RinhaBackend.API.Interfaces;
using RinhaBackend.API.Services;

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

builder.Services.AddHttpClient();

builder.Services.AddScoped<IPaymentProcessor, DefaultPaymentProcessor>();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/payments-summary", () => "Hello World!");

app.MapPost("/payments", async (IPaymentRepository paymentRepository, IPaymentProcessor paymentProcessor, CreatePaymentRequest request) =>
{
    // Validate request
    // Validate if correlation ID already exists
    
    var payment = new Payment
    {
        Id = Guid.CreateVersion7(),
        Amount = request.Amount,
        Status = PaymentStatus.Pending,
        CreatedAt = DateTime.UtcNow,
        CorrelationId = request.CorrelationId
    };
    
    await paymentRepository.CreatePaymentAsync(payment);
    
    if (!await paymentProcessor.IsHealthyAsync())
    {
        return Results.BadRequest();
    }
    
    try
    {
        await paymentProcessor.ProcessPaymentAsync(payment);
        
        payment.Status = PaymentStatus.Completed;
        payment.ProcessedAt = DateTime.UtcNow;
        await paymentRepository.UpdatePaymentAsync(payment);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
    
        payment.Status = PaymentStatus.Failed;
        await paymentRepository.UpdatePaymentAsync(payment);
        
        return Results.Problem("Payment processing failed.");
    }
    
    return Results.Ok();
});

app.Run();