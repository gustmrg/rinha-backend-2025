using RinhaBackend.API.Entities;
using RinhaBackend.API.Interfaces;

namespace RinhaBackend.API.Services;

public class FallbackPaymentProcessor : IPaymentProcessor
{
    private readonly HttpClient _httpClient;
    
    public string ProcessorName => "Fallback";

    public FallbackPaymentProcessor(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        
        // HttpClient is now pre-configured in Program.cs via AddHttpClient<FallbackPaymentProcessor>
        // Validate that base address is set
        if (_httpClient.BaseAddress == null)
        {
            throw new InvalidOperationException("HttpClient BaseAddress is not configured for FallbackPaymentProcessor");
        }
    }
    
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

    public Task ProcessPaymentAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}