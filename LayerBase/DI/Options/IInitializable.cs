namespace LayerBase.DI.Options;

/// <summary>
/// 服务初始化接口。在依赖注入完成后调用 Initialize 进行初始化。
/// </summary>
public interface IInitializable
{
    void Initialize();
}