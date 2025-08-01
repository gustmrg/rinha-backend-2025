using RinhaBackend.API.Entities;

namespace RinhaBackend.API.Services.Interfaces;

public interface IProcessorHealthMonitor
{
    Task<IEnumerable<ProcessorHealthInfo>> GetAllHealthInfosAsync();
    Task<ProcessorHealthInfo> GetHealthInfoAsync(string processorName);
    Task<ProcessorHealthInfo> GetBestProcessorAsync();
    Task InvalidateCacheAsync();
}