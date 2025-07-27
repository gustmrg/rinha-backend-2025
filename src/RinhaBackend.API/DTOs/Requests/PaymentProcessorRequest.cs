namespace RinhaBackend.API.DTOs.Requests;

public record PaymentProcessorRequest(Guid CorrelationId, decimal Amount, DateTime RequestedAt);