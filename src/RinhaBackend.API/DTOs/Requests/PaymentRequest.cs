namespace RinhaBackend.API.DTOs.Requests;

public record PaymentRequest(Guid CorrelationId, decimal Amount, DateTime RequestedAt);