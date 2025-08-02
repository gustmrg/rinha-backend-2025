using RinhaBackend.API.Interfaces;
using RinhaBackend.API.Services;

namespace RinhaBackend.API.Factories;

public class PaymentProcessorFactory : IPaymentProcessorFactory
{
    private readonly IServiceProvider _serviceProvider;

    public PaymentProcessorFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public IPaymentProcessor Create(string processorName)
    {
        return processorName switch
        {
            "default" => _serviceProvider.GetRequiredService<DefaultPaymentProcessor>(),
            "fallback" => _serviceProvider.GetRequiredService<FallbackPaymentProcessor>(),
            _ => throw new ArgumentException($"Unknown processor name: {processorName}", nameof(processorName))
        };
    }
}