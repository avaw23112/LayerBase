using LayerBase.Core.Event;
using LayerBase.ECS.Runtime;
using LayerBase.Event.Delay;

namespace LayerBase.Scope;

public sealed class ScopeRuntimeOptions
{
    public const int DefaultQueueCapacity = 1024;

    public ScopeRuntimeOptions(
        int postQueueCapacity = DefaultQueueCapacity,
        int callQueueCapacity = DefaultQueueCapacity,
        int continuationQueueCapacity = DefaultQueueCapacity,
        PostSchedulerOptions? postSchedulerOptions = null,
        TimeSchedulerOptions? timeSchedulerOptions = null,
        DelayBufferOptions? delayBufferOptions = null,
        EcsRuntimeOptions? ecsOptions = null)
    {
        if (postQueueCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(postQueueCapacity));
        }

        if (callQueueCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(callQueueCapacity));
        }

        if (continuationQueueCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(continuationQueueCapacity));
        }

        PostQueueCapacity = postQueueCapacity;
        CallQueueCapacity = callQueueCapacity;
        ContinuationQueueCapacity = continuationQueueCapacity;
        PostSchedulerOptions = postSchedulerOptions ?? PostSchedulerOptions.Default;
        TimeSchedulerOptions = timeSchedulerOptions ?? TimeSchedulerOptions.Default;
        DelayBufferOptions = delayBufferOptions ?? DelayBufferOptions.Default;
        EcsOptions = ecsOptions;
    }

    public int PostQueueCapacity { get; }

    public int CallQueueCapacity { get; }

    public int ContinuationQueueCapacity { get; }

    public PostSchedulerOptions PostSchedulerOptions { get; }

    public TimeSchedulerOptions TimeSchedulerOptions { get; }

    public DelayBufferOptions DelayBufferOptions { get; }

    public EcsRuntimeOptions? EcsOptions { get; }

    public static ScopeRuntimeOptions Default { get; } = new();
}
