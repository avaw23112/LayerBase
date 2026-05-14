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
    public readonly int SegmentCapacity;
    public readonly int MaxRetainedSegments;

    public ActorMailOptions(
        ActorPostPolicy               postPolicy,
        ActorMailFullPolicy           fullPolicy,
        ActorMailFullPolicy           growFailurePolicy,
        int                           initialCapacity,
        int                           maxCapacity,
        int                           growFactor,
        bool                          releaseWhenEmpty,
        ActorMailDisabledPolicy       disabledPolicy       = ActorMailDisabledPolicy.Accept,
        ActorMailPendingDestroyPolicy pendingDestroyPolicy = ActorMailPendingDestroyPolicy.Reject,
        int                           segmentCapacity      = 0,
        int                           maxRetainedSegments  = 0)
    {
        int normalizedInitialCapacity = ActorMailCapacity.NormalizePowerOfTwo(Math.Max(initialCapacity, 1));
        int normalizedMaxCapacity =
            ActorMailCapacity.NormalizePowerOfTwo(Math.Max(maxCapacity, normalizedInitialCapacity));

        PostPolicy = postPolicy;
        DeliveryMode = ToDeliveryMode(postPolicy);
        FullPolicy = fullPolicy;
        GrowFailurePolicy = growFailurePolicy;
        DisabledPolicy = disabledPolicy;
        PendingDestroyPolicy = pendingDestroyPolicy;
        InitialCapacity = normalizedInitialCapacity;
        MaxCapacity = Math.Max(normalizedInitialCapacity, normalizedMaxCapacity);
        GrowFactor = Math.Max(growFactor, 2);
        ReleaseWhenEmpty = releaseWhenEmpty;
        SegmentCapacity = segmentCapacity;
        MaxRetainedSegments = maxRetainedSegments;
    }

    /// <summary>
    /// 创建 EventStream 后端的 ActorMailOptions。
    /// </summary>
    /// <param name="segmentCapacity">
    /// 每个 Segment 的邮件容量。
    /// </param>
    /// <param name="maxRetainedSegments">
    /// Segment 池最多保留多少个空闲 Segment。
    /// </param>
    /// <returns>
    /// 配置好的 ActorMailOptions。
    /// </returns>
    public static ActorMailOptions EventStream(
        int segmentCapacity     = 512,
        int maxRetainedSegments = 4)
    {
        return new ActorMailOptions(
            postPolicy: ActorPostPolicy.Queued,
            fullPolicy: ActorMailFullPolicy.Grow,
            growFailurePolicy: ActorMailFullPolicy.RejectNew,
            initialCapacity: 4,
            maxCapacity: 64,
            growFactor: 2,
            releaseWhenEmpty: false,
            segmentCapacity: segmentCapacity,
            maxRetainedSegments: maxRetainedSegments);
    }

    private static ActorMailDeliveryMode ToDeliveryMode(ActorPostPolicy postPolicy)
    {
        return postPolicy switch
               {
                   ActorPostPolicy.Latest    => ActorMailDeliveryMode.LatestOnly,
                   ActorPostPolicy.Coalesced => ActorMailDeliveryMode.Merge,
                   _                         => ActorMailDeliveryMode.Queue
               };
    }
}