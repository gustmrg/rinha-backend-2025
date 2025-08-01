using RinhaBackend.API.Services;
using RinhaBackend.API.Services.Interfaces;

namespace RinhaBackend.API.Extensions;

public static class CacheExtensions
{
    public static bool TryAddCorrelationId(this ICacheService cache, string correlationId, TimeSpan? expiration = null)
    {
        return cache.SetIfNotExists(CacheKeys.CorrelationId(correlationId), expiration);
    }
    
    public static async Task<bool> TryAddCorrelationIdAsync(this ICacheService cache, string correlationId, TimeSpan? expiration = null)
    {
        return await cache.SetIfNotExistsAsync(CacheKeys.CorrelationId(correlationId), expiration);
    }
    
    public static bool CorrelationIdExists(this ICacheService cache, string correlationId)
    {
        return cache.Exists(CacheKeys.CorrelationId(correlationId));
    }
    
    public static async Task<bool> CorrelationIdExistsAsync(this ICacheService cache, string correlationId)
    {
        return await cache.ExistsAsync(CacheKeys.CorrelationId(correlationId));
    }
    
    public static void RemoveCorrelationId(this ICacheService cache, string correlationId)
    {
        cache.Remove(CacheKeys.CorrelationId(correlationId));
    }
    
    public static async Task SetPaymentsSummaryAsync<T>(this ICacheService cache, T summary, TimeSpan? expiration = null)
    {
        await cache.TryAddAsync(CacheKeys.PAYMENTS_SUMMARY, summary, expiration);
    }
    
    public static async Task<T?> GetPaymentsSummaryAsync<T>(this ICacheService cache)
    {
        return await cache.GetAsync<T>(CacheKeys.PAYMENTS_SUMMARY);
    }
    
    public static async Task InvalidatePaymentsSummaryAsync(this ICacheService cache)
    {
        // Remove the old single summary key for backward compatibility
        await cache.RemoveAsync(CacheKeys.PAYMENTS_SUMMARY);
        
        // Remove all date-based summary cache keys
        await cache.RemoveByPatternAsync($"{CacheKeys.PAYMENTS_SUMMARY}:*");
    }
    
    // Processor Health Cache Extensions
    public static async Task SetProcessorHealthAsync<T>(this ICacheService cache, string processorName, T healthInfo, TimeSpan? expiration = null)
    {
        var cacheKey = CacheKeys.ProcessorHealth(processorName);
        await cache.RemoveAsync(cacheKey); // Remove existing entry first
        await cache.TryAddAsync(cacheKey, healthInfo, expiration);
    }
    
    public static async Task<T?> GetProcessorHealthAsync<T>(this ICacheService cache, string processorName)
    {
        var cacheKey = CacheKeys.ProcessorHealth(processorName);
        return await cache.GetAsync<T>(cacheKey);
    }
    
    public static async Task<bool> ProcessorHealthExistsAsync(this ICacheService cache, string processorName)
    {
        var cacheKey = CacheKeys.ProcessorHealth(processorName);
        return await cache.ExistsAsync(cacheKey);
    }
    
    public static async Task RemoveProcessorHealthAsync(this ICacheService cache, string processorName)
    {
        var cacheKey = CacheKeys.ProcessorHealth(processorName);
        await cache.RemoveAsync(cacheKey);
    }
    
    public static async Task SetAllProcessorsHealthAsync<T>(this ICacheService cache, T allHealthInfos, TimeSpan? expiration = null)
    {
        await cache.RemoveAsync(CacheKeys.ALL_PROCESSORS_HEALTH); // Remove existing entry first
        await cache.TryAddAsync(CacheKeys.ALL_PROCESSORS_HEALTH, allHealthInfos, expiration);
    }
    
    public static async Task<T?> GetAllProcessorsHealthAsync<T>(this ICacheService cache)
    {
        return await cache.GetAsync<T>(CacheKeys.ALL_PROCESSORS_HEALTH);
    }
    
    public static async Task RemoveAllProcessorsHealthAsync(this ICacheService cache)
    {
        await cache.RemoveAsync(CacheKeys.ALL_PROCESSORS_HEALTH);
    }
    
    public static async Task InvalidateAllProcessorHealthAsync(this ICacheService cache)
    {
        // Remove individual processor health caches
        await cache.RemoveByPatternAsync($"{CacheKeys.PROCESSOR_HEALTH_PREFIX}*");
        
        // Remove the aggregated health cache
        await cache.RemoveAsync(CacheKeys.ALL_PROCESSORS_HEALTH);
    }
}

public static class CacheKeys
{
    public const string CORRELATION_PREFIX = "corr:";
    public const string PAYMENTS_SUMMARY = "payments:summary";
    public const string PAYMENT_PREFIX = "payment:";
    public const string PROCESSOR_HEALTH_PREFIX = "processor:health:";
    public const string ALL_PROCESSORS_HEALTH = "processors:health:all";
    
    public static string CorrelationId(string correlationId) => CORRELATION_PREFIX + correlationId;
    public static string Payment(Guid paymentId) => PAYMENT_PREFIX + paymentId;
    public static string ProcessorHealth(string processorName) => PROCESSOR_HEALTH_PREFIX + processorName.ToLowerInvariant();
}