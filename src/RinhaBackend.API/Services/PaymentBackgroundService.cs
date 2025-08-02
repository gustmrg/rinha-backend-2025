using RinhaBackend.API.Services.Interfaces;

namespace RinhaBackend.API.Services;

public sealed class PaymentBackgroundService : BackgroundService
{
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentBackgroundService> _logger;

    public PaymentBackgroundService(
        IBackgroundTaskQueue taskQueue, 
        IServiceProvider serviceProvider,
        ILogger<PaymentBackgroundService> logger) 
    {
        _taskQueue = taskQueue;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Payment Background Service started");

        await ProcessTaskQueueAsync(stoppingToken);
    }
    
    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Payment Background Service is stopping");
        
        await base.StopAsync(stoppingToken);
    }
    
    private async Task ProcessTaskQueueAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var workItem =  await _taskQueue.DequeueAsync(stoppingToken);
                
                using var scope = _serviceProvider.CreateScope();
                await workItem(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing background work item");
            }
        }
    }
}