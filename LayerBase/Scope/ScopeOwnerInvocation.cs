using LayerBase.Async;

namespace LayerBase.Scope;

internal static class ScopeOwnerInvocation
{
    public static LBTask<TResponse> InvokeAsync<TRequest, TResponse>(
        ScopeRuntime scope,
        in TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
        if (scope == null)
            throw new ArgumentNullException(nameof(scope));

        if (scope.Options.Threading == ScopeThreadingMode.Worker)
        {
            return scope.EnqueueControlCall<TRequest, TResponse>(
                request,
                cancellationToken);
        }

        if (cancellationToken.IsCancellationRequested)
            return LBTask<TResponse>.FromCanceled(cancellationToken);

        try
        {
            scope.RequireOwnerThread();
            return LBTask<TResponse>.FromResult(InvokeLocal<TRequest, TResponse>(scope, in request));
        }
        catch (Exception ex)
        {
            return LBTask<TResponse>.FromException(ex);
        }
    }

    private static TResponse InvokeLocal<TRequest, TResponse>(
        ScopeRuntime scope,
        in TRequest request)
        where TRequest : struct
        where TResponse : struct
    {
        object boxed = request;

        if (boxed is ScopePrewarmCall prewarm &&
            typeof(TResponse) == typeof(ScopePrewarmResponse))
        {
            LayerBase.Core.Event.LayerPrewarmOptions options = prewarm.Options;
            scope.Prewarm(in options);
            return (TResponse)(object)new ScopePrewarmResponse(ScopeControlResult.Succeeded);
        }

        if (boxed is ScopeFreezeRuntimeRegistriesCall &&
            typeof(TResponse) == typeof(ScopeFreezeRuntimeRegistriesResponse))
        {
            scope.FreezeRuntimeRegistries();
            return (TResponse)(object)new ScopeFreezeRuntimeRegistriesResponse(ScopeControlResult.Succeeded);
        }

        if (boxed is ScopeRecompileTimerPlansCall &&
            typeof(TResponse) == typeof(ScopeRecompileTimerPlansResponse))
        {
            scope.CompileTimerPlans();
            return (TResponse)(object)new ScopeRecompileTimerPlansResponse(ScopeControlResult.Succeeded);
        }

        if (boxed is ScopeCaptureDiagnosticsCall &&
            typeof(TResponse) == typeof(ScopeCaptureDiagnosticsResponse))
        {
            return (TResponse)(object)new ScopeCaptureDiagnosticsResponse(
                ScopeControlResult.Succeeded,
                scope.CaptureDiagnosticsOnOwnerThread());
        }

        throw new InvalidOperationException(
            $"Unsupported local scope owner invocation {typeof(TRequest).Name}/{typeof(TResponse).Name}.");
    }
}
