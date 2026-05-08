namespace LayerBase.Actor;

public enum ActorPostPolicy
{
    Queued,
    Latest,
    Coalesced,
    Dirty
}
