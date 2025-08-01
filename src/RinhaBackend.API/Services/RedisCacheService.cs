using System.Text.Json;
using System.Text.Json.Serialization;
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
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public bool TryAdd<T>(string key, T value, TimeSpan? expiration = null)
    {
        try
        {
            if (string.IsNullOrEmpty(key))
                return false;

            var serializedValue = SerializeValue(value);
            var exp = expiration ?? TimeSpan.FromHours(1); // Default 1h
            
            var wasAdded = _database.StringSet(key, serializedValue, exp, When.NotExists);
            
            if (wasAdded)
            {
                _logger.LogDebug("Added key {Key} to cache", key);
            }
            else
            {
                _logger.LogDebug("Key {Key} already exists in cache", key);
            }
            
            return wasAdded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding key {Key} to cache", key);
            return false;
        }
    }

    public T? Get<T>(string key)
    {
        try
        {
            if (string.IsNullOrEmpty(key))
                return default;

            var value = _database.StringGet(key);
            
            if (!value.HasValue)
            {
                _logger.LogDebug("Key {Key} not found in cache", key);
                return default;
            }

            return DeserializeValue<T>(value!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting key {Key} from cache", key);
            return default;
        }
    }

    public bool Exists(string key)
    {
        try
        {
            if (string.IsNullOrEmpty(key))
                return false;

            return _database.KeyExists(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if key {Key} exists in cache", key);
            return false;
        }
    }

    public void Remove(string key)
    {
        try
        {
            if (string.IsNullOrEmpty(key))
                return;

            _database.KeyDelete(key);
            _logger.LogDebug("Removed key {Key} from cache", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing key {Key} from cache", key);
        }
    }

    public void RemoveByPattern(string pattern)
    {
        try
        {
            if (string.IsNullOrEmpty(pattern))
                return;

            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys(pattern: pattern);
            
            foreach (var key in keys)
            {
                _database.KeyDelete(key);
            }
            
            _logger.LogDebug("Removed keys matching pattern {Pattern} from cache", pattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing keys by pattern {Pattern} from cache", pattern);
        }
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

    public async Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            if (string.IsNullOrEmpty(pattern))
                return;

            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys(pattern: pattern);
            
            var tasks = keys.Select(key => _database.KeyDeleteAsync(key));
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing keys by pattern {Pattern} from cache async", pattern);
        }
    }

    public async Task<bool> SetIfNotExistsAsync(string key, TimeSpan? expiration = null)
    {
        try
        {
            if (string.IsNullOrEmpty(key))
                return false;

            var exp = expiration ?? TimeSpan.FromHours(2);
            var timestamp = DateTime.UtcNow.Ticks.ToString();
            
            return await _database.StringSetAsync(key, timestamp, exp, When.NotExists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting key {Key} if not exists async", key);
            return false;
        }
    }

    public bool SetIfNotExists(string key, TimeSpan? expiration = null)
    {
        try
        {
            if (string.IsNullOrEmpty(key))
                return false;

            var exp = expiration ?? TimeSpan.FromHours(2);
            var timestamp = DateTime.UtcNow.Ticks.ToString();
            
            return _database.StringSet(key, timestamp, exp, When.NotExists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting key {Key} if not exists", key);
            return false;
        }
    }

    #region Utility Methods

    public async Task<long> GetCountByPatternAsync(string pattern)
    {
        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys(pattern: pattern);
            return keys.LongCount();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting count for pattern {Pattern}", pattern);
            return -1;
        }
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            await _database.PingAsync();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task FlushAllAsync()
    {
        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            await server.FlushAllDatabasesAsync();
            _logger.LogWarning("Flushed all Redis databases");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error flushing Redis databases");
        }
    }

    #endregion
    
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
    
    public void Dispose()
    {
        _redis?.Dispose();
    }
}