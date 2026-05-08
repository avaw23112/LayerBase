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
        releaseWhenEmpty: true);

    public readonly ActorPostPolicy PostPolicy;
    public readonly ActorMailFullPolicy FullPolicy;
    public readonly ActorMailFullPolicy GrowFailurePolicy;
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
        bool releaseWhenEmpty)
    {
        PostPolicy = postPolicy;
        FullPolicy = fullPolicy;
        GrowFailurePolicy = growFailurePolicy;
        InitialCapacity = initialCapacity;
        MaxCapacity = maxCapacity;
        GrowFactor = growFactor;
        ReleaseWhenEmpty = releaseWhenEmpty;
    }
}
