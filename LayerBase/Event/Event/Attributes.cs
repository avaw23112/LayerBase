namespace LayerBase.Core.Event;

[AttributeUsage(AttributeTargets.Method)]
public sealed class SubscribeNotifySafeAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
public sealed class SubscribeAttribute : Attribute { }

/// <summary>
///     <para>纯粹消息通道：无异常熔断，无Handle控制，甚至不进入异常报告通道。需要开发者自己管理异常�?/para>
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SubscribeNotifyAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class SubscribeAsyncAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class SubscribeParallelAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class SubscribeDelayAttribute : Attribute
{
}

