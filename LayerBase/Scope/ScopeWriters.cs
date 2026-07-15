using LayerBase.Core.Event;
using LayerBase.Async;

namespace LayerBase.Scope;

internal interface IScopeEventWriter
{
    ScopePostResult Post<TEvent>(in TEvent value)
        where TEvent : struct;
}

internal interface IScopeCallWriter
{
    LBTask<TResponse> Call<TRequest, TResponse>(
        in TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct;
}

internal sealed class RuntimeScopeEventWriter : IScopeEventWriter
{
    private WeakReference<ScopeRuntime>? _runtime;

    public void Attach(ScopeRuntime runtime)
    {
        _runtime = new WeakReference<ScopeRuntime>(runtime);
    }

    public void Detach()
    {
        _runtime = null;
    }

    public ScopePostResult Post<TEvent>(in TEvent value)
        where TEvent : struct
    {
        var target = _runtime;
        if (target == null || !target.TryGetTarget(out var runtime) || runtime.State == ScopeRuntimeState.Disposed)
            return ScopePostResult.RuntimeDisposed;

        return runtime.EnqueueEvent(in value);
    }
}

internal sealed class RuntimeScopeCallWriter : IScopeCallWriter
{
    private WeakReference<ScopeRuntime>? _runtime;

    public void Attach(ScopeRuntime runtime)
    {
        _runtime = new WeakReference<ScopeRuntime>(runtime);
    }

    public void Detach()
    {
        _runtime = null;
    }

    public LBTask<TResponse> Call<TRequest, TResponse>(
        in TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
        var target = _runtime;
        if (target == null || !target.TryGetTarget(out var runtime) || runtime.State == ScopeRuntimeState.Disposed)
            return LBTask<TResponse>.FromException(new ObjectDisposedException(nameof(ScopeRuntime)));

        return runtime.EnqueueCall<TRequest, TResponse>(in request, cancellationToken);
    }
}
