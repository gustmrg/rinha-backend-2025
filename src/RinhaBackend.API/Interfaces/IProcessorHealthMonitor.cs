using RinhaBackend.API.Entities;

namespace RinhaBackend.API.Interfaces;

public interface IProcessorHealthMonitor
{
    Task<IEnumerable<ProcessorHealthInfo>> GetAllHealthInfosAsync(CancellationToken cancellationToken = default);
    Task<ProcessorHealthInfo> GetHealthInfoAsync(string processorName, CancellationToken cancellationToken = default);
}