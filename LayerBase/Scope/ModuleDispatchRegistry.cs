namespace LayerBase.Scope;

public static class ModuleDispatchRegistry
{
    private static ModuleCallDispatchHandler[]? s_callDispatchers;
    private static ModuleEventDispatchHandler[]? s_eventDispatchers;

    public static void RegisterCallDispatchers(ModuleCallDispatchHandler[] dispatchers)
    {
        s_callDispatchers = dispatchers;
    }

    public static void RegisterEventDispatchers(ModuleEventDispatchHandler[] dispatchers)
    {
        s_eventDispatchers = dispatchers;
    }

    public static ModuleCallDispatchHandler[]? TryGetCallDispatchers(int expectedModuleCount)
    {
        if (s_callDispatchers != null && s_callDispatchers.Length == expectedModuleCount)
        {
            return s_callDispatchers;
        }

        return null;
    }

    public static ModuleEventDispatchHandler[]? TryGetEventDispatchers(int expectedModuleCount)
    {
        if (s_eventDispatchers != null && s_eventDispatchers.Length == expectedModuleCount)
        {
            return s_eventDispatchers;
        }

        return null;
    }
}
