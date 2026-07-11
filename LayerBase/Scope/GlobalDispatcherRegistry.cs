namespace LayerBase.Scope;

public static class GlobalDispatcherRegistry
{
    public static ScopePostDispatcher? PostDispatcher { get; set; }
    public static ScopeCallDispatcher? CallDispatcher { get; set; }
}
