namespace LayerBase.DI.Options;

/// <summary>
/// 每帧更新接口。实现此接口的服务会在 Layer.Pump 时被调用 Update。
/// </summary>
public interface IUpdate
{
    void Update();
}