namespace LayerBase.Worker;

public interface IWorkerEventJob<TInput, TEvent>
    where TInput : struct
    where TEvent : struct
{
    TEvent Execute(in TInput input);
}
