using System.Threading.Channels;
using TasraPostaManager.Core.Interfaces;

namespace TasraPostaManager.Services.Background;

/// <summary>
/// Channel tabanlı görev kuyruğu implementasyonu.
/// Bounded channel kullanarak bellek taşmasını önler.
/// </summary>
public sealed class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<BackgroundWorkItem> _channel;
    private int _pendingCount;

    public BackgroundTaskQueue(int capacity = 100)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _channel = Channel.CreateBounded<BackgroundWorkItem>(options);
    }

    public int PendingCount => _pendingCount;

    public async ValueTask QueueAsync(BackgroundWorkItem workItem, CancellationToken ct = default)
    {
        await _channel.Writer.WriteAsync(workItem, ct);
        Interlocked.Increment(ref _pendingCount);
    }

    public async ValueTask<BackgroundWorkItem> DequeueAsync(CancellationToken ct)
    {
        var item = await _channel.Reader.ReadAsync(ct);
        Interlocked.Decrement(ref _pendingCount);
        return item;
    }
}
