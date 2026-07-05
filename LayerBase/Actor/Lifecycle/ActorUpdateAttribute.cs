using System;

namespace LayerBase.Actor;

[AttributeUsage(AttributeTargets.Method)]
public sealed class ActorUpdateAttribute : Attribute
{
    public TickTier Tier { get; }

    public int Phase { get; init; } = -1;

    public ActorUpdateAttribute(TickTier tier = TickTier.Hot)
    {
        Tier = tier;
    }
}
