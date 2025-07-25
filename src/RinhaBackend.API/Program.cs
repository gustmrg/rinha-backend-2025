using RinhaBackend.API.DTOs.Requests;
using RinhaBackend.API.Entities;
using RinhaBackend.API.Enums;
using RinhaBackend.API.Extensions;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services.AddDatabase(configuration);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/payments-summary", () => "Hello World!");

app.MapPost("/payments", (CreatePaymentRequest request) =>
{
    // Validate the request
    
    // Initiate payment and save to database
    var payment = new Payment
    {
        Id = Guid.CreateVersion7(),
        Amount = request.Amount,
        Status = PaymentStatus.Pending,
        CreatedAt = DateTime.UtcNow,
        CorrelationId = request.CorrelationId
    };
    
    // Process the payment through processor
    
    // Update the payment status in the database
    payment.Status = PaymentStatus.Completed;
    
    // Return a response
    return Results.Ok();
});

app.Run();