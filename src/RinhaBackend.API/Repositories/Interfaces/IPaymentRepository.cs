using RinhaBackend.API.Domain.Entities;

namespace RinhaBackend.API.Repositories.Interfaces;

public interface IPaymentRepository
{
    Task SavePaymentAsync(Payment payment, CancellationToken cancellationToken = default);
}