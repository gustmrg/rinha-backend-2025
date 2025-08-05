using RinhaBackend.API.Domain.Enums;

namespace RinhaBackend.API.Domain.Aggregates;

public class PaymentSummary
{
    public PaymentProcessor Processor { get; set; }
    public int TotalRequests { get; set; }
    public decimal TotalAmount { get; set; }
}