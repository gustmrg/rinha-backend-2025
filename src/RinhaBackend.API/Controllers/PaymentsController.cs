using Microsoft.AspNetCore.Mvc;
using RinhaBackend.API.DTOs.Requests;
using RinhaBackend.API.DTOs.Responses;
using RinhaBackend.API.Entities;
using RinhaBackend.API.Enums;
using RinhaBackend.API.Interfaces;
using RinhaBackend.API.Services;
using RinhaBackend.API.Services.Interfaces;
using RinhaBackend.API.Extensions;

namespace RinhaBackend.API.Controllers;

[ApiController]
public class PaymentsController : ControllerBase
{
    [HttpGet]
    [Route("payments-summary")]
    public async Task<IActionResult> GetPaymentsSummaryAsync(
        [FromServices] IPaymentRepository paymentRepository,
        [FromServices] ICacheService cache,
        [FromQuery] DateTime from, DateTime to)
    {
        // Create cache key based on date range for better granularity
        var cacheKey = $"{CacheKeys.PAYMENTS_SUMMARY}:{from:yyyyMMdd}:{to:yyyyMMdd}";
        
        // Try to get from cache first
        var cachedResponse = await cache.GetAsync<PaymentsSummaryResponse>(cacheKey);
        if (cachedResponse != null)
        {
            return Ok(cachedResponse);
        }
        
        // Cache miss - fetch summary from database
        var response = await paymentRepository.GetPaymentsSummaryAsync(from, to);
        
        // Cache the response with shorter TTL for recent data, longer for older data
        var cacheExpiration = CalculateCacheExpiration(from, to);
        await cache.TryAddAsync(cacheKey, response, cacheExpiration);
        
        return Ok(response);
    }
    
    private static TimeSpan CalculateCacheExpiration(DateTime from, DateTime to)
    {
        var now = DateTime.UtcNow;
        var daysSinceEnd = (now - to).TotalDays;
        
        // Recent data (within last day): cache for 5 minutes
        if (daysSinceEnd <= 1)
            return TimeSpan.FromMinutes(5);
        
        // Data from last week: cache for 1 hour
        if (daysSinceEnd <= 7)
            return TimeSpan.FromHours(1);
        
        // Older data: cache for 24 hours (more stable)
        return TimeSpan.FromHours(24);
    }

    [HttpPost]
    [Route("payments")]
    public async Task<IActionResult> CreatePaymentAsync(
        [FromServices] IBackgroundTaskQueue backgroundTaskQueue,
        [FromServices] IPaymentRepository paymentRepository,
        [FromServices] PaymentProcessingService paymentProcessingService,
        [FromServices] ICacheService cache,
        [FromServices] ILogger<PaymentsController> logger,
        [FromBody] CreatePaymentRequest request)
    {
        try
        {
            if (!cache.TryAddCorrelationId(request.CorrelationId.ToString()))
            {
                return Conflict(new { 
                    error = "Payment with the same correlation ID already exists",
                    correlationId = request.CorrelationId 
                });
            }
        
            var payment = new Payment
            {
                Id = Guid.CreateVersion7(),
                Amount = request.Amount,
                Status = PaymentStatus.Pending,
                RequestedAt = DateTime.UtcNow,
                CorrelationId = request.CorrelationId
            };

            try
            {
                await paymentRepository.CreatePaymentAsync(payment);
            
                await cache.TryAddAsync(CacheKeys.Payment(payment.Id), payment, TimeSpan.FromMinutes(30));

                await backgroundTaskQueue.QueueBackgroundWorkItemAsync(async (token, serviceProvider) =>
                {
                    var paymentService = serviceProvider.GetRequiredService<PaymentProcessingService>();
                    var cacheService = serviceProvider.GetRequiredService<ICacheService>();
                    await paymentService.ProcessPendingPaymentAsync(payment.Id);
                    await cacheService.InvalidatePaymentsSummaryAsync();
                });
                
                return Accepted(new { 
                    paymentId = payment.Id,
                    status = "pending",
                    correlationId = request.CorrelationId 
                });
            }
            catch (Exception ex)
            {
                cache.RemoveCorrelationId(request.CorrelationId.ToString());
                logger.LogError(ex, "Failed to create payment for correlation {CorrelationId}", request.CorrelationId);
                return StatusCode(500, new { error = "Failed to create payment" });
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error in CreatePaymentAsync");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}