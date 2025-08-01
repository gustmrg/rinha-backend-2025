namespace RinhaBackend.API.DTOs.Responses;

public record ProcessorHealthCheckResponse(bool Failing, int MinResponseTime);