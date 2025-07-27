using RinhaBackend.API.Entities;
using RinhaBackend.API.Interfaces;

namespace RinhaBackend.API.Services;

public class FallbackPaymentProcessor : IPaymentProcessor
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public FallbackPaymentProcessor(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        
        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        var baseUrl = _configuration["Processors:Fallback:BaseUrl"];
        
        if (string.IsNullOrEmpty(baseUrl))
        {
            throw new ArgumentNullException(nameof(baseUrl), "Base Url for Fallback Payment Processor is not configured.");
        }
        
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }
    
    public async Task<bool> IsHealthyAsync()
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
        catch (OperationCanceledException ex)
        {
            throw;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public Task ProcessPaymentAsync(Payment payment)
    {
        throw new NotImplementedException();
    }
}