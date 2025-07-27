using RinhaBackend.API.Entities;

namespace RinhaBackend.API.Interfaces;

public interface IPaymentProcessor
{
    Task<bool> IsHealthyAsync();
    Task ProcessPaymentAsync(Payment payment);
}