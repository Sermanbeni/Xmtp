using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public sealed class AsyncQueue<T>
{
    private readonly Queue<T> _queue = new();
    private readonly SemaphoreSlim _signal = new(0);

    public void Enqueue(T item)
    {
        lock (_queue)
        {
            _queue.Enqueue(item);
        }

        _signal.Release();
    }

    public async Task<T> DequeueAsync(CancellationToken cancellationToken = default)
    {
        await _signal.WaitAsync(cancellationToken).ConfigureAwait(false);

        lock (_queue)
        {
            return _queue.Dequeue();
        }
    }

    public int Count
    {
        get
        {
            lock (_queue)
            {
                return _queue.Count;
            }
        }
    }
}
