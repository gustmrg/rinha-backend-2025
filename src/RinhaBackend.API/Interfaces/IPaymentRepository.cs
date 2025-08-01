using RinhaBackend.API.DTOs.Requests;
using RinhaBackend.API.Entities;

namespace RinhaBackend.API.Interfaces;

public interface IPaymentRepository
{
    Task CreatePaymentAsync(Payment payment);
    Task<IEnumerable<Payment>> GetPaymentsAsync(DateTime from, DateTime to);
    Task<Payment?> GetPaymentByIdAsync(Guid paymentId);
    Task<Payment?> GetPaymentByCorrelationIdAsync(Guid correlationId);
    Task UpdatePaymentAsync(Payment payment);
}