using System.Collections.Concurrent;

namespace LayerBase.Tools.Timer;

public static class TimerSchedulers
{
    private static readonly ConcurrentDictionary<string, TimerScheduler> s_schedulers = new(StringComparer.Ordinal);


    public static TimerScheduler GetOrCreate(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));

        return s_schedulers.GetOrAdd(name, static _ => new TimerScheduler());
    }


    public static bool TryGet(string name, out TimerScheduler? scheduler)
    {
        scheduler = default;
        if (string.IsNullOrWhiteSpace(name)) return false;

        return s_schedulers.TryGetValue(name, out scheduler);
    }


    public static void TickAll(double deltaTime)
    {
        foreach (var scheduler in s_schedulers.Values) scheduler.Tick(deltaTime);
    }


    public static bool Remove(string name)
    {
        return s_schedulers.TryRemove(name, out _);
    }


    public static void Clear()
    {
        s_schedulers.Clear();
    }
}