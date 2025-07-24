namespace RinhaBackend.API.Services;

public class PaymentService
{
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(ILogger<PaymentService> logger)
    {
        _logger = logger;
    }

    public string GetPaymentsSummary()
    {
        _logger.LogInformation("Getting payments summary");
        
        return "Payments Summary";
    }

    public string ProcessPayment()
    {
        _logger.LogInformation("Processing payment");
        
        return "Payment Processed";
    }
}