namespace LayerBase.Scope;

public readonly struct ScopeCallRoute
{
    public ScopeCallRoute(int scopeId, ushort moduleSlot, ushort localHandlerId, int serviceSlot)
    {
        ScopeId = scopeId;
        ModuleSlot = moduleSlot;
        LocalHandlerId = localHandlerId;
        ServiceSlot = serviceSlot;
    }

    public int ScopeId { get; }

    public ushort ModuleSlot { get; }

    public ushort LocalHandlerId { get; }

    public int ServiceSlot { get; }

    public bool IsValid => ScopeId >= 0 && ServiceSlot >= 0;
}

public readonly struct ScopeEventHandlerRoute
{
    public ScopeEventHandlerRoute(ushort moduleSlot, ushort localHandlerId, int serviceSlot)
    {
        ModuleSlot = moduleSlot;
        LocalHandlerId = localHandlerId;
        ServiceSlot = serviceSlot;
    }

    public ushort ModuleSlot { get; }

    public ushort LocalHandlerId { get; }

    public int ServiceSlot { get; }
}

public readonly struct ScopeEventRoute
{
    public ScopeEventRoute(int scopeId, int handlerStart, int handlerCount)
    {
        ScopeId = scopeId;
        HandlerStart = handlerStart;
        HandlerCount = handlerCount;
    }

    public int ScopeId { get; }

    public int HandlerStart { get; }

    public int HandlerCount { get; }
}

public delegate void ModuleCallDispatchHandler(ScopeRuntime scope, ushort localHandlerId, int serviceSlot, ScopeCallMessage message);

public delegate void ModuleEventDispatchHandler(ScopeRuntime scope, ushort localHandlerId, int serviceSlot, ScopePostMessage message);
