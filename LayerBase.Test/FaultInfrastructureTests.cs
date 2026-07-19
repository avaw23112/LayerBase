using LayerBase.Async;
using LayerBase.Scope;
using LayerBase.Worker;

namespace LayerBase.Test;

[TestFixture]
public class FaultInfrastructureTests
{
    [TearDown]
    public void TearDown()
    {
        LBTaskVoid.DefaultExceptionHandler = null;
    }

    [Test]
    public void Fault_inbox_tracks_capacity_and_high_watermark()
    {
        var inbox = new ScopeFaultInbox(maxCapacity: 1);

        Assert.That(inbox.TryEnqueue(CreateFault(scopeId: 1)), Is.True);
        Assert.That(inbox.TryEnqueue(CreateFault(scopeId: 2)), Is.False);

        Assert.That(inbox.Count, Is.EqualTo(1));
        Assert.That(inbox.DroppedCount, Is.EqualTo(1));
        Assert.That(inbox.CapacityExceededCount, Is.EqualTo(1));
        Assert.That(inbox.HighWatermark, Is.EqualTo(1));
    }

    [Test]
    public void Concurrent_fault_merge_is_atomic()
    {
        var inbox = new ScopeFaultInbox(maxCapacity: 4);
        ScopeFaultRecord record = CreateFault(scopeId: 1);
        using var start = new ManualResetEventSlim(false);
        var threads = new Thread[8];

        for (int i = 0; i < threads.Length; i++)
        {
            threads[i] = new Thread(() =>
            {
                start.Wait();
                for (int j = 0; j < 100; j++)
                {
                    Assert.That(inbox.TryEnqueue(in record), Is.True);
                }
            });
            threads[i].Start();
        }

        start.Set();
        foreach (Thread thread in threads)
        {
            thread.Join();
        }

        Assert.That(inbox.Count, Is.EqualTo(1));
        Assert.That(inbox.MergedCount, Is.EqualTo(799));
        Assert.That(inbox.HighWatermark, Is.EqualTo(1));
    }

    [Test]
    public void Fault_inbox_is_separate_from_completion_inbox()
    {
        var transport = new ScopeTransport(new ScopeAddress(1, 1, 2));

        for (int i = 0; i < 100; i++)
        {
            transport.EnqueueFault(CreateFault(scopeId: i + 1));
        }

        transport.EnqueueCompletion(
            ScopeCompletionEnvelope.WorkerExecutionStarted(WorkerHandle.Invalid));

        Assert.That(transport.FaultInbox.Count, Is.EqualTo(64));
        Assert.That(transport.FaultInbox.DroppedCount, Is.EqualTo(36));
        Assert.That(transport.CompletionInbox.Count, Is.EqualTo(1));
    }

    [Test]
    public void Worker_execute_exception_faults_item_and_worker_continues()
    {
        using var scheduler = new WorkerJobScheduler(new WorkerJobSchedulerOptions(1, 16, 8));
        var throwing = new ThrowingWorkerItem();
        var succeeding = new RecordingWorkerItem();

        Assert.That(scheduler.TryEnqueue(throwing), Is.True);
        Assert.That(throwing.InfrastructureFaulted.Wait(TimeSpan.FromSeconds(1)), Is.True);

        Assert.That(scheduler.TryEnqueue(succeeding), Is.True);
        Assert.That(succeeding.Executed.Wait(TimeSpan.FromSeconds(1)), Is.True);
        Assert.That(throwing.InfrastructureException, Is.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void Fire_and_forget_uses_default_fault_sink()
    {
        Exception? observed = null;
        using var signaled = new ManualResetEventSlim(false);
        LBTaskVoid.DefaultExceptionHandler = exception =>
        {
            observed = exception;
            signaled.Set();
        };

        _ = LBTaskVoid.Run(() => throw new InvalidOperationException("boom"));

        Assert.That(signaled.Wait(TimeSpan.FromSeconds(1)), Is.True);
        Assert.That(observed, Is.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void Throwing_fault_callback_is_contained_by_default_sink()
    {
        Exception? observed = null;
        using var signaled = new ManualResetEventSlim(false);
        LBTaskVoid.DefaultExceptionHandler = exception =>
        {
            observed = exception;
            signaled.Set();
        };

        _ = LBTaskVoid.Run(
            () => throw new InvalidOperationException("primary"),
            _ => throw new ApplicationException("callback"));

        Assert.That(signaled.Wait(TimeSpan.FromSeconds(1)), Is.True);
        Assert.That(observed, Is.TypeOf<ApplicationException>());
    }

    private static ScopeFaultRecord CreateFault(int scopeId)
    {
        return new ScopeFaultRecord(
            runtimeId: 1,
            runtimeGeneration: 1,
            sourceScopeId: scopeId,
            phase: ScopeFaultPhase.WorkerLoop,
            exception: new InvalidOperationException("fault"));
    }

    private sealed class ThrowingWorkerItem : IWorkerExecutionItem
    {
        public ManualResetEventSlim InfrastructureFaulted { get; } = new(false);

        public Exception? InfrastructureException { get; private set; }

        public void Execute(int workerIndex)
        {
            throw new InvalidOperationException("worker item failed");
        }

        public void CancelBeforeRun()
        {
        }

        public void FailInfrastructure(Exception exception)
        {
            InfrastructureException = exception;
            InfrastructureFaulted.Set();
        }
    }

    private sealed class RecordingWorkerItem : IWorkerExecutionItem
    {
        public ManualResetEventSlim Executed { get; } = new(false);

        public void Execute(int workerIndex)
        {
            Executed.Set();
        }

        public void CancelBeforeRun()
        {
        }

        public void FailInfrastructure(Exception exception)
        {
        }
    }
}
