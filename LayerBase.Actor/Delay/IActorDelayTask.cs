namespace LayerBase.Actor;

internal interface IActorDelayTask
{
    void Execute();

    void Cancel();
}
