using System.Threading.Channels;
using RinhaBackend.API.Services.Interfaces;

namespace RinhaBackend.API.Services;

public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<Func<CancellationToken, IServiceProvider, ValueTask>> _queue;
    
    public BackgroundTaskQueue(int capacity = 1000)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        
        _queue = Channel.CreateBounded<Func<CancellationToken, IServiceProvider, ValueTask>>(options);
    }
    
    public async ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, IServiceProvider, ValueTask> workItem)
    {
        if (workItem == null)
        {
            throw new ArgumentNullException(nameof(workItem));
        }
        
        await _queue.Writer.WriteAsync(workItem);
    }

    public async ValueTask<Func<CancellationToken, IServiceProvider, ValueTask>> DequeueAsync(CancellationToken cancellationToken)
    {
        var workItem = await _queue.Reader.ReadAsync(cancellationToken);
        return workItem;
    }
}