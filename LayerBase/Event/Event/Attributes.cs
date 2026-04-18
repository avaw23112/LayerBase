using System;

namespace LayerBase.Core.Event
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class SubscribeAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class SubscribeAsyncAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class SubscribeParallelAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class SubscribeDelayAttribute : Attribute { }
}
