using RinhaBackend.API.Entities;
using RinhaBackend.API.Enums;
using RinhaBackend.API.Interfaces;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace RinhaBackend.API.Services;

public class PaymentProcessingService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentProcessorFactory _paymentProcessorFactory;
    private readonly ILogger<PaymentProcessingService> _logger;
    private readonly IAsyncPolicy<HttpResponseMessage> _fallbackPolicy;
    private readonly PaymentDuplicateService _duplicateService;

    public PaymentProcessingService(
        IPaymentRepository paymentRepository, 
        IPaymentProcessorFactory paymentProcessorFactory, 
        ILogger<PaymentProcessingService> logger, 
        PaymentDuplicateService duplicateService)
    {
        _paymentRepository = paymentRepository;
        _paymentProcessorFactory = paymentProcessorFactory;
        _logger = logger;
        _duplicateService = duplicateService;
        _fallbackPolicy = CreateFallbackPolicy();
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
            
            var paymentExists = await _duplicateService.PaymentExistsAsync(payment.CorrelationId);

            if (paymentExists)
            {
                _logger.LogWarning("Payment {PaymentId} already exists", paymentId);
                return;
            }
        
            if (payment.Status != PaymentStatus.Pending)
            {
                _logger.LogInformation("Payment {PaymentId} already processed with status {Status}", 
                    paymentId, payment.Status);
                return;
            }
        
            await ProcessPaymentWithPolicies(payment, paymentId);
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
    
    private async Task ProcessPaymentWithPolicies(Payment payment, Guid paymentId)
    {
        var context = new Context($"payment-{paymentId}");
        context["payment"] = payment;
        
        await _fallbackPolicy.ExecuteAsync(async (ctx) => 
        {
            _logger.LogDebug("Using default processor for payment {PaymentId}", paymentId);
            
            var paymentProcessor = _paymentProcessorFactory.Create("default");
            await paymentProcessor.ProcessPaymentAsync(payment);
            
            payment.ProcessorName = PaymentProcessor.Default;
            
            await UpdatePaymentStatus(payment, PaymentStatus.Completed);
            
            _logger.LogInformation("Payment {PaymentId} processed successfully using default processor", paymentId);
                
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        }, context);
    }
    
    private async Task<HttpResponseMessage> CallFallbackProcessor(Context context)
    {
        var payment = (Payment)context["payment"];
        
        _logger.LogInformation("Calling fallback processor for payment {PaymentId}", payment.Id);
        
        var fallbackProcessor = _paymentProcessorFactory.Create("fallback");
        await fallbackProcessor.ProcessPaymentAsync(payment);
        
        payment.ProcessorName = PaymentProcessor.Fallback;
        
        await UpdatePaymentStatus(payment, PaymentStatus.Completed);
        
        _logger.LogInformation("Payment {PaymentId} processed successfully using fallback processor", payment.Id);
        
        return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
    }
    
    private IAsyncPolicy<HttpResponseMessage> CreateFallbackPolicy()
    {
        var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10));

        var retryPolicyDefault = HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TimeoutRejectedException>()
            .WaitAndRetryAsync(
                retryCount: 1,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(2),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning("Tentando DEFAULT novamente. Tentativa: {RetryCount}", retryCount);
                });

        var fallbackPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TimeoutRejectedException>()
            .FallbackAsync(
                fallbackAction: async (context, cancellationToken) =>
                {
                    _logger.LogWarning("DEFAULT falhou, tentando FALLBACK...");
                    return await CallFallbackProcessor(context);
                },
                onFallbackAsync: async (result, context) =>
                {
                    _logger.LogInformation("Executando fallback para processador FALLBACK");
                    await Task.CompletedTask;
                });

        return Policy.WrapAsync(retryPolicyDefault, fallbackPolicy, timeoutPolicy);
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