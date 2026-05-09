using LayerBase.Core.Event;

namespace LayerBase.Actor;

public interface IGeneratedActorMeta
{
    /// <summary>
    /// Actor 运行时上下文。
    /// </summary>
    ActorContext Context { get; }
    
    void __BuildActorMeta(ActorTypeMetaBuilder builder);

    ActorId GetId();

    void ActorInit(ActorContext context);
}
