using System.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RinhaBackend.API.Entities;
using RinhaBackend.API.Interfaces;

namespace RinhaBackend.API.Services;

public class ProcessorHealthMonitor : IProcessorHealthMonitor
{
    private readonly IEnumerable<IPaymentProcessor> _processors;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProcessorHealthMonitor> _logger;
    private readonly ProcessorSelectionConfiguration _config;

    public ProcessorHealthMonitor(
        IEnumerable<IPaymentProcessor> processors, 
        IMemoryCache cache, 
        IConfiguration configuration, 
        ILogger<ProcessorHealthMonitor> logger, 
        IOptions<ProcessorSelectionConfiguration> config)
    {
        _processors = processors;
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
        _config = config.Value;
    }

    public async Task<IEnumerable<ProcessorHealthInfo>> GetAllHealthInfosAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = "all_processor_health_infos";
        
        if (_cache.TryGetValue(cacheKey, out IEnumerable<ProcessorHealthInfo> cachedHealthInfos))
        {
            _logger.LogDebug("Cache hit for health infos for all processors");
            return cachedHealthInfos;
        }

        _logger.LogDebug("Cache miss for health infos, fetching updated data from processors");
        
        var healthInfos = new List<ProcessorHealthInfo>();
        var tasks = _processors.Select(async processor =>
        {
            try
            {
                return await GetHealthInfoAsync(processor.ProcessorName, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting health info from processor {ProcessorName}", processor.ProcessorName);
                return CreateUnhealthyInfo(processor.ProcessorName);
            }
        });
        
        var results = await Task.WhenAll(tasks);
        healthInfos.AddRange(results);
        
        var cacheExpiration = TimeSpan.FromMinutes(_config.Cache.HealthCacheSeconds);
        _cache.Set(cacheKey, healthInfos, cacheExpiration);
        
        _logger.LogInformation("Health check finished for {ProcessorCount} processors. Healthy: {HealthyCount}", 
            healthInfos.Count, healthInfos.Count(h => h.IsHealthy));

        return healthInfos;
    }

    public async Task<ProcessorHealthInfo> GetHealthInfoAsync(string processorName, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"health_{processorName}";
        
        if (_cache.TryGetValue(cacheKey, out ProcessorHealthInfo cachedHealth))
        {
            return cachedHealth;
        }
        
        var processor = _processors.FirstOrDefault(p => p.ProcessorName == processorName);

        if (processor is null)
        {
            throw new ArgumentException($"Processor {processorName} not found");
        }
        
        var healthInfo = await CheckProcessorHealthAsync(processor, cancellationToken);
        
        var cacheExpiration = TimeSpan.FromSeconds(_config.Cache.HealthCacheSeconds);
        _cache.Set(cacheKey, healthInfo, cacheExpiration);
        
        return healthInfo;
    }
    
    private async Task<ProcessorHealthInfo> CheckProcessorHealthAsync(IPaymentProcessor processor, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        bool isHealthy = false;
        
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));
            
            isHealthy = await processor.IsHealthyAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Health check timeout for {ProcessorName}", processor.ProcessorName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check failed for {ProcessorName}", processor.ProcessorName);
        }
        
        stopwatch.Stop();
        
        var processorConfig = _config.Processors.GetValueOrDefault(processor.ProcessorName, new ProcessorConfig());
        
        return new ProcessorHealthInfo
        {
            ProcessorName = processor.ProcessorName,
            IsHealthy = isHealthy && processorConfig.Enabled,
            ResponseTime = stopwatch.Elapsed,
            LastChecked = DateTime.UtcNow,
            SuccessRate = await GetSuccessRate(processor.ProcessorName),
            Priority = processorConfig.Priority,
            Metadata = new Dictionary<string, object>
            {
                ["enabled"] = processorConfig.Enabled,
                ["health_check_duration_ms"] = stopwatch.ElapsedMilliseconds
            }
        };
    }
    
    private ProcessorHealthInfo CreateUnhealthyInfo(string processorName)
    {
        var processorConfig = _config.Processors.GetValueOrDefault(processorName, new ProcessorConfig());
        
        return new ProcessorHealthInfo
        {
            ProcessorName = processorName,
            IsHealthy = false,
            ResponseTime = TimeSpan.FromSeconds(30), // Penalty time
            LastChecked = DateTime.UtcNow,
            SuccessRate = 0,
            Priority = processorConfig.Priority
        };
    }
    
    private async Task<double> GetSuccessRate(string processorName)
    {
        // TODO: Implementar busca no banco de dados das últimas transações
        // Por enquanto, retornando valores simulados baseados no processador
        
        await Task.Delay(1); // Simular async call
        
        return processorName switch
        {
            "Default" => 0.96,
            "Fallback" => 0.94,
            _ => 0.90
        };
    }
}