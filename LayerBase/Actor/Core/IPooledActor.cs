namespace LayerBase.Actor;

/// <summary>
/// ?????????? Actor?
/// </summary>
public interface IPooledActor : IActor
{
    long RecycleDeadlineTicks { get; set; }

    void OnRent();

    void OnReturn();
}
