namespace LayerBase.Core.Event;

public enum BackpressurePolicy
{
    RejectNew,
    DropNewest,
    DropOldest,
    Coalesce,
    Latest
}
