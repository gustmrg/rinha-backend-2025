using Dapper;
using RinhaBackend.API.Domain.Enums;

namespace RinhaBackend.API.Domain.Aggregates;

public class PaymentSummary
{
    public PaymentProcessor Processor { get; init; }
    public int TotalRequests { get; init; }
    public decimal TotalAmount { get; init; }
}