namespace LayerBase.Actor;

/// <summary>
/// 显式允许对象池复用的 Actor。
/// </summary>
public interface IPooledActor : IActor
{
    void OnRent();

    void OnReturn();
}
