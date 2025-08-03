using RinhaBackend.API.Enums;

namespace RinhaBackend.API.Entities;

public class Payment
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTime RequestedAt { get; set; }
    public Guid CorrelationId { get; set; }
    public PaymentProcessor PaymentProcessor { get; set; }
}