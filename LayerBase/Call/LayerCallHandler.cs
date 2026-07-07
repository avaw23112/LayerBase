using LayerBase.Async;
using LayerBase.DI;
using LayerBase.Layers;

namespace LayerBase.Call;

/// <summary>
/// 跨层调用处理器的标记接口。
/// </summary>
public interface ILayerCallHandler
{
}

/// <summary>
/// 跨层调用处理器的泛型接口。处理 TRequest 请求并返回 TResponse 响应。
/// </summary>
public interface ILayerCallHandler<TRequest, TResponse> : ILayerCallHandler
    where TRequest : struct
    where TResponse : struct
{
    LBTask<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// 调用处理器的扩展方法，提供便捷的服务解析和跨层调用能力。
/// </summary>
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
        var layer = ServiceLayerBinder.Require(handler);
        if (layer.OwnerContext == null) throw new InvalidOperationException("Layer not attached to a runtime context.");
        return layer.OwnerContext.CallAsync<TLayer, TRequest, TResponse>(request, cancellationToken);
    }
}

public static class LayerCallRegistrationBridge
{
    public static void Register<TRequest, TResponse>(
        Layer                                  layer,
        ILayerCallHandler<TRequest, TResponse> handler)
        where TRequest : struct
        where TResponse : struct
    {
        if (layer == null) throw new ArgumentNullException(nameof(layer));
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        layer.RegisterCallHandler(handler);
    }
}