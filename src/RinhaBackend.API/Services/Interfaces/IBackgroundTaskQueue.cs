namespace RinhaBackend.API.Services.Interfaces;

public interface IBackgroundTaskQueue
{
    ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, IServiceProvider, ValueTask> workItem);
    ValueTask<Func<CancellationToken, IServiceProvider, ValueTask>> DequeueAsync(CancellationToken cancellationToken);
}