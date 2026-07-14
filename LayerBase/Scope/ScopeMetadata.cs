namespace LayerBase.Scope;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
[Obsolete("Use ScopeOption<TScope> and ScopeOptionRegistry instead. ScopeOptionsAttribute remains compatible during migration.", false)]
public sealed class ScopeOptionsAttribute : Attribute
{
    public ScopeThreadingMode Threading { get; }

    public ScopeClockMode Clock { get; }

    public int TickRateHz { get; }

    public ScopeStopPolicy StopPolicy { get; }

    public ScopeOptionsAttribute(
        ScopeThreadingMode threading = ScopeThreadingMode.Inline,
        ScopeClockMode clock = ScopeClockMode.EngineDriven,
        int tickRateHz = 0,
        ScopeStopPolicy stopPolicy = ScopeStopPolicy.Drain)
    {
        if (tickRateHz < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tickRateHz));
        }

        Threading = threading;
        Clock = clock;
        TickRateHz = tickRateHz;
        StopPolicy = stopPolicy;
    }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class ScopeAttribute<TScope> : Attribute
{
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class ScopeCallAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class ScopeEventAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ScopeCallAttribute<TScope, TResult> : Attribute
{
}

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ScopeEventAttribute<TScope> : Attribute
{
}

public enum ScopeThreadingMode
{
    Inline,
    Worker
}

public enum ScopeClockMode
{
    EngineDriven,
    FixedRate,
    Realtime,
    Manual
}

public enum ScopeStopPolicy
{
    Drain,
    Drop
}

public readonly struct ScopeDescriptor
{
    public readonly int ScopeId;
    public readonly string Name;
    public readonly ScopeThreadingMode Threading;
    public readonly ScopeClockMode Clock;
    public readonly int TickRateHz;
    public readonly ScopeStopPolicy StopPolicy;

    public ScopeDescriptor(
        int scopeId,
        string name,
        ScopeThreadingMode threading,
        ScopeClockMode clock,
        int tickRateHz,
        ScopeStopPolicy stopPolicy)
    {
        if (tickRateHz < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tickRateHz));
        }

        ScopeId = scopeId;
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Scope name cannot be empty.", nameof(name)) : name;
        Threading = threading;
        Clock = clock;
        TickRateHz = tickRateHz;
        StopPolicy = stopPolicy;
    }
}

public static class ScopeDescriptors
{
    public static readonly ScopeDescriptor Main = new(
        scopeId: 0,
        name: "MainScope",
        threading: ScopeThreadingMode.Inline,
        clock: ScopeClockMode.EngineDriven,
        tickRateHz: 0,
        stopPolicy: ScopeStopPolicy.Drain);
}

[ScopeOptions]
public sealed partial class MainScope
{
}
