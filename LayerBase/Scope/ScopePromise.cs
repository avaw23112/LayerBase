using System.Runtime.CompilerServices;

namespace LayerBase.Scope;

public sealed class ScopePromise<TResult> : IScopePromise
{
    private readonly object _gate = new();
    private readonly ScopeRuntime? _continuationScope;
    private bool _completed;
    private TResult? _result;
    private Exception? _exception;
    private Action? _continuation;

    internal ScopePromise(ScopeRuntime? continuationScope)
    {
        _continuationScope = continuationScope;
    }

    public bool IsCompleted
    {
        get
        {
            lock (_gate)
            {
                return _completed;
            }
        }
    }

    public void OnCompleted(Action continuation)
    {
        if (continuation == null)
        {
            throw new ArgumentNullException(nameof(continuation));
        }

        bool runNow;
        lock (_gate)
        {
            runNow = _completed;
            if (!runNow)
            {
                if (_continuation != null)
                {
                    throw new InvalidOperationException("ScopePromise only supports one continuation in the current runtime.");
                }

                _continuation = continuation;
                return;
            }
        }

        ScheduleContinuation(continuation);
    }

    public Awaiter GetAwaiter()
    {
        return new Awaiter(this);
    }

    public TResult GetResult()
    {
        lock (_gate)
        {
            if (!_completed)
            {
                throw new InvalidOperationException("Scope call has not completed.");
            }

            if (_exception != null)
            {
                throw _exception;
            }

            return _result!;
        }
    }

    public void SetResult(TResult result)
    {
        Complete(result, null);
    }

    public void SetException(Exception exception)
    {
        if (exception == null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        Complete(default, exception);
    }

    private void Complete(TResult? result, Exception? exception)
    {
        Action? continuation;
        lock (_gate)
        {
            if (_completed)
            {
                return;
            }

            _completed = true;
            _result = result;
            _exception = exception;
            continuation = _continuation;
            _continuation = null;
        }

        if (continuation != null)
        {
            ScheduleContinuation(continuation);
        }
    }

    private void ScheduleContinuation(Action continuation)
    {
        if (_continuationScope == null)
        {
            continuation();
            return;
        }

        if (!_continuationScope.TryEnqueueContinuation(continuation))
        {
            continuation();
        }
    }

    public readonly struct Awaiter : INotifyCompletion
    {
        private readonly ScopePromise<TResult> _promise;

        internal Awaiter(ScopePromise<TResult> promise)
        {
            _promise = promise ?? throw new ArgumentNullException(nameof(promise));
        }

        public bool IsCompleted => _promise.IsCompleted;

        public void OnCompleted(Action continuation)
        {
            _promise.OnCompleted(continuation);
        }

        public TResult GetResult()
        {
            return _promise.GetResult();
        }
    }
}
