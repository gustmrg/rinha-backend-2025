using RinhaBackend.API.Enums;

namespace RinhaBackend.API.Entities;

public class ProcessorHealthInfo
{
    public string ProcessorName { get; set; } = string.Empty;
    public ProcessorStatus Status { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public DateTime LastChecked { get; set; }
    public string? ErrorMessage { get; set; }
}