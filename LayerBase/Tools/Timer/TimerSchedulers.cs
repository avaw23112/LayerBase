using System.Collections.Concurrent;

namespace LayerBase.Tools.Timer;

/// <summary>
/// 命名 TimerScheduler 实例的全局注册表。支持通过名称创建、获取和统一推进所有调度器。
/// </summary>
public static class TimerSchedulers
{
    private static readonly ConcurrentDictionary<string, TimerScheduler> s_schedulers = new(StringComparer.Ordinal);


    /// <summary>获取或创建指定名称的 TimerScheduler。</summary>
    public static TimerScheduler GetOrCreate(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
        return s_schedulers.GetOrAdd(name, static _ => new TimerScheduler());
    }

    /// <summary>尝试获取指定名称的 TimerScheduler。</summary>
    public static bool TryGet(string name, out TimerScheduler? scheduler)
    {
        scheduler = default;
        if (string.IsNullOrWhiteSpace(name)) return false;
        return s_schedulers.TryGetValue(name, out scheduler);
    }

    /// <summary>推进所有已注册调度器的时间。</summary>
    public static void TickAll(double deltaTime)
    {
        foreach (var scheduler in s_schedulers.Values) scheduler.Tick(deltaTime);
    }

    /// <summary>移除指定名称的调度器。</summary>
    public static bool Remove(string name)
    {
        return s_schedulers.TryRemove(name, out _);
    }

    /// <summary>清空所有调度器。</summary>
    public static void Clear()
    {
        s_schedulers.Clear();
    }
}