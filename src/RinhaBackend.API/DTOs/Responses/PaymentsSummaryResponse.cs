namespace RinhaBackend.API.DTOs.Responses;

public class PaymentsSummaryResponse
{
    public PaymentSummary Default { get; set; }
    public PaymentSummary Fallback { get; set; }
}

public class PaymentSummary(int TotalRequests, decimal TotalAmount);