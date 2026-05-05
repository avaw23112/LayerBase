using System;

namespace LayerBase.DI.Options;

/// <summary>
/// MountAttribute 用于声明 LayerBase 的自动挂载 / 自动注入目标。
///
/// 用法一：
///   [Mount]
///   private CombatService _service;
///
/// 用法二：
///   [Mount]
///   private DamageManager _manager;
///
/// 用法三：
///   [Mount(typeof(DamageManager))]
///   private IDamageManager _manager;
///
/// 用法四：
///   [Mount]
///   private SomeManager(SomeDependency dep) { }
/// </summary>
[AttributeUsage(
    AttributeTargets.Field |
    AttributeTargets.Property |
    AttributeTargets.Constructor,
    AllowMultiple = false,
    Inherited = true)]
public sealed class MountAttribute : Attribute
{
    /// <summary>
    /// 创建默认 Mount 标记。
    ///
    /// 字段 / 属性：
    ///   表示由 LayerBase 自动挂载或自动注入。
    ///
    /// 构造函数：
    ///   表示 DI 应选择该构造函数。
    /// </summary>
    public MountAttribute()
    {
    }

    /// <summary>
    /// 创建带实现类型的 Mount 标记。
    ///
    /// 主要用于字段 / 属性类型是 interface 或 abstract 的情况。
    /// </summary>
    /// <param name="implementationType">
    /// 实际实现类型。
    /// 例如字段类型是 IDamageManager，
    /// implementationType 可以是 typeof(DamageManager)。
    /// </param>
    public MountAttribute(Type implementationType)
    {
        ImplementationType = implementationType ?? throw new ArgumentNullException(nameof(implementationType));
    }

    /// <summary>
    /// 显式指定的实现类型。
    ///
    /// null：
    ///   表示未指定实现类型，使用字段 / 属性类型本身作为实现类型。
    ///
    /// 非 null：
    ///   表示生成器应使用该类型作为 DI 注册实现类型。
    /// </summary>
    public Type? ImplementationType { get; }
}
