namespace RinhaBackend.API.Services.Interfaces;

public interface ICacheService
{
    bool TryAdd<T>(string key, T value, TimeSpan? expiration = null);
    T? Get<T>(string key);
    bool Exists(string key);
    void Remove(string key);
    void RemoveByPattern(string pattern);

    Task<bool> TryAddAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task<T?> GetAsync<T>(string key);
    Task<bool> ExistsAsync(string key);
    Task RemoveAsync(string key);
    Task RemoveByPatternAsync(string pattern);
    
    Task<bool> SetIfNotExistsAsync(string key, TimeSpan? expiration = null);
    bool SetIfNotExists(string key, TimeSpan? expiration = null);
    
    Task<long> GetCountByPatternAsync(string pattern);
    Task<bool> IsHealthyAsync();
    Task FlushAllAsync();
}