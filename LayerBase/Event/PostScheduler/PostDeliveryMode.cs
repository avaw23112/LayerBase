namespace LayerBase.Core.Event;

public enum PostDeliveryMode
{
    Normal,
    DirtySignal,
    Latest,
    Coalesced
}
