using System.Text.Json;
using System.Text.Json.Serialization;
using RinhaBackend.API.Configurations;
using RinhaBackend.API.Services.Interfaces;
using StackExchange.Redis;

namespace RinhaBackend.API.Services;

public class RedisCacheService : ICacheService
{
    private readonly IDatabase _database;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCacheService(
        IConnectionMultiplexer redis,
        ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _database = redis.GetDatabase();
        _logger = logger;
        
        _jsonOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = AppJsonSerializerContext.Default
        };
    }
    
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

    public async Task<bool> TryAddAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        try
        {
            if (string.IsNullOrEmpty(key))
                return false;

            var serializedValue = SerializeValue(value);
            var exp = expiration ?? TimeSpan.FromHours(1);
            
            var wasAdded = await _database.StringSetAsync(key, serializedValue, exp, When.NotExists);
            return wasAdded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding key {Key} to cache async", key);
            return false;
        }
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            if (string.IsNullOrEmpty(key))
                return default;

            var value = await _database.StringGetAsync(key);
            
            if (!value.HasValue)
                return default;

            return DeserializeValue<T>(value!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting key {Key} from cache async", key);
            return default;
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            if (string.IsNullOrEmpty(key))
                return false;

            return await _database.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if key {Key} exists in cache async", key);
            return false;
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            if (string.IsNullOrEmpty(key))
                return;

            await _database.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing key {Key} from cache async", key);
        }
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
    
    #region Private Serialization Methods

    private string SerializeValue<T>(T value)
    {
        if (value is string stringValue)
            return stringValue;
            
        if (value is null)
            return string.Empty;
        
        if (IsPrimitiveType(typeof(T)))
            return value.ToString() ?? string.Empty;
        
        return JsonSerializer.Serialize(value, _jsonOptions);
    }

    private T? DeserializeValue<T>(string value)
    {
        if (string.IsNullOrEmpty(value))
            return default;

        var targetType = typeof(T);
        
        if (targetType == typeof(string))
            return (T)(object)value;
        
        if (IsPrimitiveType(targetType))
        {
            return (T)Convert.ChangeType(value, targetType);
        }
        
        return JsonSerializer.Deserialize<T>(value, _jsonOptions);
    }

    private static bool IsPrimitiveType(Type type)
    {
        return type.IsPrimitive || 
               type == typeof(string) || 
               type == typeof(DateTime) || 
               type == typeof(DateTimeOffset) || 
               type == typeof(TimeSpan) || 
               type == typeof(Guid) ||
               type == typeof(decimal);
    }

    #endregion
}