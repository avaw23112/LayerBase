namespace LayerBase.Actor;

public interface IPooledActor : IActor
{
    long RecycleDeadlineTicks { get; set; }

    void OnRent();

    void OnReturn();
}
