using RinhaBackend.API.Services.Interfaces;

namespace RinhaBackend.API.Services;

public sealed class PaymentBackgroundService : BackgroundService
{
    private readonly IBackgroundTaskQueue _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentBackgroundService> _logger;

    public PaymentBackgroundService(
        IBackgroundTaskQueue queue,
        ILogger<PaymentBackgroundService> logger, 
        IServiceProvider serviceProvider) 
    {
        _queue = queue;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Payment Background Service is started");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var workItem = await _queue.DequeueAsync(stoppingToken);
                
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        await workItem(stoppingToken, scope.ServiceProvider);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Payment Background Service encountered an error");
                    }
                }, stoppingToken);
                
                _logger.LogInformation("Work item processed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment Background Service encountered an error");
            }
        }
    }
    
    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Payment Background Service is stopping");
        
        await base.StopAsync(stoppingToken);
    }
}