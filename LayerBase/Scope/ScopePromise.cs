using System;
using System.Runtime.CompilerServices;
using LayerBase.Scope.Completion;

namespace LayerBase.Scope;

public sealed class ScopePromise<TResult> : IScopePromise, IScopePromiseControl
{
    private readonly object _gate = new();
    private readonly ScopeRuntime? _continuationScope;
    private bool _completed;
    private TResult? _result;
    private Exception? _exception;
    private Action? _continuation;
    private bool _cancelled;
    private bool _accepted = true;

    internal ScopePromise(ScopeRuntime? continuationScope)
    {
        _continuationScope = continuationScope;
        if (continuationScope != null)
        {
            if (!continuationScope.CompletionPort.TryReserve(this))
            {
                _accepted = false;
                _completed = true;
                _cancelled = true;
                _exception = new ScopeBackpressureException(
                    $"Scope '{continuationScope.Descriptor.Name}' completion capacity is exhausted.");
                return;
            }

            if (!continuationScope.AwaitRegistry.TryRegister(this))
            {
                continuationScope.CompletionPort.Release(this);
                _accepted = false;
                _completed = true;
                _cancelled = true;
                _exception = new InvalidOperationException("Scope is shutting down, call cannot be registered.");
            }
        }
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

    bool IScopePromiseControl.IsCompleted
    {
        get
        {
            lock (_gate) return _completed;
        }
    }

    bool IScopePromiseControl.IsCancelled
    {
        get
        {
            lock (_gate) return _cancelled;
        }
    }

    bool IScopePromiseControl.TrySetResult(object? result)
    {
        if (result is TResult typed)
        {
            return Complete(typed, null);
        }

        if (result == null && default(TResult) == null)
        {
            return Complete(default, null);
        }

        return false;
    }

    bool IScopePromiseControl.TrySetException(Exception exception)
    {
        if (exception == null) return false;
        return Complete(default, exception);
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

    public void RequireAccepted()
    {
        if (_accepted)
        {
            return;
        }

        lock (_gate)
        {
            if (_exception != null)
            {
                throw _exception;
            }
        }

        throw new ScopeBackpressureException("Scope call was rejected before acceptance.");
    }

    public TResult GetResult()
    {
        bool unregister = false;
        try
        {
            TResult result;
            lock (_gate)
            {
                if (!_completed)
                {
                    throw new InvalidOperationException("Scope call has not completed.");
                }

                unregister = true;
                if (_cancelled)
                {
                    throw new InvalidOperationException("Scope call was cancelled.");
                }

                if (_exception != null)
                {
                    throw _exception;
                }

                result = _result!;
            }

            return result;
        }
        finally
        {
            if (unregister)
            {
                _continuationScope?.CompletionPort.Release(this);
                _continuationScope?.AwaitRegistry.Unregister(this);
            }
        }
    }

    public void SetResult(TResult result)
    {
        _ = Complete(result, null);
    }

    public void SetException(Exception exception)
    {
        if (exception == null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        _ = Complete(default, exception);
    }

    private bool Complete(TResult? result, Exception? exception)
    {
        Action? continuation;
        lock (_gate)
        {
            if (_completed)
            {
                return false;
            }

            _completed = true;
            _cancelled = false;
            _result = result;
            _exception = exception;
            continuation = _continuation;
            _continuation = null;
        }

        if (continuation != null)
        {
            ScheduleContinuation(continuation);
        }
        else
        {
            _continuationScope?.CompletionPort.Release(this);
        }

        return true;
    }

    private void ScheduleContinuation(Action continuation)
    {
        if (_continuationScope == null)
        {
            continuation();
            return;
        }

        if (!_continuationScope.CompletionPort.TryPublishCompleted(this, continuation))
        {
            if (!_continuationScope.CompletionPort.TryReserve(this) ||
                !_continuationScope.CompletionPort.TryPublishCompleted(this, continuation))
            {
                TrySetSchedulingFailure();
                _continuationScope.CompletionPort.Release(this);
                _continuationScope.AwaitRegistry.Unregister(this);
                return;
            }
        }

        _continuationScope.AwaitRegistry.Unregister(this);
    }

    private void TrySetSchedulingFailure()
    {
        lock (_gate)
        {
            if (_cancelled || _exception != null)
            {
                return;
            }

            _cancelled = true;
            _result = default;
            _exception = new InvalidOperationException(
                "Scope completion port rejected a reserved continuation.");
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
