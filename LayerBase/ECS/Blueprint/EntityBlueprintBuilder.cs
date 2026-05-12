using System.Runtime.CompilerServices;

namespace LayerBase.ECS;

/// <summary>
/// 实体结构构建器。
/// 用于声明实体需要的组件、Bundle 和 Actor 投影。
/// </summary>
public ref struct EntityBlueprintBuilder
{
    private readonly EntityBlueprint _blueprint;

    public EntityBlueprintBuilder()
    {
        _blueprint = new EntityBlueprint();
    }

    /// <summary>
    /// 声明一个 ECS 组件。
    /// </summary>
    /// <typeparam name="TComponent">组件类型，必须实现 IComponent。</typeparam>
    /// <returns>当前构建器，用于链式调用。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityBlueprintBuilder WithComponent<TComponent>()
        where TComponent : struct, Core.IComponent
    {
        _blueprint.AddComponent<TComponent>();
        return this;
    }

    /// <summary>
    /// 展开一个 Bundle 的结构切片。
    /// </summary>
    /// <typeparam name="TBundle">Bundle 类型，必须实现 IBundle。</typeparam>
    /// <returns>当前构建器，用于链式调用。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityBlueprintBuilder WithBundle<TBundle>()
        where TBundle : class, IBundle, new()
    {
        BlueprintUnitCache<TBundle>.Config(ref this);
        return this;
    }

    /// <summary>
    /// 声明延迟 Actor 投影。
    /// </summary>
    /// <typeparam name="TActor">Actor 类型，必须实现 IPooledActor。</typeparam>
    /// <returns>当前构建器，用于链式调用。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityBlueprintBuilder WithProjectedActor<TActor>()
        where TActor : class, Actor.IPooledActor, new()
    {
        _blueprint.SetProjectedActor<TActor>();
        return this;
    }

    /// <summary>
    /// 声明创建实体时立即绑定 Actor。
    /// </summary>
    /// <typeparam name="TActor">Actor 类型，必须实现 IActor。</typeparam>
    /// <returns>当前构建器，用于链式调用。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityBlueprintBuilder WithActor<TActor>()
        where TActor : class, Actor.IActor, new()
    {
        _blueprint.SetActor<TActor>();
        return this;
    }

    /// <summary>
    /// 构建最终的 EntityBlueprint。
    /// </summary>
    /// <returns>构建好的 EntityBlueprint。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal EntityBlueprint Build()
    {
        return _blueprint;
    }
}
