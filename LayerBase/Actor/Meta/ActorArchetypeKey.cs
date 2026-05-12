namespace LayerBase.Actor;

internal readonly struct ActorArchetypeKey : IEquatable<ActorArchetypeKey>
{
    public readonly Type ActorType;
    public readonly BehaviourSignature Behaviour;
    public readonly ActorTagSignature Tags;
    public readonly ActorGroupSignature Groups;

    public ActorArchetypeKey(
        Type                actorType,
        BehaviourSignature  behaviour,
        ActorTagSignature   tags,
        ActorGroupSignature groups)
    {
        ActorType = actorType ?? throw new ArgumentNullException(nameof(actorType));
        Behaviour = behaviour;
        Tags = tags;
        Groups = groups;
    }

    public bool Equals(ActorArchetypeKey other)
    {
        return ActorType == other.ActorType
               && Behaviour.Equals(other.Behaviour)
               && Tags.Equals(other.Tags)
               && Groups.Equals(other.Groups);
    }

    public override bool Equals(object? obj)
    {
        return obj is ActorArchetypeKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ActorType, Behaviour, Tags, Groups);
    }
}