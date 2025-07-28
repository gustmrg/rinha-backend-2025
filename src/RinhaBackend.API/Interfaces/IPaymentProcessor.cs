using RinhaBackend.API.Entities;

namespace RinhaBackend.API.Interfaces;

public interface IPaymentProcessor
{
    string ProcessorName { get; }
    
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
    Task ProcessPaymentAsync(Payment payment, CancellationToken cancellationToken = default);
}