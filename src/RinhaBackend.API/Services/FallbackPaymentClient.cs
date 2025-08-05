using System.Text;
using System.Text.Json;
using RinhaBackend.API.Configurations;
using RinhaBackend.API.Domain.Entities;
using RinhaBackend.API.DTOs.Requests;

namespace RinhaBackend.API.Services;

public class FallbackPaymentClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public FallbackPaymentClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        
        _jsonOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = AppJsonSerializerContext.Default
        };
    }
    
    public async Task<HttpResponseMessage> PostPaymentAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "payments")
        {
            Content = new StringContent(JsonSerializer.Serialize(
                    new PaymentRequest(payment.CorrelationId, payment.Amount, payment.RequestedAt), _jsonOptions), 
                Encoding.UTF8, 
                "application/json")
        };

        return await _httpClient.SendAsync(request, cancellationToken);
    }
}