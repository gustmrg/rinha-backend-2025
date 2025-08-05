using RinhaBackend.API.Domain.Enums;

namespace RinhaBackend.API.Domain.Results;

public class PaymentProcessingResult
{
    public bool IsSuccess { get; set; }
    public PaymentStatus Status { get; set; }
    public PaymentProcessor? Processor { get; set; }
    public string? ErrorMessage { get; set; }
    
    public static PaymentProcessingResult Success(PaymentStatus status, PaymentProcessor processor)
        => new() { IsSuccess = true, Status = status, Processor = processor };
        
    public static PaymentProcessingResult Failure(string errorMessage)
        => new() { IsSuccess = false, Status = PaymentStatus.Failed, ErrorMessage = errorMessage };
}