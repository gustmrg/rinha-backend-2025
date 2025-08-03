using RinhaBackend.API.Services.Interfaces;

namespace RinhaBackend.API.Services;

public class RedisCacheService : ICacheService
{
    public bool TryAdd<T>(string key, T value, TimeSpan? expiration = null)
    {
        throw new NotImplementedException();
    }

    public T? Get<T>(string key)
    {
        throw new NotImplementedException();
    }

    public bool Exists(string key)
    {
        throw new NotImplementedException();
    }

    public void Remove(string key)
    {
        throw new NotImplementedException();
    }

    public void RemoveByPattern(string pattern)
    {
        throw new NotImplementedException();
    }

    public Task<bool> TryAddAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        throw new NotImplementedException();
    }

    public Task<T?> GetAsync<T>(string key)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ExistsAsync(string key)
    {
        throw new NotImplementedException();
    }

    public Task RemoveAsync(string key)
    {
        throw new NotImplementedException();
    }

    public Task RemoveByPatternAsync(string pattern)
    {
        throw new NotImplementedException();
    }

    public Task<bool> SetIfNotExistsAsync(string key, TimeSpan? expiration = null)
    {
        throw new NotImplementedException();
    }

    public bool SetIfNotExists(string key, TimeSpan? expiration = null)
    {
        throw new NotImplementedException();
    }

    public Task<long> GetCountByPatternAsync(string pattern)
    {
        throw new NotImplementedException();
    }

    public Task<bool> IsHealthyAsync()
    {
        throw new NotImplementedException();
    }

    public Task FlushAllAsync()
    {
        throw new NotImplementedException();
    }
}