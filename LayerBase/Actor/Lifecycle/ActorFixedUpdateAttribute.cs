using System;

namespace LayerBase.Actor;

[AttributeUsage(AttributeTargets.Method)]
public sealed class ActorFixedUpdateAttribute : Attribute
{
    public TickTier Tier { get; }

    public int Phase { get; init; } = -1;

    public ActorFixedUpdateAttribute(TickTier tier = TickTier.Hot)
    {
        Tier = tier;
    }
}
