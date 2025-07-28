namespace RinhaBackend.API.Entities;

public class ProcessorSelectionConfiguration
{
    public Dictionary<string, ProcessorConfig> Processors { get; set; } = new();
    public SelectionWeights Weights { get; set; } = new();
    public CacheSettings Cache { get; set; } = new();
}

public class ProcessorConfig
{
    public decimal Cost { get; set; }
    public int Priority { get; set; }
    public bool Enabled { get; set; } = true;
}

public class SelectionWeights
{
    public double ResponseTime { get; set; } = 0.3;
    public double SuccessRate { get; set; } = 0.4;
    public double Cost { get; set; } = 0.2;
    public double Priority { get; set; } = 0.1;
}

public class CacheSettings
{
    public int HealthCacheSeconds { get; set; } = 5;
    public int DecisionCacheMinutes { get; set; } = 5;
    public int MaxCacheSize { get; set; } = 1000;
}