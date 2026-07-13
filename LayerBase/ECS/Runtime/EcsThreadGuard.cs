using System.Collections.Concurrent;

namespace LayerBase.ECS.Runtime;

internal static class EcsThreadGuard
{
    private static readonly ConcurrentDictionary<int, int> SchedulerThreads = new();
    [ThreadStatic]
    private static int _currentRuntimeId;
    [ThreadStatic]
    private static EcsResultQueue? _currentResultQueue;

    public static void Bind(int schedulerId, int threadId)
    {
        SchedulerThreads[schedulerId] = threadId;
    }

    public static void Unbind(int schedulerId)
    {
        SchedulerThreads.TryRemove(schedulerId, out _);
    }

    public static bool IsEcsThread(int schedulerId)
    {
        return SchedulerThreads.TryGetValue(schedulerId, out int threadId) &&
               threadId == Environment.CurrentManagedThreadId;
    }

    public static void EnterExecution(int runtimeId, EcsResultQueue results)
    {
        _currentRuntimeId = runtimeId;
        _currentResultQueue = results;
    }

    public static void ExitExecution(int runtimeId)
    {
        if (_currentRuntimeId != runtimeId)
        {
            return;
        }

        _currentRuntimeId = 0;
        _currentResultQueue = null;
    }

    public static bool TryGetCurrentResultQueue(int runtimeId, out EcsResultQueue? results)
    {
        if (_currentRuntimeId == runtimeId && _currentResultQueue != null)
        {
            results = _currentResultQueue;
            return true;
        }

        results = null;
        return false;
    }
}
