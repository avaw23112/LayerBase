using LayerBase.Async;

namespace LayerBase.Scope;

internal enum ScopeControlResult : byte
{
    Succeeded = 0,
    Faulted = 1,
    Rejected = 2
}

internal readonly struct ScopeStopCall
{
}

internal readonly struct ScopeStopResponse
{
    public ScopeStopResponse(ScopeControlResult state)
    {
        State = state;
    }

    public ScopeControlResult State { get; }
}

internal readonly struct ScopeDisposeCall
{
}

internal readonly struct ScopeDisposeResponse
{
    public ScopeDisposeResponse(ScopeControlResult state)
    {
        State = state;
    }

    public ScopeControlResult State { get; }
}

internal static class ScopeLifecycleRouteIds
{
    public const int Stop = -101;

    public const int Dispose = -102;

    public static int Resolve<TRequest, TResponse>()
        where TRequest : struct
        where TResponse : struct
    {
        if (typeof(TRequest) == typeof(ScopeStopCall) &&
            typeof(TResponse) == typeof(ScopeStopResponse))
        {
            return Stop;
        }

        if (typeof(TRequest) == typeof(ScopeDisposeCall) &&
            typeof(TResponse) == typeof(ScopeDisposeResponse))
        {
            return Dispose;
        }

        throw new InvalidOperationException(
            $"Unsupported scope lifecycle control call {typeof(TRequest).Name}/{typeof(TResponse).Name}.");
    }
}

internal static class ScopeLifecycleControlExtensions
{
    public static LBTask<ScopeStopResponse> RequestStopAsync(this ScopeRuntime scope)
    {
        return scope.EnqueueControlCall<ScopeStopCall, ScopeStopResponse>(new ScopeStopCall());
    }

    public static LBTask<ScopeDisposeResponse> RequestDisposeAsync(this ScopeRuntime scope)
    {
        return scope.EnqueueControlCall<ScopeDisposeCall, ScopeDisposeResponse>(new ScopeDisposeCall());
    }
}
