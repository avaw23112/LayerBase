namespace LayerBase.Core.Event;

[AttributeUsage(AttributeTargets.Method)]
public sealed class SubscribeAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class SubscribeFlowAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class SubscribeNotifyAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class SubscribeAsyncAttribute : Attribute
{
}

/// <summary>
/// SubscribeParallel is fire-and-forget background execution.
/// <para>Constraints:</para>
/// <list type="bullet">
/// <item>Fire-and-forget: caller does not wait for results.</item>
/// <item>Not thread-safe: users must handle synchronization for shared state.</item>
/// <item>No order guarantee: events may be processed out of order.</item>
/// <item>No results: return values (if any) are ignored.</item>
/// </list>
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SubscribeParallelAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class SubscribeDelayAttribute : Attribute
{
}