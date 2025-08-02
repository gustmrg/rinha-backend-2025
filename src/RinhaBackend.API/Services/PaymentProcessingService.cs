using RinhaBackend.API.Entities;
using RinhaBackend.API.Enums;
using RinhaBackend.API.Interfaces;
using RinhaBackend.API.Services.Interfaces;

namespace RinhaBackend.API.Services;

public class PaymentProcessingService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IProcessorHealthMonitor _processorHealthMonitor;
    private readonly IPaymentProcessorFactory _paymentProcessorFactory;
    private readonly ILogger<PaymentProcessingService> _logger;

    public PaymentProcessingService(
        IPaymentRepository paymentRepository, 
        IProcessorHealthMonitor processorHealthMonitor, 
        IPaymentProcessorFactory paymentProcessorFactory, 
        ILogger<PaymentProcessingService> logger)
    {
        _paymentRepository = paymentRepository;
        _processorHealthMonitor = processorHealthMonitor;
        _paymentProcessorFactory = paymentProcessorFactory;
        _logger = logger;
    }

    public async ValueTask ProcessPendingPaymentAsync(Guid paymentId)
    {
        try
        {
            _logger.LogInformation("Processing payment {PaymentId}", paymentId);
        
            var payment = await _paymentRepository.GetPaymentByIdAsync(paymentId);
        
            if (payment == null)
            {
                _logger.LogWarning("Payment {PaymentId} not found", paymentId);
                return;
            }
        
            if (payment.Status != PaymentStatus.Pending)
            {
                _logger.LogInformation("Payment {PaymentId} already processed with status {Status}", 
                    paymentId, payment.Status);
                return;
            }
        
            var bestProcessor = await _processorHealthMonitor.GetBestProcessorAsync();
            
            if (bestProcessor.Status == ProcessorStatus.Unhealthy)
            {
                _logger.LogError("No healthy processors available for payment {PaymentId}", paymentId);
                await UpdatePaymentStatus(payment, PaymentStatus.Failed);
                return;
            }
            
            _logger.LogDebug("Using processor {ProcessorName} for payment {PaymentId}", 
                bestProcessor.ProcessorName, paymentId);
            
            var paymentProcessor = _paymentProcessorFactory.Create(bestProcessor.ProcessorName);
        
            await paymentProcessor.ProcessPaymentAsync(payment);
        
            payment.ProcessedAt = DateTime.UtcNow;
            payment.ProcessorName = GetPaymentProcessorByName(bestProcessor.ProcessorName);
        
            await UpdatePaymentStatus(payment, PaymentStatus.Completed);

            _logger.LogInformation("Payment {PaymentId} processed successfully using {ProcessorName}", 
                paymentId, bestProcessor.ProcessorName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment {PaymentId}", paymentId);
            
            var payment = await _paymentRepository.GetPaymentByIdAsync(paymentId);
            if (payment != null)
            {
                await UpdatePaymentStatus(payment, PaymentStatus.Failed);
            }
        }
    }
    
    private async Task UpdatePaymentStatus(Payment payment, PaymentStatus status)
    {
        try
        {
            payment.Status = status;
            await _paymentRepository.UpdatePaymentAsync(payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update payment {PaymentId} status to {Status}", 
                payment.Id, status);
        }
    }
    
    private PaymentProcessor GetPaymentProcessorByName(string processorName)
    {
        return processorName.ToLowerInvariant() switch
        {
            "default" => PaymentProcessor.Default,
            "fallback" => PaymentProcessor.Fallback,
            _ => PaymentProcessor.Default
        };
    }
}