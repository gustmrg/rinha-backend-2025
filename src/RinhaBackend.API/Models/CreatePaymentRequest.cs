namespace RinhaBackend.API.Models;

public record CreatePaymentRequest(Guid CorrelationId, decimal Amount);