using System.Text.Json;
using Microsoft.AspNetCore.Http.Json;
using RinhaBackend.API.Extensions;
using RinhaBackend.API.Factories;
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
builder.Services.AddMemoryCache();

builder.Services.AddScoped<DefaultPaymentProcessor>();
builder.Services.AddScoped<FallbackPaymentProcessor>();

builder.Services.AddScoped<IPaymentProcessorFactory, PaymentProcessorFactory>();
builder.Services.AddScoped<IProcessorHealthMonitor, ProcessorHealthMonitor>();
builder.Services.AddScoped<IPaymentProcessor, DefaultPaymentProcessor>();
builder.Services.AddScoped<IPaymentProcessor, FallbackPaymentProcessor>();

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
// app.UseRouting();
app.MapControllers();

app.Run();