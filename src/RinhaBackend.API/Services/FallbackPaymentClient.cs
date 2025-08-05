using System.Text;
using System.Text.Json;
using RinhaBackend.API.Domain.Entities;
using RinhaBackend.API.DTOs.Requests;

namespace RinhaBackend.API.Services;

public class FallbackPaymentClient
{
    private readonly HttpClient _httpClient;

    public FallbackPaymentClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task<HttpResponseMessage> PostPaymentAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "payments")
        {
            Content = new StringContent(JsonSerializer.Serialize(
                    new PaymentRequest(payment.CorrelationId, payment.Amount, payment.RequestedAt)), 
                Encoding.UTF8, 
                "application/json")
        };

        return await _httpClient.SendAsync(request, cancellationToken);
    }
}