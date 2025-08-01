using System.Text.Json;
using Microsoft.AspNetCore.Http.Json;
using RinhaBackend.API.Extensions;
using RinhaBackend.API.Factories;
using RinhaBackend.API.Interfaces;
using RinhaBackend.API.Services;
using RinhaBackend.API.Services.Interfaces;
using StackExchange.Redis;

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

builder.Services.AddMemoryCache();

var redisConnectionString = builder.Configuration["REDIS_CONNECTION_STRING"] 
                            ?? builder.Configuration.GetConnectionString("Redis") 
                            ?? "localhost:6379";

builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
{
    var configuration = ConfigurationOptions.Parse(redisConnectionString);
    configuration.AbortOnConnectFail = false;
    configuration.ConnectTimeout = 5000;  
    configuration.SyncTimeout = 1000;
    return ConnectionMultiplexer.Connect(configuration);
});


builder.Services.AddSingleton<ICacheService, RedisCacheService>();
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

app.MapControllers();

app.Run();