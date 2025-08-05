using RinhaBackend.API.Domain.Entities;
using RinhaBackend.API.Domain.Enums;
using RinhaBackend.API.Domain.Results;
using RinhaBackend.API.Repositories.Interfaces;
using RinhaBackend.API.Services.Interfaces;

namespace RinhaBackend.API.Services;

public class PaymentProcessingService : IPaymentProcessingService
{
    private readonly ICacheService _cache;
    private readonly DefaultPaymentClient _defaultPaymentClient;
    private readonly FallbackPaymentClient _fallbackPaymentClient;
    private readonly IPaymentRepository _paymentRepository;

    public PaymentProcessingService(
        ICacheService cache,
        DefaultPaymentClient defaultPaymentClient,
        FallbackPaymentClient fallbackPaymentClient,
        IPaymentRepository paymentRepository)
    {
        _cache = cache;
        _defaultPaymentClient = defaultPaymentClient;
        _fallbackPaymentClient = fallbackPaymentClient;
        _paymentRepository = paymentRepository;
    }

    public async Task<PaymentProcessingResult> ProcessPayment(Payment payment)
    {
        var cacheKey = $"payment:{payment.CorrelationId}";
        
        HttpResponseMessage? response = null;
        PaymentProcessor usedProcessor = PaymentProcessor.Default;
        
        try
        {
            response = await _defaultPaymentClient.PostPaymentAsync(payment);
            
            if (response.IsSuccessStatusCode)
            {
                payment.Status = PaymentStatus.Succeeded;
                usedProcessor = PaymentProcessor.Default;
            }
            else
            {
                throw new HttpRequestException($"Default payment client failed with status code: {response.StatusCode}");
            }
        }
        catch (Exception)
        {
            try
            {
                response?.Dispose();
                response = await _fallbackPaymentClient.PostPaymentAsync(payment);
                
                if (response.IsSuccessStatusCode)
                {
                    payment.Status = PaymentStatus.Succeeded;
                    usedProcessor = PaymentProcessor.Fallback;
                }
                else
                {
                    throw new HttpRequestException($"Fallback payment client failed with status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response?.Dispose();
                payment.Status = PaymentStatus.Failed;
                await _cache.RemoveAsync(cacheKey);
                return PaymentProcessingResult.Failure(ex.Message);
            }
        }
        
        try
        {
            await _cache.RemoveAsync(cacheKey);
            await _cache.TryAddAsync(cacheKey, payment, TimeSpan.FromMinutes(5));
            await _paymentRepository.SavePaymentAsync(payment);
            
            response?.Dispose();
            return PaymentProcessingResult.Success(payment.Status, usedProcessor);
        }
        catch (Exception ex)
        {
            response?.Dispose();
            await _cache.RemoveAsync(cacheKey);
            return PaymentProcessingResult.Failure(ex.Message);
        }
    }
}