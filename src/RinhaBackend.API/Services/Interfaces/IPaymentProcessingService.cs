using RinhaBackend.API.Domain.Entities;
using RinhaBackend.API.Domain.Results;
using RinhaBackend.API.DTOs.Responses;

namespace RinhaBackend.API.Services.Interfaces;

public interface IPaymentProcessingService
{
    Task<PaymentProcessingResult> ProcessPayment(Payment payment);
    Task<PaymentSummaryResponse> GetPaymentSummaryAsync(DateTime from, DateTime to);
}