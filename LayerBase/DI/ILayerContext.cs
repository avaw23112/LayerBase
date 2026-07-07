using System.Runtime.CompilerServices;
using LayerBase.Actor;
using LayerBase.Core.Event;
using LayerBase.Core.EventHandler;
using LayerBase.Layers;

namespace LayerBase.DI;

/// <summary>
/// 表示一个分层系统的上下文，用于绑定服务与 Layer。
/// </summary>
public interface ILayerContext
{
}

public interface IInternalLayerContext : ILayerContext
{
    int LayerIndex { get; set; }
}

/// <summary>
/// 由 LayerBase 源生成器自动实现的隐藏绑定接口。
/// </summary>
public interface ILayerBindingAccessor
{
    object? __LayerBaseBinding { get; set; }
}

/// <summary>
/// 由生成器为 Layer 实现的自动挂载接口。
/// </summary>
public interface IAutoLayerMount
{
    void __AutoMountServices(Layers.Layer layer);
}
