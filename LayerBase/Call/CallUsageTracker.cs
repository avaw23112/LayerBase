using System.Collections.Concurrent;

namespace LayerBase.Call;

/// <summary>
/// 跟踪所有已使用的跨层调用请求类型。用于拓扑审计中检测"死调用路由"。
/// </summary>
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