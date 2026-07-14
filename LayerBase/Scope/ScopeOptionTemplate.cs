using LayerBase.Core.Event;
using LayerBase.ECS.Runtime;
using LayerBase.Event.Delay;

namespace LayerBase.Scope;

internal readonly struct ScopeOptionTemplate
{
    public ScopeOptionTemplate(
        ScopeThreadingMode threading,
        ScopeClockMode clock,
        int tickRateHz,
        ScopeStopPolicy stopPolicy,
        int postQueueCapacity,
        int callQueueCapacity,
        int continuationQueueCapacity,
        int completionQueueCapacity,
        PostSchedulerOptions postSchedulerOptions,
        TimeSchedulerOptions timeSchedulerOptions,
        DelayBufferOptions delayBufferOptions,
        EcsRuntimeOptions? ecsRuntimeOptions)
    {
        Threading = threading;
        Clock = clock;
        TickRateHz = tickRateHz;
        StopPolicy = stopPolicy;
        PostQueueCapacity = postQueueCapacity;
        CallQueueCapacity = callQueueCapacity;
        ContinuationQueueCapacity = continuationQueueCapacity;
        CompletionQueueCapacity = completionQueueCapacity;
        PostSchedulerOptions = postSchedulerOptions;
        TimeSchedulerOptions = timeSchedulerOptions;
        DelayBufferOptions = delayBufferOptions;
        EcsRuntimeOptions = ecsRuntimeOptions;
    }

    public ScopeThreadingMode Threading { get; }

    public ScopeClockMode Clock { get; }

    public int TickRateHz { get; }

    public ScopeStopPolicy StopPolicy { get; }

    public int PostQueueCapacity { get; }

    public int CallQueueCapacity { get; }

    public int ContinuationQueueCapacity { get; }

    public int CompletionQueueCapacity { get; }

    public PostSchedulerOptions PostSchedulerOptions { get; }

    public TimeSchedulerOptions TimeSchedulerOptions { get; }

    public DelayBufferOptions DelayBufferOptions { get; }

    public EcsRuntimeOptions? EcsRuntimeOptions { get; }

    public static ScopeOptionTemplate Default { get; } = new(
        ScopeThreadingMode.Inline,
        ScopeClockMode.EngineDriven,
        0,
        ScopeStopPolicy.Drain,
        ScopeRuntimeOptions.DefaultQueueCapacity,
        ScopeRuntimeOptions.DefaultQueueCapacity,
        ScopeRuntimeOptions.DefaultQueueCapacity,
        ScopeRuntimeOptions.DefaultQueueCapacity,
        PostSchedulerOptions.Default,
        TimeSchedulerOptions.Default,
        DelayBufferOptions.Default,
        null);

    public static ScopeOptionTemplate FromDescriptor(ScopeDescriptor descriptor)
    {
        return new ScopeOptionTemplate(
            descriptor.Threading,
            descriptor.Clock,
            descriptor.TickRateHz,
            descriptor.StopPolicy,
            ScopeRuntimeOptions.DefaultQueueCapacity,
            ScopeRuntimeOptions.DefaultQueueCapacity,
            ScopeRuntimeOptions.DefaultQueueCapacity,
            ScopeRuntimeOptions.DefaultQueueCapacity,
            PostSchedulerOptions.Default,
            TimeSchedulerOptions.Default,
            DelayBufferOptions.Default,
            null);
    }
}
