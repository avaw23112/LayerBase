using System.Collections.Concurrent;

namespace LayerBase.Call;

public static class CallUsageTracker
{
    private static readonly ConcurrentBag<Type> s_usedRequestTypes = new();

    public static void RegisterUsed(Type requestType)
    {
        s_usedRequestTypes.Add(requestType);
    }

    public static IEnumerable<Type> GetUsedRequestTypes()
    {
        return s_usedRequestTypes.Distinct();
    }
}

