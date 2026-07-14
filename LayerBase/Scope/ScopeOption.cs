using LayerBase.Core.Event;
using LayerBase.ECS.Runtime;
using LayerBase.Event.Delay;

namespace LayerBase.Scope;

public class ScopeOption<TScope>
{
    public virtual ScopeThreadingMode Threading => ScopeThreadingMode.Inline;

    public virtual ScopeClockMode Clock => ScopeClockMode.EngineDriven;

    public virtual int TickRateHz => 0;

    public virtual ScopeStopPolicy StopPolicy => ScopeStopPolicy.Drain;

    public virtual int PostQueueCapacity => ScopeRuntimeOptions.DefaultQueueCapacity;

    public virtual int CallQueueCapacity => ScopeRuntimeOptions.DefaultQueueCapacity;

    public virtual int ContinuationQueueCapacity => ScopeRuntimeOptions.DefaultQueueCapacity;

    public virtual int CompletionQueueCapacity => ScopeRuntimeOptions.DefaultQueueCapacity;

    public virtual PostSchedulerOptions PostSchedulerOptions => PostSchedulerOptions.Default;

    public virtual TimeSchedulerOptions TimeSchedulerOptions => TimeSchedulerOptions.Default;

    public virtual DelayBufferOptions DelayBufferOptions => DelayBufferOptions.Default;

    public virtual EcsRuntimeOptions? EcsRuntimeOptions => null;
}

public sealed class ScopeOptionDefaults<TScope> : ScopeOption<TScope>
{
    private ScopeOptionDefaults()
    {
    }

    public static ScopeOption<TScope> Instance { get; } = new ScopeOptionDefaults<TScope>();
}
