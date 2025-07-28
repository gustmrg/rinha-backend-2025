using RinhaBackend.API.Entities;
using RinhaBackend.API.Interfaces;

namespace RinhaBackend.API.Services;

public class SmartProcessorSelectionStrategy : IProcessorSelectionStrategy
{
    public Task<ProcessorHealthInfo> SelectProcessorAsync(IEnumerable<ProcessorHealthInfo> healthInfos, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}