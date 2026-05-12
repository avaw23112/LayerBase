namespace LayerBase.Actor;

public delegate void ActorBehaviourInvoker<TActor, TEvent>(TActor actor, in TEvent value)
    where TActor : class, IActor
    where TEvent : struct;