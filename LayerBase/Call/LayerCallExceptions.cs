namespace LayerBase.Call;

/// <summary>
/// 调用目标 Layer 未找到时抛出的异常。
/// </summary>
public sealed class LayerCallTargetNotFoundException : InvalidOperationException
{
    public LayerCallTargetNotFoundException(Type layerType)
        : base($"Target layer '{layerType?.Name}' is not built.")
    {
    }
}

/// <summary>
/// 调用目标 Layer 存在多个实例时抛出的异常。
/// </summary>
public sealed class LayerCallTargetAmbiguousException : InvalidOperationException
{
    public LayerCallTargetAmbiguousException(Type layerType)
        : base($"Target layer '{layerType?.Name}' is ambiguous because multiple instances are built.")
    {
    }
}

/// <summary>
/// 在 Layer 中未找到指定请求-响应类型的调用路由时抛出的异常。
/// </summary>
public sealed class LayerCallRouteNotFoundException : InvalidOperationException
{
    public LayerCallRouteNotFoundException(Type layerType, Type requestType, Type responseType)
        : base(
            $"Layer '{layerType?.Name}' has no Call handler for request '{requestType?.Name}' and response '{responseType?.Name}'.")
    {
    }
}

/// <summary>
/// 同一请求-响应对注册了多个处理器时抛出的异常。
/// </summary>
public sealed class LayerCallRouteConflictException : InvalidOperationException
{
    public LayerCallRouteConflictException(Type layerType, Type requestType, Type responseType,
                                           Type existingHandlerType,
                                           Type newHandlerType)
        : base(
            $"Layer '{layerType?.Name}' has duplicate Call handlers for request '{requestType?.Name}' and response '{responseType?.Name}': '{existingHandlerType?.Name}' and '{newHandlerType?.Name}'.")
    {
    }
}