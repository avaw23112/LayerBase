using LayerBase.Async;
using LayerBase.DI;
using LayerBase.Layers;

namespace LayerBase.Call;

public interface ILayerCallHandler
{
}

public interface ILayerCallHandler<TRequest, TResponse> : ILayerCallHandler
    where TRequest : struct
    where TResponse : struct
{
    LBTask<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}

public static class LayerCallHandlerExtensions
{
    public static TService Get<TService>(this ILayerCallHandler handler) where TService : class
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        return ServiceLayerBinder.Require(handler).GetService<TService>();
    }

    public static LBTask<TResponse> Call<TLayer, TRequest, TResponse>(this ILayerCallHandler handler, TRequest request,
                                                                       CancellationToken cancellationToken = default)
        where TLayer : Layer
        where TRequest : struct
        where TResponse : struct
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        return LayerHub.CallAsync<TLayer, TRequest, TResponse>(request, cancellationToken);
    }
}
