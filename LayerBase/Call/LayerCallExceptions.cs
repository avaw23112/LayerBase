namespace LayerBase.Call;

public sealed class LayerCallTargetNotFoundException : InvalidOperationException
{
    public LayerCallTargetNotFoundException(Type layerType)
        : base($"Target layer '{layerType?.Name}' is not built.")
    {
    }
}

public sealed class LayerCallTargetAmbiguousException : InvalidOperationException
{
    public LayerCallTargetAmbiguousException(Type layerType)
        : base($"Target layer '{layerType?.Name}' is ambiguous because multiple instances are built.")
    {
    }
}

public sealed class LayerCallRouteNotFoundException : InvalidOperationException
{
    public LayerCallRouteNotFoundException(Type layerType, Type requestType, Type responseType)
        : base(
            $"Layer '{layerType?.Name}' has no Call handler for request '{requestType?.Name}' and response '{responseType?.Name}'.")
    {
    }
}

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

