using RinhaBackend.API.Entities;

namespace RinhaBackend.API.Interfaces;

public interface IPaymentRepository
{
    Task CreatePaymentAsync(Payment payment);
    Task<Payment?> GetPaymentByIdAsync(Guid paymentId);
    Task<IEnumerable<Payment>> GetPaymentsByCorrelationIdAsync(Guid correlationId);
    Task UpdatePaymentAsync(Payment payment);
}