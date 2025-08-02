using RinhaBackend.API.DTOs.Requests;
using RinhaBackend.API.Entities;
using RinhaBackend.API.Interfaces;

namespace RinhaBackend.API.Services;

public class DefaultPaymentProcessor : IPaymentProcessor
{
    private readonly HttpClient _httpClient;
    
    public string ProcessorName => "Default";

    public DefaultPaymentProcessor(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        
        // HttpClient is now pre-configured in Program.cs via AddHttpClient<DefaultPaymentProcessor>
        // Validate that base address is set
        if (_httpClient.BaseAddress == null)
        {
            throw new InvalidOperationException("HttpClient BaseAddress is not configured for DefaultPaymentProcessor");
        }
    }

    // TODO: Validate timeout and retry policies
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _httpClient.GetAsync("/payments/service-health/");
            
            if (!result.IsSuccessStatusCode)
            {
                Console.WriteLine($"Health check failed with status code: {result.StatusCode}");
                return false;
            }

            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task ProcessPaymentAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        if (payment == null)
            throw new ArgumentNullException(nameof(payment));
        
        try
        {
            var request = new PaymentProcessorRequest(payment.CorrelationId, payment.Amount, payment.CreatedAt);
            
            var result = await _httpClient.PostAsJsonAsync("payments", request);
            
            var responseContent = await result.Content.ReadAsStringAsync();
            Console.WriteLine(responseContent);
            
            if (!result.IsSuccessStatusCode)
            {
                Console.WriteLine($"Payment processing failed with status code: {result.StatusCode}");
                Console.WriteLine($"Response content: {responseContent}");
                throw new HttpRequestException($"Payment processing failed with status {result.StatusCode}: {responseContent}");
            }
            
            Console.WriteLine($"Payment processed successfully for CorrelationId: {payment.CorrelationId}");
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Unexpected error processing payment: {e}");
            throw;
        }
    }
}