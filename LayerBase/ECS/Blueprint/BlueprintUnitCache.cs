using System.Runtime.CompilerServices;

namespace LayerBase.ECS;

/// <summary>
/// Blueprint / Bundle 的泛型实例缓存。
/// </summary>
/// <typeparam name="TUnit">要缓存的 BlueprintUnit 类型。</typeparam>
internal static class BlueprintUnitCache<TUnit>
    where TUnit : class, IBlueprintUnit, new()
{
    public static readonly TUnit Instance = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Config(ref EntityBlueprintBuilder builder)
    {
        Instance.Config(ref builder);
    }
}