namespace RinhaBackend.API.DTOs.Requests;

public record CreatePaymentRequest(Guid CorrelationId, decimal Amount);