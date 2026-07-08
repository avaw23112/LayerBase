using System.Collections.Concurrent;

namespace LayerBase.ECS.Runtime;

internal static class EcsThreadGuard
{
    private static readonly ConcurrentDictionary<int, int> RuntimeThreads = new();
    [ThreadStatic]
    private static int _currentRuntimeId;
    [ThreadStatic]
    private static EcsResultQueue? _currentResultQueue;

    public static void Bind(int runtimeId, int threadId)
    {
        RuntimeThreads[runtimeId] = threadId;
    }

    public static void Unbind(int runtimeId)
    {
        RuntimeThreads.TryRemove(runtimeId, out _);
    }

    public static bool IsEcsThread(int runtimeId)
    {
        return RuntimeThreads.TryGetValue(runtimeId, out int threadId) &&
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
