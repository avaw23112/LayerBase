using System.Collections.Concurrent;
using System.Threading;

namespace LayerBase.Tools.Job;

/// <summary>
/// 固定线程数的轻量任务调度器。
/// </summary>
public sealed class JobScheduler : IDisposable
{
    private readonly BlockingCollection<Action> _jobs;
    private readonly Thread[] _workers;
    private int _disposed;

    public JobScheduler(int workerCount = 0, int queueCapacity = 0, string workerNamePrefix = "LayerBase.Job")
    {
        if (workerCount <= 0)
        {
            workerCount = Math.Max(1, Environment.ProcessorCount);
        }

        if (queueCapacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(queueCapacity));
        }

        if (string.IsNullOrWhiteSpace(workerNamePrefix))
        {
            throw new ArgumentException("Worker name prefix is required.", nameof(workerNamePrefix));
        }

        WorkerCount = workerCount;
        QueueCapacity = queueCapacity;
        _jobs = queueCapacity > 0
            ? new BlockingCollection<Action>(queueCapacity)
            : new BlockingCollection<Action>();

        _workers = new Thread[workerCount];
        for (int i = 0; i < workerCount; i++)
        {
            var worker = new Thread(WorkerLoop)
            {
                IsBackground = true,
                Name = $"{workerNamePrefix}-{i}"
            };
            _workers[i] = worker;
            worker.Start();
        }
    }

    public int WorkerCount { get; }
    public int QueueCapacity { get; }

    public bool TrySchedule(Action job)
    {
        if (job == null) throw new ArgumentNullException(nameof(job));
        if (Volatile.Read(ref _disposed) == 1)
        {
            return false;
        }

        try
        {
            return _jobs.TryAdd(job);
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public void Schedule(Action job)
    {
        if (!TrySchedule(job))
        {
            throw new InvalidOperationException("JobScheduler is full or disposed.");
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        _jobs.CompleteAdding();
        for (int i = 0; i < _workers.Length; i++)
        {
            _workers[i].Join();
        }
        _jobs.Dispose();
    }

    private void WorkerLoop()
    {
        foreach (var job in _jobs.GetConsumingEnumerable())
        {
            try
            {
                job();
            }
            catch
            {
                // keep worker alive
            }
        }
    }
}
