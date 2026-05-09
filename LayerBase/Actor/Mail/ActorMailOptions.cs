namespace LayerBase.Actor;

public readonly struct ActorMailOptions
{
    public static ActorMailOptions Default => new(
        postPolicy: ActorPostPolicy.Queued,
        fullPolicy: ActorMailFullPolicy.Grow,
        growFailurePolicy: ActorMailFullPolicy.RejectNew,
        initialCapacity: 4,
        maxCapacity: 64,
        growFactor: 2,
        releaseWhenEmpty: false,
        disabledPolicy: ActorMailDisabledPolicy.Accept,
        pendingDestroyPolicy: ActorMailPendingDestroyPolicy.Reject);

    public static ActorMailOptions MemorySaving => new(
        postPolicy: ActorPostPolicy.Queued,
        fullPolicy: ActorMailFullPolicy.Grow,
        growFailurePolicy: ActorMailFullPolicy.RejectNew,
        initialCapacity: 4,
        maxCapacity: 64,
        growFactor: 2,
        releaseWhenEmpty: true,
        disabledPolicy: ActorMailDisabledPolicy.Accept,
        pendingDestroyPolicy: ActorMailPendingDestroyPolicy.Reject);

    public readonly ActorPostPolicy PostPolicy;
    public readonly ActorMailDeliveryMode DeliveryMode;
    public readonly ActorMailFullPolicy FullPolicy;
    public readonly ActorMailFullPolicy GrowFailurePolicy;
    public readonly ActorMailDisabledPolicy DisabledPolicy;
    public readonly ActorMailPendingDestroyPolicy PendingDestroyPolicy;
    public readonly int InitialCapacity;
    public readonly int MaxCapacity;
    public readonly int GrowFactor;
    public readonly bool ReleaseWhenEmpty;

    public ActorMailOptions(
        ActorPostPolicy postPolicy,
        ActorMailFullPolicy fullPolicy,
        ActorMailFullPolicy growFailurePolicy,
        int initialCapacity,
        int maxCapacity,
        int growFactor,
        bool releaseWhenEmpty,
        ActorMailDisabledPolicy disabledPolicy = ActorMailDisabledPolicy.Accept,
        ActorMailPendingDestroyPolicy pendingDestroyPolicy = ActorMailPendingDestroyPolicy.Reject)
    {
        PostPolicy = postPolicy;
        DeliveryMode = ToDeliveryMode(postPolicy);
        FullPolicy = fullPolicy;
        GrowFailurePolicy = growFailurePolicy;
        DisabledPolicy = disabledPolicy;
        PendingDestroyPolicy = pendingDestroyPolicy;
        InitialCapacity = initialCapacity;
        MaxCapacity = maxCapacity;
        GrowFactor = growFactor;
        ReleaseWhenEmpty = releaseWhenEmpty;
    }

    private static ActorMailDeliveryMode ToDeliveryMode(ActorPostPolicy postPolicy)
    {
        return postPolicy switch
        {
            ActorPostPolicy.Latest => ActorMailDeliveryMode.LatestOnly,
            ActorPostPolicy.Coalesced => ActorMailDeliveryMode.Merge,
            _ => ActorMailDeliveryMode.Queue
        };
    }
}
