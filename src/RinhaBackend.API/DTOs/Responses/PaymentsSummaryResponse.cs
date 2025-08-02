namespace RinhaBackend.API.DTOs.Responses;

public class PaymentsSummaryResponse
{
    public PaymentSummary Default { get; set; }
    public PaymentSummary Fallback { get; set; }
}

public class PaymentSummary
{
    public int TotalRequests { get; }
    public decimal TotalAmount { get; }
    
    public PaymentSummary(int totalRequests, decimal totalAmount)
    {
        TotalRequests = totalRequests;
        TotalAmount = totalAmount;
    }
}