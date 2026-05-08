using LayerBase.Core.Event;

namespace LayerBase.Actor;

public interface IGeneratedActorMeta
{
    void __BuildActorMeta(ActorTypeMetaBuilder builder);

    ActorId GetId();

    void ActorInit(ActorContext context);

    bool GetEnable();

    bool SetEnable(bool enable);

    PostResult Post<TEvent>(in TEvent value)
        where TEvent : struct;

    PostResult TryPost<TEvent>(in TEvent value)
        where TEvent : struct;
}
