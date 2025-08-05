using RinhaBackend.API.Domain.Entities;
using RinhaBackend.API.Repositories.Interfaces;

namespace RinhaBackend.API.Repositories;

public class PaymentRepository : IPaymentRepository
{
    public async Task SavePaymentAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
    }
}