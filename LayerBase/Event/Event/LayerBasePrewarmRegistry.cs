using System;
using System.Collections.Generic;
using LayerBase.Core.Event;

namespace LayerBase.Core.Event;

public partial struct EventPrewarmBootstrapper
{
    
}

/// <summary>
/// 事件预热注册表。
/// 该类维护了一系列由源生成器或手动添加的预热动作。
/// </summary>
public static class LayerBasePrewarmRegistry
{
    private static readonly List<Action<EventCenter, LayerPrewarmOptions>> s_prewarmers = new();
    private static readonly object s_lock = new();

    /// <summary>
    /// 注册一个预热动作。通常由源生成器生成的 Initializer 调用。
    /// </summary>
    /// <param name="prewarmer">预热逻辑。</param>
    public static void Register(Action<EventCenter, LayerPrewarmOptions> prewarmer)
    {
        if (prewarmer == null) return;
        lock (s_lock)
        {
            s_prewarmers.Add(prewarmer);
        }
    }

    /// <summary>
    /// 执行所有已注册的预热动作。
    /// </summary>
    /// <param name="center">全局事件中心。</param>
    /// <param name="options">预热参数。</param>
    public static void Prewarm(EventCenter center, in LayerPrewarmOptions options)
    {
        if (center == null) throw new ArgumentNullException(nameof(center));
        
        Action<EventCenter, LayerPrewarmOptions>[] snapshots;
        lock (s_lock)
        {
            snapshots = s_prewarmers.ToArray();
        }

        foreach (var prewarmer in snapshots)
        {
            prewarmer(center, options);
        }
    }
}
