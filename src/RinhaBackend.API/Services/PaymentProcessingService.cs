using RinhaBackend.API.Domain.Entities;
using RinhaBackend.API.Domain.Enums;
using RinhaBackend.API.Domain.Results;
using RinhaBackend.API.Services.Interfaces;

namespace RinhaBackend.API.Services;

public class PaymentProcessingService : IPaymentProcessingService
{
    private readonly ICacheService _cache;

    public PaymentProcessingService(ICacheService cache)
    {
        _cache = cache;
    }

    public async Task<PaymentProcessingResult> ProcessPayment(Payment payment)
    {
        var cacheKey = $"payment:{payment.CorrelationId}";
        
        // Process the payment

        try
        {
            payment.Status = PaymentStatus.Succeeded;

            await _cache.RemoveAsync(cacheKey);
            await _cache.TryAddAsync(cacheKey, payment, TimeSpan.FromMinutes(5));
        
            return PaymentProcessingResult.Success(payment.Status, PaymentProcessor.Default);
        }
        catch (Exception ex)
        {
            await _cache.RemoveAsync(cacheKey);
            return PaymentProcessingResult.Failure(ex.Message);
        }
    }
}