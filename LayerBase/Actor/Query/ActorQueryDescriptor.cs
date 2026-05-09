namespace LayerBase.Actor;

internal readonly struct ActorQueryDescriptor : IEquatable<ActorQueryDescriptor>
{
    public readonly BehaviourSignature AllBehaviours;
    public readonly BehaviourSignature NoneBehaviours;
    public readonly ActorTagSignature AllTags;
    public readonly ActorTagSignature NoneTags;
    public readonly ActorGroupSignature AllGroups;
    public readonly ActorGroupSignature NoneGroups;

    public ActorQueryDescriptor(
        BehaviourSignature allBehaviours,
        BehaviourSignature noneBehaviours,
        ActorTagSignature allTags,
        ActorTagSignature noneTags,
        ActorGroupSignature allGroups,
        ActorGroupSignature noneGroups)
    {
        AllBehaviours = allBehaviours;
        NoneBehaviours = noneBehaviours;
        AllTags = allTags;
        NoneTags = noneTags;
        AllGroups = allGroups;
        NoneGroups = noneGroups;
    }

    public bool Equals(ActorQueryDescriptor other)
    {
        return AllBehaviours.Equals(other.AllBehaviours)
               && NoneBehaviours.Equals(other.NoneBehaviours)
               && AllTags.Equals(other.AllTags)
               && NoneTags.Equals(other.NoneTags)
               && AllGroups.Equals(other.AllGroups)
               && NoneGroups.Equals(other.NoneGroups);
    }

    public override bool Equals(object? obj)
    {
        return obj is ActorQueryDescriptor other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            AllBehaviours,
            NoneBehaviours,
            AllTags,
            NoneTags,
            AllGroups,
            NoneGroups);
    }
}
