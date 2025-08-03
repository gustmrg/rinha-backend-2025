using RinhaBackend.API.Domain.Entities;
using RinhaBackend.API.Domain.Results;
using RinhaBackend.API.Services.Interfaces;

namespace RinhaBackend.API.Services;

public class DefaultPaymentProcessor : IPaymentProcessor
{
    public Task<PaymentProcessingResult> ProcessPaymentAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}