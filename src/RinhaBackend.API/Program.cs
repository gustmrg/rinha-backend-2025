using RinhaBackend.API.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/payments-summary", () => "Hello World!");

app.MapPost("/payments", (CreatePaymentRequest request) => Results.Ok());

app.Run();