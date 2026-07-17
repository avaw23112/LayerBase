namespace LayerBase.Core.Event;

internal readonly struct PostTimerPayload
{
    public readonly PayloadHandle PayloadHandle;
    public readonly PostTypePlan OverridePlan;
    public readonly bool HasOverridePlan;

    public PostTimerPayload(
        PayloadHandle payloadHandle,
        in PostTypePlan overridePlan,
        bool hasOverridePlan)
    {
        PayloadHandle = payloadHandle;
        OverridePlan = overridePlan;
        HasOverridePlan = hasOverridePlan;
    }
}
