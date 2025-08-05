using RinhaBackend.API.Domain.Aggregates;
using RinhaBackend.API.Domain.Entities;

namespace RinhaBackend.API.Repositories.Interfaces;

public interface IPaymentRepository
{
    Task SavePaymentAsync(Payment payment, CancellationToken cancellationToken = default);
    Task<IEnumerable<PaymentSummary>> GetPaymentSummaryAsync(
        DateTime from, 
        DateTime to);
}