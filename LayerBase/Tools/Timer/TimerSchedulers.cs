using System.Collections.Concurrent;

namespace LayerBase.Tools.Timer;

/// <summary>
/// 统一管理与调度所有 TimerScheduler。
/// </summary>
public static class TimerSchedulers
{
	private static readonly ConcurrentDictionary<string, TimerScheduler> s_schedulers = new(StringComparer.Ordinal);

	/// <summary>
	/// 获取或创建具名调度器。
	/// </summary>
	public static TimerScheduler GetOrCreate(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			throw new ArgumentNullException(nameof(name));
		}

		return s_schedulers.GetOrAdd(name, static _ => new TimerScheduler());
	}

	/// <summary>
	/// 尝试获取具名调度器。
	/// </summary>
	public static bool TryGet(string name, out TimerScheduler? scheduler)
	{
		scheduler = default;
		if (string.IsNullOrWhiteSpace(name))
		{
			return false;
		}

		return s_schedulers.TryGetValue(name, out scheduler);
	}

	/// <summary>
	/// 统一推进所有调度器的时间。
	/// </summary>
	public static void TickAll(double deltaTime)
	{
		foreach (var scheduler in s_schedulers.Values)
		{
			scheduler.Tick(deltaTime);
		}
	}

	/// <summary>
	/// 移除具名调度器。
	/// </summary>
	public static bool Remove(string name) => s_schedulers.TryRemove(name, out _);

	/// <summary>
	/// 清空所有调度器。
	/// </summary>
	public static void Clear() => s_schedulers.Clear();
}
