using RinhaBackend.API.Domain.Entities;
using RinhaBackend.API.Domain.Results;

namespace RinhaBackend.API.Services.Interfaces;

public interface IPaymentProcessor
{
    Task<PaymentProcessingResult> ProcessPaymentAsync(Payment payment, CancellationToken cancellationToken = default);
}