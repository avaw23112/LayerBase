using System.Collections.Concurrent;
using LayerBase.Worker;

namespace LayerBase.Scope;

internal enum ScopeCompletionKind : byte
{
    WorkerExecutionCompleted = 0,
    WorkerCancelRequested = 1,
    WorkerExecutionStarted = 2
}

internal readonly struct ScopeCompletionEnvelope
{
    private ScopeCompletionEnvelope(
        ScopeCompletionKind kind,
        in WorkerExecutionCompletedScopeEvent workerCompletion,
        WorkerHandle workerHandle)
    {
        Kind = kind;
        WorkerCompletion = workerCompletion;
        WorkerHandle = workerHandle;
    }

    public ScopeCompletionKind Kind { get; }

    public WorkerExecutionCompletedScopeEvent WorkerCompletion { get; }

    public WorkerHandle WorkerHandle { get; }

    public static ScopeCompletionEnvelope WorkerExecutionCompleted(
        in WorkerExecutionCompletedScopeEvent completion)
    {
        return new ScopeCompletionEnvelope(
            ScopeCompletionKind.WorkerExecutionCompleted,
            in completion,
            WorkerHandle.Invalid);
    }

    public static ScopeCompletionEnvelope WorkerCancelRequested(
        WorkerHandle handle)
    {
        var emptyCompletion = default(
            WorkerExecutionCompletedScopeEvent);

        return new ScopeCompletionEnvelope(
            ScopeCompletionKind.WorkerCancelRequested,
            in emptyCompletion,
            handle);
    }

    public static ScopeCompletionEnvelope WorkerExecutionStarted(
        WorkerHandle handle)
    {
        var emptyCompletion = default(
            WorkerExecutionCompletedScopeEvent);

        return new ScopeCompletionEnvelope(
            ScopeCompletionKind.WorkerExecutionStarted,
            in emptyCompletion,
            handle);
    }
}

internal sealed class ScopeCompletionInbox
{
    private readonly ConcurrentQueue<ScopeCompletionEnvelope> _queue = new();
    private Action? _onAccepted;
    private int _count;

    public ScopeCompletionInbox(Action? onAccepted)
    {
        _onAccepted = onAccepted;
    }

    public int Count => Volatile.Read(ref _count);

    public void Enqueue(in ScopeCompletionEnvelope envelope)
    {
        _queue.Enqueue(envelope);
        Interlocked.Increment(ref _count);
        _onAccepted?.Invoke();
    }

    public bool TryDequeue(out ScopeCompletionEnvelope envelope)
    {
        if (!_queue.TryDequeue(out envelope))
            return false;

        Interlocked.Decrement(ref _count);
        return true;
    }

    public void ClearAcceptedCallback()
    {
        Volatile.Write(ref _onAccepted, null);
    }
}
