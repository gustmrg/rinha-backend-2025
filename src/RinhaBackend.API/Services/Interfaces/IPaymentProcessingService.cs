using RinhaBackend.API.Domain.Entities;
using RinhaBackend.API.Domain.Results;

namespace RinhaBackend.API.Services.Interfaces;

public interface IPaymentProcessingService
{
    Task<PaymentProcessingResult> ProcessPayment(Payment payment);
}