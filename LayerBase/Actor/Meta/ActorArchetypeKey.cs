namespace LayerBase.Actor;

internal readonly struct ActorArchetypeKey : IEquatable<ActorArchetypeKey>
{
    public readonly BehaviourSignature Behaviour;
    public readonly ActorTagSignature Tags;
    public readonly ActorGroupSignature Groups;

    public ActorArchetypeKey(
        BehaviourSignature behaviour,
        ActorTagSignature tags,
        ActorGroupSignature groups)
    {
        Behaviour = behaviour;
        Tags = tags;
        Groups = groups;
    }

    public bool Equals(ActorArchetypeKey other)
    {
        return Behaviour.Equals(other.Behaviour)
               && Tags.Equals(other.Tags)
               && Groups.Equals(other.Groups);
    }

    public override bool Equals(object? obj)
    {
        return obj is ActorArchetypeKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Behaviour, Tags, Groups);
    }
}
