using System.Collections.Concurrent;
using LayerBase.Lifetime;
using LayerBase.Scope;
using LayerBase.Worker;

namespace LayerBase.Test;

[TestFixture]
[Category("ProductionHardening")]
public sealed class WorkerJobSchedulerThreadPoolTests
{
    [Test]
    public void Scheduler_creates_no_dedicated_threads()
    {
        string source = File.ReadAllText(
            Path.Combine(FindRepoRoot(), "LayerBase", "Worker", "WorkerJobScheduler.cs"));

        Assert.That(source, Does.Contain("ThreadPool.UnsafeQueueUserWorkItem"));
        Assert.That(source, Does.Not.Contain("new Thread"));
        Assert.That(source, Does.Not.Contain("ThreadStart"));
        Assert.That(source, Does.Not.Contain("Thread[]"));
        Assert.That(source, Does.Not.Contain(".Join("));
        Assert.That(source, Does.Not.Contain(".WaitOne("));
        Assert.That(source, Does.Not.Contain("Thread.Sleep"));
        Assert.That(source, Does.Not.Contain("SpinWait"));
        Assert.That(source, Does.Not.Contain("StartThreadsOnce"));
        Assert.That(source, Does.Not.Contain("WorkerLoop"));
    }

    [Test]
    public void Queue_capacity_is_enforced()
    {
        using var scheduler = CreateScheduler(
            maxConcurrency: 1,
            queueCapacity: 1,
            maxBatchItems: 16);
        using var release = new ManualResetEventSlim(false);
        var running = new BlockingWorkerItem(release);
        var pending = new RecordingWorkerItem();

        Assert.That(scheduler.TryEnqueue(running), Is.True);
        Assert.That(running.Started.Wait(TimeSpan.FromSeconds(2)), Is.True);
        Assert.That(scheduler.TryEnqueue(pending), Is.True);
        Assert.That(scheduler.TryEnqueue(new RecordingWorkerItem()), Is.False);

        release.Set();

        ShutdownDeadline deadline = ShutdownDeadline.Start(TimeSpan.FromSeconds(5));
        Assert.That(scheduler.Drain(in deadline), Is.EqualTo(LifetimeDrainResult.Drained));
        Assert.That(pending.ExecutedCount, Is.EqualTo(1));
    }

    [Test]
    public void Multiple_producers_and_consumers_execute_all_jobs()
    {
        using var scheduler = CreateScheduler(
            maxConcurrency: 4,
            queueCapacity: 512,
            maxBatchItems: 8);
        var seen = new ConcurrentDictionary<int, byte>();
        const int producerCount = 4;
        const int jobsPerProducer = 50;
        var producers = new Task[producerCount];

        for (int producer = 0; producer < producerCount; producer++)
        {
            int producerId = producer;
            producers[producer] = Task.Run(() =>
            {
                for (int i = 0; i < jobsPerProducer; i++)
                {
                    int id = producerId * jobsPerProducer + i;
                    Assert.That(scheduler.TryEnqueue(new UniqueWorkerItem(id, seen)), Is.True);
                }
            });
        }

        Task.WaitAll(producers);

        ShutdownDeadline deadline = ShutdownDeadline.Start(TimeSpan.FromSeconds(5));
        Assert.That(scheduler.Drain(in deadline), Is.EqualTo(LifetimeDrainResult.Drained));
        Assert.That(seen.Count, Is.EqualTo(producerCount * jobsPerProducer));
    }

    [Test]
    public void Runner_count_never_exceeds_max_concurrency()
    {
        using var scheduler = CreateScheduler(
            maxConcurrency: 2,
            queueCapacity: 32,
            maxBatchItems: 32);
        using var release = new ManualResetEventSlim(false);
        var probe = new ConcurrentProbe();

        for (int i = 0; i < 16; i++)
            Assert.That(scheduler.TryEnqueue(new ConcurrentWorkerItem(probe, release)), Is.True);

        Assert.That(probe.WaitForStarted(2, TimeSpan.FromSeconds(2)), Is.True);
        Assert.That(probe.MaxObserved, Is.LessThanOrEqualTo(2));

        release.Set();

        ShutdownDeadline deadline = ShutdownDeadline.Start(TimeSpan.FromSeconds(5));
        Assert.That(scheduler.Drain(in deadline), Is.EqualTo(LifetimeDrainResult.Drained));
        Assert.That(probe.MaxObserved, Is.LessThanOrEqualTo(2));
        Assert.That(probe.ExecutedCount, Is.EqualTo(16));
    }

    [Test]
    public void Low_frequency_long_jobs_expand_to_available_concurrency()
    {
        using var scheduler = CreateScheduler(
            maxConcurrency: 2,
            queueCapacity: 8,
            maxBatchItems: 16);
        using var firstRelease = new ManualResetEventSlim(false);
        using var secondRelease = new ManualResetEventSlim(false);
        var first = new BlockingWorkerItem(firstRelease);
        var second = new BlockingWorkerItem(secondRelease);

        try
        {
            Assert.That(scheduler.TryEnqueue(first), Is.True);
            Assert.That(first.Started.Wait(TimeSpan.FromSeconds(2)), Is.True);

            Assert.That(scheduler.TryEnqueue(second), Is.True);

            Assert.That(second.Started.Wait(TimeSpan.FromMilliseconds(500)), Is.True,
                "A queued job should start a new runner while another runner is executing and MaxConcurrency is available.");
        }
        finally
        {
            firstRelease.Set();
            secondRelease.Set();
        }

        ShutdownDeadline deadline = ShutdownDeadline.Start(TimeSpan.FromSeconds(5));
        Assert.That(scheduler.Drain(in deadline), Is.EqualTo(LifetimeDrainResult.Drained));
        Assert.That(first.ExecutedCount, Is.EqualTo(1));
        Assert.That(second.ExecutedCount, Is.EqualTo(1));
    }

    [Test]
    public void Runner_reschedules_after_batch_budget()
    {
        using var scheduler = CreateScheduler(
            maxConcurrency: 1,
            queueCapacity: 64,
            maxBatchItems: 1);
        var executed = 0;

        for (int i = 0; i < 32; i++)
            Assert.That(scheduler.TryEnqueue(new ActionWorkerItem(_ => Interlocked.Increment(ref executed))), Is.True);

        ShutdownDeadline deadline = ShutdownDeadline.Start(TimeSpan.FromSeconds(5));
        Assert.That(scheduler.Drain(in deadline), Is.EqualTo(LifetimeDrainResult.Drained));
        Assert.That(executed, Is.EqualTo(32));
    }

    [Test]
    public void Normal_stop_drains_accepted_jobs_and_rejects_new_jobs()
    {
        using var scheduler = CreateScheduler(
            maxConcurrency: 1,
            queueCapacity: 8,
            maxBatchItems: 16);
        using var release = new ManualResetEventSlim(false);
        var running = new BlockingWorkerItem(release);
        var accepted = new RecordingWorkerItem();

        Assert.That(scheduler.TryEnqueue(running), Is.True);
        Assert.That(running.Started.Wait(TimeSpan.FromSeconds(2)), Is.True);
        Assert.That(scheduler.TryEnqueue(accepted), Is.True);

        scheduler.RequestStop();

        Assert.That(scheduler.TryEnqueue(new RecordingWorkerItem()), Is.False);

        release.Set();

        ShutdownDeadline deadline = ShutdownDeadline.Start(TimeSpan.FromSeconds(5));
        Assert.That(scheduler.Drain(in deadline), Is.EqualTo(LifetimeDrainResult.Drained));
        Assert.That(running.ExecutedCount, Is.EqualTo(1));
        Assert.That(accepted.ExecutedCount, Is.EqualTo(1));
        Assert.That(running.CancelledCount + accepted.CancelledCount, Is.EqualTo(0));
    }

    [Test]
    public void Fault_abort_cancels_pending_jobs()
    {
        using var scheduler = CreateScheduler(
            maxConcurrency: 1,
            queueCapacity: 8,
            maxBatchItems: 16);
        using var release = new ManualResetEventSlim(false);
        var running = new BlockingWorkerItem(release);
        var pending = Enumerable.Range(0, 4)
            .Select(_ => new RecordingWorkerItem())
            .ToArray();

        Assert.That(scheduler.TryEnqueue(running), Is.True);
        Assert.That(running.Started.Wait(TimeSpan.FromSeconds(2)), Is.True);

        foreach (var item in pending)
            Assert.That(scheduler.TryEnqueue(item), Is.True);

        scheduler.AbortPending();
        release.Set();

        ShutdownDeadline deadline = ShutdownDeadline.Start(TimeSpan.FromSeconds(5));
        Assert.That(scheduler.Drain(in deadline), Is.EqualTo(LifetimeDrainResult.Drained));
        Assert.That(running.ExecutedCount, Is.EqualTo(1));
        Assert.That(pending.Sum(item => item.CancelledCount), Is.EqualTo(4));
        Assert.That(pending.Sum(item => item.ExecutedCount), Is.EqualTo(0));
    }

    [Test]
    public void Execution_lane_ids_are_bounded()
    {
        const int maxConcurrency = 3;
        using var scheduler = CreateScheduler(
            maxConcurrency: maxConcurrency,
            queueCapacity: 64,
            maxBatchItems: 4);
        var lanes = new ConcurrentBag<int>();

        for (int i = 0; i < 48; i++)
            Assert.That(scheduler.TryEnqueue(new ActionWorkerItem(lane => lanes.Add(lane))), Is.True);

        ShutdownDeadline deadline = ShutdownDeadline.Start(TimeSpan.FromSeconds(5));
        Assert.That(scheduler.Drain(in deadline), Is.EqualTo(LifetimeDrainResult.Drained));
        Assert.That(lanes, Is.Not.Empty);
        Assert.That(lanes.All(lane => lane >= 0 && lane < maxConcurrency), Is.True);
    }

    [Test]
    public void Dispose_reports_timeout_when_jobs_are_still_running()
    {
        var scheduler = CreateScheduler(
            maxConcurrency: 1,
            queueCapacity: 8,
            maxBatchItems: 16,
            shutdownTotalTimeoutMilliseconds: 100);
        using var release = new ManualResetEventSlim(false);
        var running = new BlockingWorkerItem(release);

        try
        {
            Assert.That(scheduler.TryEnqueue(running), Is.True);
            Assert.That(running.Started.Wait(TimeSpan.FromSeconds(2)), Is.True);

            Assert.Throws<TimeoutException>(() => scheduler.Dispose());
        }
        finally
        {
            release.Set();
        }

        ShutdownDeadline deadline = ShutdownDeadline.Start(TimeSpan.FromSeconds(5));
        Assert.That(scheduler.Drain(in deadline), Is.EqualTo(LifetimeDrainResult.Drained));
    }

    private static WorkerJobScheduler CreateScheduler(
        int maxConcurrency,
        int queueCapacity,
        int maxBatchItems,
        int? shutdownTotalTimeoutMilliseconds = null)
    {
        return new WorkerJobScheduler(
            new WorkerJobSchedulerOptions(
                maxConcurrency,
                queueCapacity,
                maxBatchItems,
                TimeSpan.FromMilliseconds(50))
            {
                ShutdownTotalTimeoutMilliseconds = shutdownTotalTimeoutMilliseconds ?? 15000
            });
    }

    private static string FindRepoRoot()
    {
        DirectoryInfo directory = new(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "LayerBase.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find LayerBase.sln.");
    }

    private class RecordingWorkerItem : IWorkerExecutionItem
    {
        public int ExecutedCount;
        public int CancelledCount;

        public virtual void Execute(int executionLaneId)
        {
            Interlocked.Increment(ref ExecutedCount);
        }

        public virtual void CancelBeforeRun()
        {
            Interlocked.Increment(ref CancelledCount);
        }

        public virtual void FailInfrastructure(Exception exception)
        {
        }
    }

    private sealed class BlockingWorkerItem : RecordingWorkerItem
    {
        private readonly ManualResetEventSlim _release;

        public BlockingWorkerItem(ManualResetEventSlim release)
        {
            _release = release;
        }

        public ManualResetEventSlim Started { get; } = new(false);

        public override void Execute(int executionLaneId)
        {
            Started.Set();
            _release.Wait();
            base.Execute(executionLaneId);
        }
    }

    private sealed class UniqueWorkerItem : IWorkerExecutionItem
    {
        private readonly int _id;
        private readonly ConcurrentDictionary<int, byte> _seen;

        public UniqueWorkerItem(int id, ConcurrentDictionary<int, byte> seen)
        {
            _id = id;
            _seen = seen;
        }

        public void Execute(int executionLaneId)
        {
            if (!_seen.TryAdd(_id, 0))
                throw new InvalidOperationException("Duplicate execution.");
        }

        public void CancelBeforeRun()
        {
        }

        public void FailInfrastructure(Exception exception)
        {
            Assert.Fail(exception.Message);
        }
    }

    private sealed class ActionWorkerItem : IWorkerExecutionItem
    {
        private readonly Action<int> _execute;

        public ActionWorkerItem(Action<int> execute)
        {
            _execute = execute;
        }

        public void Execute(int executionLaneId)
        {
            _execute(executionLaneId);
        }

        public void CancelBeforeRun()
        {
        }

        public void FailInfrastructure(Exception exception)
        {
            Assert.Fail(exception.Message);
        }
    }

    private sealed class ConcurrentWorkerItem : IWorkerExecutionItem
    {
        private readonly ConcurrentProbe _probe;
        private readonly ManualResetEventSlim _release;

        public ConcurrentWorkerItem(ConcurrentProbe probe, ManualResetEventSlim release)
        {
            _probe = probe;
            _release = release;
        }

        public void Execute(int executionLaneId)
        {
            _probe.Enter();
            try
            {
                _release.Wait();
            }
            finally
            {
                _probe.Exit();
            }
        }

        public void CancelBeforeRun()
        {
        }

        public void FailInfrastructure(Exception exception)
        {
            Assert.Fail(exception.Message);
        }
    }

    private sealed class ConcurrentProbe
    {
        private int _current;
        private int _started;
        private int _maxObserved;
        private readonly ManualResetEventSlim _startedSignal = new(false);

        public int ExecutedCount => Volatile.Read(ref _started);

        public int MaxObserved => Volatile.Read(ref _maxObserved);

        public void Enter()
        {
            int current = Interlocked.Increment(ref _current);
            int started = Interlocked.Increment(ref _started);
            UpdateMax(current);

            if (started >= 2)
                _startedSignal.Set();
        }

        public void Exit()
        {
            Interlocked.Decrement(ref _current);
        }

        public bool WaitForStarted(int count, TimeSpan timeout)
        {
            if (Volatile.Read(ref _started) >= count)
                return true;

            return _startedSignal.Wait(timeout);
        }

        private void UpdateMax(int current)
        {
            while (true)
            {
                int observed = Volatile.Read(ref _maxObserved);
                if (observed >= current)
                    return;

                if (Interlocked.CompareExchange(ref _maxObserved, current, observed) == observed)
                    return;
            }
        }
    }
}
