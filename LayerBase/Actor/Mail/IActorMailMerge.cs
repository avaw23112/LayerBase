namespace LayerBase.Actor;

public interface IActorMailMerge<TEvent>
    where TEvent : struct
{
    TEvent Merge(in TEvent oldValue, in TEvent newValue);
}
