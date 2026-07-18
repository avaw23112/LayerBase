using LayerBase.Scope;
using LayerBase.Worker;

namespace LayerBase.Test;

[TestFixture]
[Category("ProductionHardening")]
public sealed class WorkerCompletionInboxTests
{
    [Test]
    public void Completion_inbox_accepts_when_event_inbox_is_full()
    {
        int wakeCount = 0;

        using var transport = new ScopeTransport(
            new ScopeAddress(
                runtimeId: 7001,
                runtimeGeneration: 1,
                scopeId: 0),
            () => Interlocked.Increment(ref wakeCount));

        var payload = new CompletionFloodEvent(1);

        for (int i = 0; i < 1024; i++)
        {
            ScopePostResult result = transport.EnqueueEvent(
                routeId: 9001,
                ScopeEventClass.Critical,
                in payload);

            Assert.That(result, Is.EqualTo(ScopePostResult.Accepted));
        }

        ScopePostResult rejected = transport.EnqueueEvent(
            routeId: 9001,
            ScopeEventClass.Critical,
            in payload);

        Assert.That(rejected, Is.EqualTo(ScopePostResult.QueueFull));

        var completion = ScopeCompletionEnvelope.WorkerCancelRequested(
            new WorkerHandle(index: 3, version: 7));

        transport.EnqueueCompletion(in completion);

        Assert.That(transport.CompletionInbox.Count, Is.EqualTo(1));
        Assert.That(wakeCount, Is.GreaterThan(0));
    }

    [Test]
    public void Completion_inbox_preserves_fifo_order()
    {
        using var transport = new ScopeTransport(
            new ScopeAddress(7002, 1, 0));

        var first = ScopeCompletionEnvelope.WorkerCancelRequested(
            new WorkerHandle(1, 1));

        var second = ScopeCompletionEnvelope.WorkerCancelRequested(
            new WorkerHandle(2, 1));

        transport.EnqueueCompletion(in first);
        transport.EnqueueCompletion(in second);

        Assert.That(
            transport.CompletionInbox.TryDequeue(out var actualFirst),
            Is.True);

        Assert.That(
            transport.CompletionInbox.TryDequeue(out var actualSecond),
            Is.True);

        Assert.That(actualFirst.WorkerHandle.Index, Is.EqualTo(1));
        Assert.That(actualSecond.WorkerHandle.Index, Is.EqualTo(2));
    }

    private readonly struct CompletionFloodEvent
    {
        public CompletionFloodEvent(int value)
        {
            Value = value;
        }

        public int Value { get; }
    }
}
