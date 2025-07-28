using RinhaBackend.API.Entities;

namespace RinhaBackend.API.Interfaces;

public interface IProcessorSelectionStrategy
{
    Task<ProcessorHealthInfo> SelectProcessorAsync(IEnumerable<ProcessorHealthInfo> healthInfos, CancellationToken cancellationToken = default);
}