namespace LayerBase;

/// <summary>
/// Build 完成后调用。
///
/// 此时：
/// 1. DI 已经完成。
/// 2. 自动订阅已经完成。
/// 3. Call 路由已经完成。
/// 4. SharedField 已经完成。
///
/// 适合做跨服务的最终检查或缓存预热。
/// </summary>
public interface IPostBuild
{
    void PostBuild();
}