namespace LayerBase.Actor;

internal static class ActorMailMergeInvoker<TEvent>
    where TEvent : struct
{
    public static bool CanMerge => typeof(IActorMailMerge<TEvent>).IsAssignableFrom(typeof(TEvent));

    public static TEvent Merge(in TEvent oldValue, in TEvent newValue)
    {
        if (!CanMerge)
        {
            throw new InvalidOperationException(
                $"Event type {typeof(TEvent).Name} does not implement IActorMailMerge<{typeof(TEvent).Name}>.");
        }

        var merger = (IActorMailMerge<TEvent>)default(TEvent);
        return merger.Merge(in oldValue, in newValue);
    }
}
