using System.Collections.Concurrent;
using LayerBase.Scope;

namespace LayerBase.Worker;

internal interface IWorkerExecutionItem
{
    void Execute(int executionLaneId);

    void CancelBeforeRun();

    void FailInfrastructure(Exception exception);
}

internal sealed class WorkerExecutionItem<TJob, TInput, TEvent> : IWorkerExecutionItem
    where TJob : struct, IWorkerEventJob<TInput, TEvent>
    where TInput : struct
    where TEvent : struct
{
    private static readonly ConcurrentQueue<WorkerExecutionItem<TJob, TInput, TEvent>> Pool = new();

    private static int s_poolCount;

    private ScopeEndpoint _origin;
    private WorkerHandle _handle;
    private TJob _job;
    private TInput _input;
    private WorkerEventJobOptions _options;
    private CancellationToken _token;

    internal static WorkerExecutionItem<TJob, TInput, TEvent> Rent(
        ScopeEndpoint origin,
        WorkerHandle handle,
        in TJob job,
        in TInput input,
        WorkerEventJobOptions options,
        CancellationToken token)
    {
        if (!Pool.TryDequeue(out var item))
        {
            item = new WorkerExecutionItem<TJob, TInput, TEvent>();
        }
        else
        {
            Interlocked.Decrement(ref s_poolCount);
        }

        item._origin = origin;
        item._handle = handle;
        item._job = job;
        item._input = input;
        item._options = options;
        item._token = token;

        return item;
    }

    public void Execute(int executionLaneId)
    {
        WorkerExecutionCompletedScopeEvent completion;

        if (_token.IsCancellationRequested)
        {
            completion = CreateCancelledCompletion();
        }
        else
        {
            SubmitExecutionStarted();

            try
            {
                var context = new WorkerJobContext(executionLaneId, _token);

                TEvent result = _job.Execute(in _input, in context);

                completion = _token.IsCancellationRequested
                    ? CreateCancelledCompletion()
                    : new WorkerExecutionCompletedScopeEvent(
                        _handle,
                        WorkerExecutionCompletionKind.Succeeded,
                        new WorkerExecutionResult<TEvent>(in result),
                        _options,
                        WorkerJobExceptionInfo.None);
            }
            catch (OperationCanceledException)
                when (_token.IsCancellationRequested)
            {
                completion = CreateCancelledCompletion();
            }
            catch (Exception ex)
            {
                completion = new WorkerExecutionCompletedScopeEvent(
                    _handle,
                    WorkerExecutionCompletionKind.Faulted,
                    result: null,
                    _options,
                    WorkerJobExceptionInfo.FromException(ex));
            }
        }

        try
        {
            SubmitCompletion(in completion);
        }
        finally
        {
            Return();
        }
    }

    public void CancelBeforeRun()
    {
        WorkerExecutionCompletedScopeEvent completion = CreateCancelledCompletion();

        try
        {
            SubmitCompletion(in completion);
        }
        finally
        {
            Return();
        }
    }

    public void FailInfrastructure(Exception exception)
    {
        WorkerExecutionCompletedScopeEvent completion =
            new(
                _handle,
                WorkerExecutionCompletionKind.Faulted,
                result: null,
                _options,
                WorkerJobExceptionInfo.FromException(exception));

        try
        {
            SubmitCompletion(in completion);
        }
        finally
        {
            Return();
        }
    }

    private void SubmitCompletion(
        in WorkerExecutionCompletedScopeEvent completion)
    {
        ScopeCompletionEnvelope envelope =
            ScopeCompletionEnvelope.WorkerExecutionCompleted(
                in completion);

        _origin.Transport.EnqueueCompletion(in envelope);
    }

    private void SubmitExecutionStarted()
    {
        ScopeCompletionEnvelope envelope =
            ScopeCompletionEnvelope.WorkerExecutionStarted(_handle);

        _origin.Transport.EnqueueCompletion(in envelope);
    }

    internal void ReturnWithoutExecution()
    {
        Return();
    }

    private WorkerExecutionCompletedScopeEvent CreateCancelledCompletion()
    {
        return new WorkerExecutionCompletedScopeEvent(
            _handle,
            WorkerExecutionCompletionKind.Cancelled,
            result: null,
            _options,
            WorkerJobExceptionInfo.None);
    }

    private void Return()
    {
        _origin = default;
        _handle = default;
        _job = default;
        _input = default;
        _options = default;
        _token = default;

        if (Interlocked.Increment(ref s_poolCount) <= 64)
        {
            Pool.Enqueue(this);
            return;
        }

        Interlocked.Decrement(ref s_poolCount);
    }
}
