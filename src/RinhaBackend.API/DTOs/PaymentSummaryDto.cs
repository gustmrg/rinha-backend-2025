namespace RinhaBackend.API.DTOs;

public class PaymentSummaryDto
{
    public int Processor { get; set; }
    public int TotalRequests { get; set; }
    public decimal TotalAmount { get; set; }
}