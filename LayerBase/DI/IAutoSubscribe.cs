using System.Runtime.CompilerServices;
using LayerBase.Actor;
using LayerBase.Core.Event;
using LayerBase.Core.EventHandler;
using LayerBase.Layers;

namespace LayerBase.DI;

/// <summary>
/// 表示事件之间的依赖关系。
/// </summary>
public readonly struct EventDependency
{
    public readonly Type Source;
    public readonly Type Target;

    public EventDependency(Type source, Type target)
    {
        Source = source;
        Target = target;
    }
}

/// <summary>
/// 支持自动订阅的接口，通过此接口可以自动绑定事件与处理逻辑。
/// </summary>
public interface IAutoSubscribe
{
    void AutoBind(Layer layer);
    IEnumerable<EventDependency> GetEventDependencies();
    IEnumerable<Type> GetSubscribedEvents();
}
