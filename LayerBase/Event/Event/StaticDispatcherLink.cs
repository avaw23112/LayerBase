namespace LayerBase.Core.Event;

public interface IStaticEventDispatcher<T> where T : struct
{
    EventHandledState Dispatch(in T value, int sourceIndex, Propagation propagation);


    EventHandledState DispatchLocal(int layerIndex, in T value);
}

public static class StaticEventDispatcher<T> where T : struct
{
    public static IStaticEventDispatcher<T>? Dispatcher;
}