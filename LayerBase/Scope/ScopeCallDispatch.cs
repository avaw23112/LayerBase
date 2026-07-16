using System.Collections.Concurrent;
using LayerBase.Async;
using LayerBase.Call;
using LayerBase.Core.Event;

namespace LayerBase.Scope;

internal interface IScopeCallCompletion
{
    void TrySetException(Exception exception);

    void TrySetCanceled(CancellationToken cancellationToken);
}

internal sealed class ScopeCallCompletion<TResponse> : IScopeCallCompletion
    where TResponse : struct
{
    private readonly LBTaskCompletionSource<TResponse> _source = new();

    internal LBTaskCompletionSource<TResponse> Source => _source;

    public LBTask<TResponse> Task => _source.Task;

    public void TrySetResult(TResponse response)
    {
        _source.TrySetResult(response);
    }

    public void TrySetException(Exception exception)
    {
        _source.TrySetException(exception);
    }

    public void TrySetCanceled(CancellationToken cancellationToken)
    {
        _source.TrySetCanceled(cancellationToken);
    }
}

internal readonly struct ScopeQueuedCall<TRequest, TResponse>
    where TRequest : struct
    where TResponse : struct
{
    public ScopeQueuedCall(
        TRequest request,
        ScopeCallCompletion<TResponse> completion,
        CancellationToken cancellationToken)
    {
        Request = request;
        Completion = completion;
        CancellationToken = cancellationToken;
    }

    public TRequest Request { get; }

    public ScopeCallCompletion<TResponse> Completion { get; }

    public CancellationToken CancellationToken { get; }
}

internal interface IScopeLocalCallDispatcher
{
    void Dispatch(
        int runtimeId,
        ScopeCallEnvelope envelope,
        EventPayloadStorage payloadStorage);
}

internal sealed class ScopeLocalCallDispatcher<TRequest, TResponse> : IScopeLocalCallDispatcher
    where TRequest : struct
    where TResponse : struct
{
    private readonly ScopeLocalCallInvoker<TRequest, TResponse> _invoker;

    public ScopeLocalCallDispatcher(ScopeLocalCallInvoker<TRequest, TResponse> invoker)
    {
        _invoker = invoker;
    }

    public void Dispatch(
        int runtimeId,
        ScopeCallEnvelope envelope,
        EventPayloadStorage payloadStorage)
    {
        if (!payloadStorage.TryGet<ScopeQueuedCall<TRequest, TResponse>>(
                runtimeId,
                envelope.Payload,
                out var queuedCall))
        {
            envelope.Completion?.TrySetException(
                new InvalidOperationException("Scope call payload is no longer available."));
            return;
        }

        if (queuedCall.CancellationToken.IsCancellationRequested)
        {
            queuedCall.Completion.TrySetCanceled(queuedCall.CancellationToken);
            return;
        }

        try
        {
            LBTask<TResponse> task =
                _invoker(
                    queuedCall.Request,
                    queuedCall.CancellationToken);

            if (task.GetAwaiter().IsCompleted)
            {
                CompleteSynchronously(
                    task,
                    queuedCall.Completion);
            }
            else
            {
                PendingScopeCallObservation.Observe(
                    task,
                    queuedCall.Completion);
            }
        }
        catch (Exception ex)
        {
            queuedCall.Completion.TrySetException(ex);
        }
    }

    private static void CompleteSynchronously(
        LBTask<TResponse> task,
        ScopeCallCompletion<TResponse> completion)
    {
        try
        {
            TResponse response = task.GetAwaiter().GetResult();
            completion.TrySetResult(response);
        }
        catch (OperationCanceledException ex)
        {
            completion.TrySetCanceled(ex.CancellationToken);
        }
        catch (Exception ex)
        {
            completion.TrySetException(ex);
        }
    }

    private sealed class PendingScopeCallObservation
    {
        private const int MaxPoolSize = 1024;

        private static readonly ConcurrentQueue<PendingScopeCallObservation> Pool = new();

        private static int _poolCount;

        private readonly Action _continuation;

        private LBTask<TResponse> _task;
        private ScopeCallCompletion<TResponse>? _completion;

        private PendingScopeCallObservation()
        {
            _continuation = Complete;
        }

        public static void Observe(
            LBTask<TResponse> task,
            ScopeCallCompletion<TResponse> completion)
        {
            if (!Pool.TryDequeue(out var observation))
            {
                observation = new PendingScopeCallObservation();
            }
            else
            {
                Interlocked.Decrement(ref _poolCount);
            }

            observation._task = task;
            observation._completion = completion;

            task.GetAwaiter().OnCompleted(
                observation._continuation);
        }

        private void Complete()
        {
            ScopeCallCompletion<TResponse>? completion =
                _completion;

            try
            {
                TResponse response =
                    _task.GetAwaiter().GetResult();

                completion?.TrySetResult(response);
            }
            catch (OperationCanceledException ex)
            {
                completion?.TrySetCanceled(
                    ex.CancellationToken);
            }
            catch (Exception ex)
            {
                completion?.TrySetException(ex);
            }
            finally
            {
                _task = default;
                _completion = null;

                if (Interlocked.Increment(ref _poolCount) <= MaxPoolSize)
                {
                    Pool.Enqueue(this);
                }
                else
                {
                    Interlocked.Decrement(ref _poolCount);
                }
            }
        }
    }
}
