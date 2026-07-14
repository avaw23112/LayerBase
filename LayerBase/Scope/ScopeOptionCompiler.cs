namespace LayerBase.Scope;

internal static class ScopeOptionCompiler
{
    public static ScopeOptionTemplate Compile<TScope>(ScopeOption<TScope> option)
    {
        if (option == null)
        {
            throw new ArgumentNullException(nameof(option));
        }

        return new ScopeOptionTemplate(
            option.Threading,
            option.Clock,
            option.TickRateHz,
            option.StopPolicy,
            option.PostQueueCapacity,
            option.CallQueueCapacity,
            option.ContinuationQueueCapacity,
            option.CompletionQueueCapacity,
            option.PostSchedulerOptions,
            option.TimeSchedulerOptions,
            option.DelayBufferOptions,
            option.EcsRuntimeOptions);
    }

    public static ScopeRuntimeOptions ToRuntimeOptions(ScopeOptionTemplate template)
    {
        return new ScopeRuntimeOptions(
            template.PostQueueCapacity,
            template.CallQueueCapacity,
            template.ContinuationQueueCapacity,
            template.CompletionQueueCapacity,
            template.PostSchedulerOptions,
            template.TimeSchedulerOptions,
            template.DelayBufferOptions,
            template.EcsRuntimeOptions);
    }
}
