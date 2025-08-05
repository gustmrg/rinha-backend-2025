namespace RinhaBackend.API.DTOs.Responses;

public class PaymentSummaryResponse
{
    public ProcessorSummary Default { get; set; }
    public ProcessorSummary Fallback { get; set; }
}