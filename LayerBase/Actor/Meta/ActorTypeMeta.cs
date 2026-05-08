namespace LayerBase.Actor;

internal sealed class ActorTypeMeta<TActor>
    where TActor : class, IActor
{
    public BehaviourSignature Signature { get; }

    public ActorBehaviourEntry[] Behaviours { get; }

    public ActorTypeMeta(BehaviourSignature signature, ActorBehaviourEntry[] behaviours)
    {
        Signature = signature;
        Behaviours = behaviours ?? throw new ArgumentNullException(nameof(behaviours));
    }
}
