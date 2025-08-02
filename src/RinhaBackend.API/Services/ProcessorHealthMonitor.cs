using System.Diagnostics;
using System.Text.Json;
using RinhaBackend.API.DTOs.Responses;
using RinhaBackend.API.Entities;
using RinhaBackend.API.Enums;
using RinhaBackend.API.Services.Interfaces;

namespace RinhaBackend.API.Services;

public class ProcessorHealthMonitor : IProcessorHealthMonitor
{
    private readonly ICacheService _cache;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ProcessorHealthMonitor> _logger;
    private readonly IConfiguration _configuration;
    
    private const string HEALTH_CACHE_KEY = "processors:health";
    private static readonly TimeSpan CACHE_DURATION = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan HEALTH_CHECK_TIMEOUT = TimeSpan.FromSeconds(1);
    
    private readonly Dictionary<string, string> _processorUrls;

    public ProcessorHealthMonitor(
        ICacheService cache, 
        IHttpClientFactory httpClientFactory, 
        ILogger<ProcessorHealthMonitor> logger, 
        IConfiguration configuration)
    {
        _cache = cache;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _configuration = configuration;
        
        _processorUrls = new Dictionary<string, string>
        {
            ["default"] = _configuration["PROCESSOR_DEFAULT_URL"] ?? "http://payment-processor-default:8080",
            ["fallback"] = _configuration["PROCESSOR_FALLBACK_URL"] ?? "http://payment-processor-fallback:8080"
        };
    }
    
    public async Task<IEnumerable<ProcessorHealthInfo>> GetAllHealthInfosAsync()
    {
        var cachedHealth = await _cache.GetAsync<ProcessorsHealthCache>(HEALTH_CACHE_KEY);
        
        if (cachedHealth != null && cachedHealth.CacheAge < CACHE_DURATION)
        {
            _logger.LogDebug("Using cached processor health (age: {Age}s)", cachedHealth.CacheAge.TotalSeconds);
            return cachedHealth.Processors;
        }
        
        _logger.LogDebug("Cache expired or missing, checking processor health");
        var healthInfos = await CheckAllProcessorsHealthAsync();
        
        var cacheData = new ProcessorsHealthCache
        {
            Processors = healthInfos,
            CachedAt = DateTime.UtcNow
        };
            
        await _cache.TryAddAsync(HEALTH_CACHE_KEY, cacheData, CACHE_DURATION);
            
        return healthInfos;
    }

    public async Task<ProcessorHealthInfo> GetHealthInfoAsync(string processorName)
    {
        var allProcessors = await GetAllHealthInfosAsync();
        return allProcessors.FirstOrDefault(p => p.ProcessorName.Equals(processorName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<ProcessorHealthInfo> GetBestProcessorAsync()
    {
        var allProcessors = await GetAllHealthInfosAsync();
        
        // Estratégia de seleção por prioridade
        var orderedProcessors = allProcessors
            .OrderBy(p => GetProcessorPriority(p))
            .ThenBy(p => p.ResponseTime)
            .ToArray();

        var selected = orderedProcessors.First();
        
        _logger.LogDebug("Selected processor {ProcessorName} with status {Status} (response time: {ResponseTime}ms)", 
            selected.ProcessorName, selected.Status, selected.ResponseTime.TotalMilliseconds);
            
        return selected;
    }

    public async Task InvalidateCacheAsync()
    {
        await _cache.RemoveAsync(HEALTH_CACHE_KEY);
        _logger.LogDebug("Processor health cache invalidated");
    }
    
    private async Task<ProcessorHealthInfo[]> CheckAllProcessorsHealthAsync()
    {
        _logger.LogDebug("Starting health check for {Count} processors", _processorUrls.Count);
        
        // Faz todos os checks em paralelo para máxima eficiência
        var healthCheckTasks = _processorUrls.Select(kvp => 
            CheckSingleProcessorHealthAsync(kvp.Key, kvp.Value)).ToArray();
        
        var results = await Task.WhenAll(healthCheckTasks);
        
        var healthyCount = results.Count(r => r.Status == ProcessorStatus.Healthy);
        _logger.LogInformation("Health check completed: {Healthy}/{Total} processors healthy", 
            healthyCount, results.Length);
        
        return results;
    }
    
    private async Task<ProcessorHealthInfo> CheckSingleProcessorHealthAsync(string processorName, string baseUrl)
    {
        var stopwatch = Stopwatch.StartNew();
        var healthInfo = new ProcessorHealthInfo
        {
            ProcessorName = processorName,
            LastChecked = DateTime.UtcNow
        };

        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = HEALTH_CHECK_TIMEOUT;
            
            // Usa endpoint /health (padrão) ou /ping se disponível
            var healthUrl = $"{baseUrl.TrimEnd('/')}/service-health";
            
            _logger.LogDebug("Checking health for {ProcessorName} at {Url}", processorName, healthUrl);
            
            using var response = await httpClient.GetAsync(healthUrl);
            stopwatch.Stop();
            
            healthInfo.ResponseTime = stopwatch.Elapsed;
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ProcessorHealthCheckResponse>(content);

                if (result!.Failing)
                {
                    healthInfo.Status = ProcessorStatus.Unhealthy;
                }
                else
                {
                    healthInfo.Status = healthInfo.ResponseTime.TotalMilliseconds switch
                    {
                        < 1000 => ProcessorStatus.Healthy,
                        < 2000 => ProcessorStatus.Degraded,
                        _ => ProcessorStatus.Unhealthy
                    };
                }
                
                _logger.LogDebug("Processor {ProcessorName} is {Status} (response time: {ResponseTime}ms)", 
                    processorName, healthInfo.Status, healthInfo.ResponseTime.TotalMilliseconds);
            }
            else
            {
                healthInfo.Status = ProcessorStatus.Unhealthy;
                healthInfo.ErrorMessage = $"HTTP {response.StatusCode}";
                
                _logger.LogWarning("Processor {ProcessorName} returned {StatusCode}", 
                    processorName, response.StatusCode);
            }
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            stopwatch.Stop();
            healthInfo.Status = ProcessorStatus.Unhealthy;
            healthInfo.ResponseTime = HEALTH_CHECK_TIMEOUT;
            healthInfo.ErrorMessage = "Timeout";
            
            _logger.LogWarning("Processor {ProcessorName} health check timed out", processorName);
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            healthInfo.Status = ProcessorStatus.Unhealthy;
            healthInfo.ResponseTime = stopwatch.Elapsed;
            healthInfo.ErrorMessage = ex.Message;
            
            _logger.LogWarning(ex, "Processor {ProcessorName} health check failed", processorName);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            healthInfo.Status = ProcessorStatus.Unhealthy;
            healthInfo.ResponseTime = stopwatch.Elapsed;
            healthInfo.ErrorMessage = ex.Message;
            
            _logger.LogError(ex, "Unexpected error checking processor {ProcessorName}", processorName);
        }

        return healthInfo;
    }
    
    private static int GetProcessorPriority(ProcessorHealthInfo processor)
    {
        // Prioridade de seleção (menor número = maior prioridade)
        var basePriority = processor.ProcessorName.ToLower() switch
        {
            "default" => 1,    // Default tem prioridade
            "fallback" => 2,   // Fallback é segunda opção
            _ => 3             // Outros processadores
        };

        // Ajusta prioridade baseado no status
        var statusPenalty = processor.Status switch
        {
            ProcessorStatus.Healthy => 0,
            ProcessorStatus.Degraded => 10,
            ProcessorStatus.Unhealthy => 20,
            ProcessorStatus.Unknown => 15,
            _ => 25
        };

        return basePriority + statusPenalty;
    }
    
    private ProcessorHealthInfo[] GetDefaultHealthInfos()
    {
        _logger.LogWarning("Using default health infos (assuming all processors healthy)");
        
        return _processorUrls.Keys.Select(processorName => new ProcessorHealthInfo
        {
            ProcessorName = processorName,
            Status = ProcessorStatus.Healthy,
            ResponseTime = TimeSpan.FromMilliseconds(100), // Assume 100ms
            LastChecked = DateTime.UtcNow,
            ErrorMessage = "Default assumption"
        }).ToArray();
    }
}