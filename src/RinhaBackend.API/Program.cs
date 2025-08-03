using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using RinhaBackend.API.Configurations;
using RinhaBackend.API.DTOs.Requests;
using RinhaBackend.API.Extensions;
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


var app = builder.Build();

app.MapPost("payments", async (
    [FromServices] ICacheService cache,
    [FromBody] CreatePaymentRequest request) =>
{
    if (await cache.ExistsAsync($"payment:{request.CorrelationId}"))
    {
        return Results.Conflict("Payment with this correlation ID already exists.");
    }
    
    await cache.TryAddAsync(
        $"payment:{request.CorrelationId}",
        request,
        TimeSpan.FromMinutes(5));

    return Results.Accepted();
});

app.Run();