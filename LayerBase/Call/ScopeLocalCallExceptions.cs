namespace LayerBase.Call;

/// <summary>
/// 在 Layer 中未找到指定请求-响应类型的调用路由时抛出的异常。
/// </summary>
public sealed class ScopeLocalCallRouteNotFoundException : InvalidOperationException
{
    public ScopeLocalCallRouteNotFoundException(int scopeId, Type requestType, Type responseType)
        : base(
            $"Scope '{scopeId}' has no local Call handler for request '{requestType?.Name}' and response '{responseType?.Name}'.")
    {
    }
}

/// <summary>
/// 同一请求-响应对注册了多个处理器时抛出的异常。
/// </summary>
public sealed class ScopeLocalCallRouteConflictException : InvalidOperationException
{
    public ScopeLocalCallRouteConflictException(
        int scopeId,
        Type requestType,
        Type responseType,
        Type existingOwnerLayerType,
        Type existingHandlerType,
        Type newOwnerLayerType,
        Type newHandlerType)
        : base(
            $"Scope '{scopeId}' has duplicate local Call handlers for request '{requestType?.Name}' and response '{responseType?.Name}': '{existingOwnerLayerType?.Name}.{existingHandlerType?.Name}' and '{newOwnerLayerType?.Name}.{newHandlerType?.Name}'.")
    {
    }
}
