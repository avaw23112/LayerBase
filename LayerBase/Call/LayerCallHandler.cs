using LayerBase.Async;
using LayerBase.DI;
using LayerBase.Layers;

namespace LayerBase.Call;

/// <summary>
/// 当前 Scope 内本地调用处理器的标记接口。
/// </summary>
public interface IScopeLocalCallHandler
{
}

/// <summary>
/// 当前 Scope 内本地调用处理器的泛型接口。处理 TRequest 请求并返回 TResponse 响应。
/// </summary>
public interface IScopeLocalCallHandler<TRequest, TResponse> : IScopeLocalCallHandler
    where TRequest : struct
    where TResponse : struct
{
    LBTask<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// 调用处理器的扩展方法，提供便捷的服务解析和当前 Scope 本地调用能力。
/// </summary>
public static class ScopeLocalCallHandlerExtensions
{
    public static TService Get<TService>(this IScopeLocalCallHandler handler) where TService : class
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        return ServiceLayerBinder.Require(handler).GetService<TService>();
    }

    public static LBTask<TResponse> Call<TRequest, TResponse>(
        this IScopeLocalCallHandler handler,
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        return ServiceLayerBinder.RequireBinding(handler)
                                 .OwnerScope
                                 .CallLocalAsync<TRequest, TResponse>(request, cancellationToken);
    }

    public static LBTask<TResponse> Call<TRequest, TResponse>(
        this IService service,
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
        if (service == null) throw new ArgumentNullException(nameof(service));
        return ServiceLayerBinder.RequireBinding(service)
                                 .OwnerScope
                                 .CallLocalAsync<TRequest, TResponse>(request, cancellationToken);
    }

    public static LBTask<TResponse> Call<TRequest, TResponse>(
        this ILayerContext context,
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        return ServiceLayerBinder.RequireBinding(context)
                                 .OwnerScope
                                 .CallLocalAsync<TRequest, TResponse>(request, cancellationToken);
    }
}

public static class ScopeLocalCallRegistrationBridge
{
    public static void Register<TRequest, TResponse>(
        Layer                                  layer,
        IScopeLocalCallHandler<TRequest, TResponse> handler)
        where TRequest : struct
        where TResponse : struct
    {
        if (layer == null) throw new ArgumentNullException(nameof(layer));
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        layer.RegisterCallHandler(handler);
    }

    public static void RegisterForOwner<TRequest, TResponse>(
        Layer layer,
        object owner,
        IScopeLocalCallHandler<TRequest, TResponse> handler)
        where TRequest : struct
        where TResponse : struct
    {
        if (layer == null) throw new ArgumentNullException(nameof(layer));
        if (owner == null) throw new ArgumentNullException(nameof(owner));
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        layer.RegisterCallHandlerForOwner(owner, handler);
    }
}
