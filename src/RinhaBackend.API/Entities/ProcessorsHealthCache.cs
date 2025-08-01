namespace RinhaBackend.API.Entities;

public class ProcessorsHealthCache
{
    public ProcessorHealthInfo[] Processors { get; set; } = Array.Empty<ProcessorHealthInfo>();
    public DateTime CachedAt { get; set; }
    public TimeSpan CacheAge => DateTime.UtcNow - CachedAt;
}