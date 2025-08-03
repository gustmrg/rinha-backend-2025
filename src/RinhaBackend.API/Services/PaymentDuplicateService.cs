using Microsoft.Extensions.Caching.Memory;
using RinhaBackend.API.Interfaces;

namespace RinhaBackend.API.Services;

public class PaymentDuplicateService
{
    private readonly IMemoryCache _localCache;
    private readonly IPaymentRepository _repository;

    public PaymentDuplicateService(IMemoryCache localCache, IPaymentRepository repository)
    {
        _localCache = localCache;
        _repository = repository;
    }
    
    public async Task<bool> PaymentExistsAsync(Guid correlationId)
    {
        if (_localCache.TryGetValue($"payment:{correlationId}", out _))
            return true;
        
        var exists = await _repository.PaymentExistsAsync(correlationId);
        
        if (exists)
        {
            _localCache.Set($"payment:{correlationId}", true, TimeSpan.FromMinutes(10));
        }
        
        return exists;
    }
    
    public void MarkAsProcessed(Guid correlationId)
    {
        _localCache.Set($"payment_processed:{correlationId}", true, TimeSpan.FromMinutes(10));
    }
}