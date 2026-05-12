namespace LayerBase.Actor;

public sealed class ActorQueryBuilder
{
    private readonly ActorWorld _world;
    private BehaviourSignature _allBehaviours = BehaviourSignature.Empty;
    private BehaviourSignature _noneBehaviours = BehaviourSignature.Empty;
    private ActorTagSignature _allTags = ActorTagSignature.Empty;
    private ActorTagSignature _noneTags = ActorTagSignature.Empty;
    private ActorGroupSignature _allGroups = ActorGroupSignature.Empty;
    private ActorGroupSignature _noneGroups = ActorGroupSignature.Empty;

    public ActorQueryBuilder(ActorWorld world)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
    }

    public ActorQueryBuilder AllBehaviours<TEvent>()
        where TEvent : struct
    {
        _allBehaviours = Merge(_allBehaviours, ActorBehaviourQuerySignature<TEvent>.Value);
        return this;
    }

    public ActorQueryBuilder AllBehaviours<TEvent1, TEvent2>()
        where TEvent1 : struct
        where TEvent2 : struct
    {
        _allBehaviours = Merge(_allBehaviours, ActorBehaviourQuerySignature<TEvent1, TEvent2>.Value);
        return this;
    }

    public ActorQueryBuilder NoneBehaviours<TEvent>()
        where TEvent : struct
    {
        _noneBehaviours = Merge(_noneBehaviours, ActorBehaviourQuerySignature<TEvent>.Value);
        return this;
    }

    public ActorQueryBuilder NoneBehaviours<TEvent1, TEvent2>()
        where TEvent1 : struct
        where TEvent2 : struct
    {
        _noneBehaviours = Merge(_noneBehaviours, ActorBehaviourQuerySignature<TEvent1, TEvent2>.Value);
        return this;
    }

    public ActorQueryBuilder AllTags<TTag>()
        where TTag : struct, IActorTag
    {
        _allTags = Merge(_allTags, ActorTagQuerySignature<TTag>.Value);
        return this;
    }

    public ActorQueryBuilder AllTags<TTag1, TTag2>()
        where TTag1 : struct, IActorTag
        where TTag2 : struct, IActorTag
    {
        _allTags = Merge(_allTags, ActorTagQuerySignature<TTag1, TTag2>.Value);
        return this;
    }

    public ActorQueryBuilder NoneTags<TTag>()
        where TTag : struct, IActorTag
    {
        _noneTags = Merge(_noneTags, ActorTagQuerySignature<TTag>.Value);
        return this;
    }

    public ActorQueryBuilder NoneTags<TTag1, TTag2>()
        where TTag1 : struct, IActorTag
        where TTag2 : struct, IActorTag
    {
        _noneTags = Merge(_noneTags, ActorTagQuerySignature<TTag1, TTag2>.Value);
        return this;
    }

    public ActorQueryBuilder AllGroups<TGroup>()
        where TGroup : struct, IActorGroup
    {
        _allGroups = Merge(_allGroups, ActorGroupQuerySignature<TGroup>.Value);
        return this;
    }

    public ActorQueryBuilder AllGroups<TGroup1, TGroup2>()
        where TGroup1 : struct, IActorGroup
        where TGroup2 : struct, IActorGroup
    {
        _allGroups = Merge(_allGroups, ActorGroupQuerySignature<TGroup1, TGroup2>.Value);
        return this;
    }

    public ActorQueryBuilder NoneGroups<TGroup>()
        where TGroup : struct, IActorGroup
    {
        _noneGroups = Merge(_noneGroups, ActorGroupQuerySignature<TGroup>.Value);
        return this;
    }

    public ActorQueryBuilder NoneGroups<TGroup1, TGroup2>()
        where TGroup1 : struct, IActorGroup
        where TGroup2 : struct, IActorGroup
    {
        _noneGroups = Merge(_noneGroups, ActorGroupQuerySignature<TGroup1, TGroup2>.Value);
        return this;
    }

    public ActorQueryResult Build()
    {
        return _world.GetOrBuildQuery(new ActorQueryDescriptor(
            _allBehaviours,
            _noneBehaviours,
            _allTags,
            _noneTags,
            _allGroups,
            _noneGroups));
    }

    private static BehaviourSignature Merge(BehaviourSignature left, BehaviourSignature right)
    {
        return new BehaviourSignature(ActorSignatureUtility.Merge(left.EventTypeIds, right.EventTypeIds));
    }

    private static ActorTagSignature Merge(ActorTagSignature left, ActorTagSignature right)
    {
        return new ActorTagSignature(ActorSignatureUtility.Merge(left.Ids, right.Ids));
    }

    private static ActorGroupSignature Merge(ActorGroupSignature left, ActorGroupSignature right)
    {
        return new ActorGroupSignature(ActorSignatureUtility.Merge(left.Ids, right.Ids));
    }
}