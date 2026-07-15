using System;
using LayerBase.Core.Event;

namespace LayerBase;

/// <summary>
/// LayerHub/LayerRuntime 预热扩展方法。
/// </summary>
public static class LayerHubPrewarmExtensions
{
    /// <summary>
    /// 使用默认参数预热 LayerRuntime。
    /// </summary>
    /// <param name="runtime">已经 Build 完成的 LayerRuntime。</param>
    /// <returns>返回原始 LayerRuntime，方便链式调用。</returns>
    public static LayerRuntime Prewarm(this LayerRuntime runtime)
    {
        return runtime.Prewarm(LayerPrewarmOptions.Default);
    }

    /// <summary>
    /// 使用指定参数预热 LayerRuntime。
    /// </summary>
    /// <param name="runtime">已经 Build 完成的 LayerRuntime。</param>
    /// <param name="options">预热参数。</param>
    /// <returns>返回原始 LayerRuntime，方便链式调用。</returns>
    public static LayerRuntime Prewarm(this LayerRuntime runtime, in LayerPrewarmOptions options)
    {
        if (runtime == null)
        {
            throw new ArgumentNullException(nameof(runtime));
        }

        // 注意：
        // 这里的实现方式是：
        // 1. 调用源生成器参与填充的预热注册表。
        // 2. 注册表会调用源生成器生成的 PrewarmGenerated 方法。
        return runtime.PrewarmInternal(in options);
    }
}
