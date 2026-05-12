namespace LayerBase.Actor;

internal sealed class ActorTypeMeta<TActor>
    where TActor : class, IActor
{
    public BehaviourSignature Signature { get; }

    public ActorBehaviourEntry[] Behaviours { get; }
    public ActorCallEntry[] CallBehaviours { get; }

    public int[] TagIds { get; }

    public int[] GroupIds { get; }

    public ActorTypeMeta(
        BehaviourSignature    signature,
        ActorBehaviourEntry[] behaviours,
        ActorCallEntry[]      callBehaviours,
        int[]                 tagIds,
        int[]                 groupIds)
    {
        Signature = signature;
        Behaviours = behaviours ?? throw new ArgumentNullException(nameof(behaviours));
        CallBehaviours = callBehaviours ?? throw new ArgumentNullException(nameof(callBehaviours));
        TagIds = tagIds ?? throw new ArgumentNullException(nameof(tagIds));
        GroupIds = groupIds ?? throw new ArgumentNullException(nameof(groupIds));
    }
}