namespace RinhaBackend.API.Entities;

public class ProcessorHealthInfo
{
    public string ProcessorName { get; set; }
    public bool IsHealthy { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public DateTime LastChecked { get; set; }
    public double SuccessRate { get; set; }
    public decimal Cost { get; set; }
    public int Priority { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}