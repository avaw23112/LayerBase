using LayerBase.Async;

namespace LayerBase.Call;

/// <summary>
/// 当前 Scope 内本地调用的实际执行委托。
/// </summary>
internal delegate LBTask<TResponse> ScopeLocalCallInvoker<TRequest, TResponse>(
    TRequest          request,
    CancellationToken cancellationToken)
    where TRequest : struct
    where TResponse : struct;

internal static class ScopeLocalCallRouteRegistry
{
    private static int s_nextId;

    public static int GetNextId()
    {
        return Interlocked.Increment(ref s_nextId) - 1;
    }
}

internal static class ScopeLocalCallRouteId<TRequest, TResponse>
    where TRequest : struct
    where TResponse : struct
{
    public static readonly int Id = ScopeLocalCallRouteRegistry.GetNextId();
}
