using System;

namespace LayerBase.Actor;

[AttributeUsage(AttributeTargets.Method)]
public sealed class ActorLateUpdateAttribute : Attribute
{
    public TickTier Tier { get; }

    public int Phase { get; init; } = -1;

    public ActorLateUpdateAttribute(TickTier tier = TickTier.Hot)
    {
        Tier = tier;
    }
}
