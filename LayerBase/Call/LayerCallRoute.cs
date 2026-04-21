using LayerBase.Async;

namespace LayerBase.Call;

internal delegate LBTask<TResponse> LayerCallInvoker<TRequest, TResponse>(
    TRequest request,
    CancellationToken cancellationToken)
    where TRequest : struct
    where TResponse : struct;

internal static class LayerCallRouteRegistry
{
    private static int s_nextId;

    public static int GetNextId()
    {
        return Interlocked.Increment(ref s_nextId) - 1;
    }
}

internal static class LayerCallRouteId<TRequest, TResponse>
    where TRequest : struct
    where TResponse : struct
{
    public static readonly int Id = LayerCallRouteRegistry.GetNextId();
}
