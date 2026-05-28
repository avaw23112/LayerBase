# LayerBase ProjectedActor Touch / Disable / Generated Registration 最终执行方案

## 1. 目标

本方案用于一次性落地 ProjectedActor 的以下改造：

```text
1. 保留 TouchProjectedActor 作为唯一兴趣刷新 API。
2. 增加 ActorOptionsAttribute，用于描述 ProjectedActor 的类型级策略。
3. 将 OnEnable / OnDisable 内化到 IPooledActor，视为池化 Actor 的必要语义。
4. 在 ProjectedActorTypeRegistry.RegisterGenerated 中缓存 ProjectedActorOptions。
5. 保留旧 RegisterGenerated 作为兼容冷路径，允许首次反射读取特性并缓存。
6. 新增带 ProjectedActorOptions 的 RegisterGenerated overload，供源生成器生成的一次性注册入口使用。
7. 源生成器生成 GeneratedProjectedActorTypes.RegisterAll。
8. 在 LayerRuntime.LayersBuilder.Build 阶段接入 RegisterAll。
9. EntityCreateBuilder 最终不再承担 ProjectedActor 类型注册职责。
10. Touch / Post / Sweep / Ensure 等热路径不使用反射、不使用 Dictionary。
11. 增加 Disable 退场策略，使 Actor 失去兴趣后进入轻量挂起，而不是直接 ReturnToPool。
12. 增加 Touch 节流、Sweep 预算化、ProjectionBatchBuffer 容量预测、DirtyProjectionSet。
```

---

## 2. 不允许做的事

```text
1. 不新增 MarkProjectedActor / UnmarkProjectedActor 业务 API。
2. 不新增 IEnable / IDisable / IActorEnableDisable 接口。
3. 不新增 ProjectedActorLifecycleCallback。
4. 不新增 ProjectedActorGeneratedRegistry。
5. 不替换 ProjectedActorTypeRegistry 原有注册职责。
6. 不删除 RegisterGenerated。
7. 不改写 CreateActorByTypeId 的原有语义。
8. 不使用 CreateTypesById / CreateFactoriesById 这类 partial 初始化方法替代原有功能。
9. 不在 Touch / Post / Sweep / Ensure 热路径使用反射。
10. 不在热路径使用 Dictionary。
11. 不把本次任务扩展成 ProjectedActorTypeRegistry 全量重构。
12. 未明确标注为重构的扩展，必须延续 LayerBase 既有设计、命名、目录结构和性能风格。
```

---

## 3. 当前仓库中的接入事实

当前 Runtime 创建链路为：

```text
LayerHub.CreateLayers()
→ new LayerRuntime(id)
→ new LayerRuntime.LayersBuilder(runtime)
→ 用户 Push Layer
→ LayersBuilder.Build()
```

当前 `LayerRuntime` 构造函数会执行：

```text
1. 创建 EventCenter。
2. 创建 ActorWorld。
3. 创建 WorldServiceRoot。
4. InitializeEcsWorld。
5. LayerHub.Internal_Register。
```

当前 `InitializeEcsWorld` 只做：

```csharp
internal void InitializeEcsWorld()
{
    EcsWorld = World.Create();
    EcsWorld.BindRuntime(this);
}
```

当前 `LayersBuilder.Build()` 的关键顺序为：

```text
1. 安装 LayerBaseSynchronizationContext。
2. 创建 WorldTaskApi。
3. _layerChain.Prebuild();
4. 设置 FixedUpdateOptions。
5. InitializeScheduler。
6. InitializeTimer。
7. InitializeDelay。
8. BuildServiceProvider。
9. Actors.PrepareRuntimeBuild();
10. _layerChain.Build(1024, true);
11. Actors.CompleteRuntimeBuild();
12. BuildFullSnapCache();
13. PolicyTable.Freeze();
```

`GeneratedProjectedActorTypes.RegisterAll()` 必须接入 `LayersBuilder.Build()`，并且必须早于 `_layerChain.Build(1024, true)`。

---

## 4. 最终调用链路

推荐默认链路：

```text
LayerHub.CreateLayers()
→ new LayerRuntime(id)
→ new LayersBuilder(runtime)
→ Push Layer
→ LayersBuilder.Build()
→ _layerChain.Prebuild()
→ GeneratedProjectedActorTypes.RegisterAll()
→ InitializeScheduler / InitializeTimer / InitializeDelay
→ BuildServiceProvider
→ Actors.PrepareRuntimeBuild()
→ _layerChain.Build(1024, true)
→ Actors.CompleteRuntimeBuild()
```

如果 agent 检查后发现 `_layerChain.Prebuild()` 内部可能创建 ECS Entity，或者可能调用 `WithProjectedActor<TActor>()`，则必须改为：

```text
LayersBuilder.Build()
→ GeneratedProjectedActorTypes.RegisterAll()
→ _layerChain.Prebuild()
→ 后续 Build 流程
```

默认接入位置建议：

```csharp
_runtime.Tasks = new WorldTaskApi(_runtime._context);
_layerChain.Prebuild();

LayerBase.ECS.Projection.Generated.GeneratedProjectedActorTypes.RegisterAll();

_runtime._fixedUpdateOptions = _fixedUpdateOptions;
_runtime.InitializeScheduler(_postOptions);
```

保守接入位置建议：

```csharp
_runtime.Tasks = new WorldTaskApi(_runtime._context);

LayerBase.ECS.Projection.Generated.GeneratedProjectedActorTypes.RegisterAll();

_layerChain.Prebuild();
_runtime._fixedUpdateOptions = _fixedUpdateOptions;
```

agent 必须先检查 `LayerChain.Prebuild()` 的真实实现，再决定采用默认接入还是保守接入。

---

## 5. Touch 语义

`TouchProjectedActor` 是唯一兴趣刷新 API。

```text
TouchProjectedActor = 当前 Query 命中的 Entity 仍然处于兴趣范围内。
```

以下行为表示兴趣命中：

```text
1. TouchProjectedActor()
2. ProjectResult.Touch
3. ProjectResult.Post
```

以下行为不表示兴趣命中：

```text
1. predicate 过滤失败。
2. ProjectResult.Fail。
3. Entity 已销毁。
4. Entity 不带 ProjectedActorRef。
```

`ProjectResult` 语义必须保持：

```text
ProjectResult.Fail:
  不 Touch，不 Post。

ProjectResult.Touch:
  Touch，但不 Post。

ProjectResult.Post:
  Touch，并 Post。
```

---

## 6. Disable / ReturnToPool 语义

### 6.1 Disable

```text
Disable = 轻量挂起，不回池。
```

当 Actor 失去兴趣并超过 KeepAlive 后，如果 `RetirePolicy = Disable`：

```text
Active -> Disabled
调用 IPooledActor.OnDisable()
保留 ActorId
保留 Entity 绑定
保留 ProjectedActorRef.ActorId
不调用 IPooledActor.OnReturn()
不归还对象池
```

当该 Entity 再次被 Touch：

```text
Disabled -> Active
调用 IPooledActor.OnEnable()
刷新 RecycleDeadlineTicks
不调用 IPooledActor.OnRent()
ActorId 不变
Entity 绑定不变
```

### 6.2 ReturnToPool

当 Actor 失去兴趣并超过 KeepAlive 后，如果 `RetirePolicy = ReturnToPool`：

```text
Active -> Released
调用 IPooledActor.OnReturn()
清理 ActorId
清理 Entity 绑定
归还对象池
```

当 Entity 再次被 Touch：

```text
重新租出 Actor
调用 IPooledActor.OnRent()
重新绑定 Entity
```

---

## 7. IPooledActor 修改

修改真实 `IPooledActor` 定义文件。不要新增其它 Enable / Disable 接口。

```csharp
namespace LayerBase.Actor;

/// <summary>
/// IPooledActor 表示可被 ProjectedActor 对象池复用的 Actor。
///
/// 设计语义：
/// 1. OnRent / OnReturn 对应对象池租出和归还。
/// 2. OnEnable / OnDisable 对应 Disable 策略下的轻量恢复和挂起。
/// 3. Disable / Enable 不等于 Return / Rent。
/// </summary>
public interface IPooledActor : IActor
{
    /// <summary>
    /// RecycleDeadlineTicks 参数作用：
    /// 记录 Actor 的兴趣截止时间。
    /// Touch 会刷新该值。
    /// Sweep 到期后按 RetirePolicy 处理。
    /// </summary>
    long RecycleDeadlineTicks { get; set; }

    /// <summary>
    /// OnRent 作用：
    /// Actor 从对象池租出时调用。
    /// 完整初始化放在这里。
    /// </summary>
    void OnRent();

    /// <summary>
    /// OnReturn 作用：
    /// Actor 归还对象池前调用。
    /// 完整清理放在这里。
    /// </summary>
    void OnReturn();

    /// <summary>
    /// OnEnable 作用：
    /// Actor 从 Disabled 恢复为 Active 时调用。
    /// 只做轻量恢复，不做完整初始化。
    /// </summary>
    void OnEnable();

    /// <summary>
    /// OnDisable 作用：
    /// Actor 从 Active 进入 Disabled 时调用。
    /// 只做轻量挂起，不做完整清理。
    /// </summary>
    void OnDisable();
}
```

兼容要求：

```text
1. 所有现有 IPooledActor 实现必须补空 OnEnable / OnDisable。
2. 不允许改成可选接口。
3. 不允许新增 IEnable / IDisable。
4. Disable / Enable 必须直接通过 IPooledActor 调用。
```

---

## 8. ActorOptionsAttribute

新增文件：

```text
LayerBase/Actor/Core/ActorOptionsAttribute.cs
```

```csharp
using System;
using LayerBase.ECS.Projection;

namespace LayerBase.Actor;

/// <summary>
/// ActorOptionsAttribute 用于声明 ProjectedActor 的类型级默认策略。
///
/// 约束：
/// 1. 源生成器应在构建期读取该特性。
/// 2. 兼容旧路径时，RegisterGenerated 可以在冷路径读取一次该特性。
/// 3. Touch / Post / Sweep / Ensure 热路径绝不读取该特性。
/// 4. 事件投递、缓冲、背压仍由 EventMetaData 管理。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ActorOptionsAttribute : Attribute
{
    /// <summary>
    /// RetirePolicy 参数作用：
    /// 指定 Actor 失去兴趣并超过 KeepAlive 后的退场方式。
    /// </summary>
    public ProjectedActorRetirePolicy RetirePolicy { get; }

    /// <summary>
    /// CreatePolicy 参数作用：
    /// 指定 ProjectedActor 首次创建时机。
    /// </summary>
    public ProjectedActorCreatePolicy CreatePolicy { get; }

    /// <summary>
    /// KeepAliveSeconds 参数作用：
    /// Actor 最后一次兴趣命中后还能保持 Active 的秒数。
    /// </summary>
    public float KeepAliveSeconds { get; }

    /// <summary>
    /// TouchIntervalSeconds 参数作用：
    /// 两次真实 Touch 之间的最小间隔。
    /// </summary>
    public float TouchIntervalSeconds { get; }

    /// <summary>
    /// 构造 ActorOptionsAttribute。
    ///
    /// retirePolicy 参数作用：
    /// 指定失去兴趣后的处理方式。
    ///
    /// createPolicy 参数作用：
    /// 指定首次创建时机。
    ///
    /// keepAliveSeconds 参数作用：
    /// 指定兴趣保活时间。
    ///
    /// touchIntervalSeconds 参数作用：
    /// 指定 Touch 节流时间。
    /// </summary>
    public ActorOptionsAttribute(
        ProjectedActorRetirePolicy retirePolicy = ProjectedActorRetirePolicy.ReturnToPool,
        ProjectedActorCreatePolicy createPolicy = ProjectedActorCreatePolicy.Lazy,
        float keepAliveSeconds = 0.5f,
        float touchIntervalSeconds = 0.1f)
    {
        RetirePolicy = retirePolicy;
        CreatePolicy = createPolicy;
        KeepAliveSeconds = keepAliveSeconds;
        TouchIntervalSeconds = touchIntervalSeconds;
    }
}
```

---

## 9. ProjectedActorPolicies

新增文件：

```text
LayerBase/ECS/Projection/ProjectedActorPolicies.cs
```

```csharp
namespace LayerBase.ECS.Projection;

/// <summary>
/// ProjectedActorRetirePolicy 表示 ProjectedActor 失去兴趣后的退场方式。
/// </summary>
public enum ProjectedActorRetirePolicy : byte
{
    /// <summary>
    /// Disable 参数作用：
    /// Actor 失去兴趣后只进入 Disabled 状态。
    /// 不调用 OnReturn，不清理 ActorId，不改变 Entity 绑定。
    /// </summary>
    Disable = 0,

    /// <summary>
    /// ReturnToPool 参数作用：
    /// Actor 失去兴趣后归还对象池。
    /// 会调用 OnReturn，下次重新命中时会调用 OnRent。
    /// </summary>
    ReturnToPool = 1,

    /// <summary>
    /// DestroyImmediately 参数作用：
    /// Actor 失去兴趣后直接销毁。
    /// </summary>
    DestroyImmediately = 2,

    /// <summary>
    /// DetachAndLetActorFinish 参数作用：
    /// Entity 与 Actor 解绑，但允许 Actor 自行完成剩余事件或收尾逻辑。
    /// </summary>
    DetachAndLetActorFinish = 3
}

/// <summary>
/// ProjectedActorCreatePolicy 表示 ProjectedActor 首次创建时机。
/// </summary>
public enum ProjectedActorCreatePolicy : byte
{
    /// <summary>
    /// Lazy 参数作用：
    /// WithProjectedActor 时只写配置，首次 Touch / Post 时创建 Actor。
    /// </summary>
    Lazy = 0,

    /// <summary>
    /// OnMark 参数作用：
    /// WithProjectedActor 时立即创建 Actor。
    /// 这里的 Mark 是内部投影资格写入，不是业务 API。
    /// </summary>
    OnMark = 1
}
```

---

## 10. ProjectedActorOptions

新增文件：

```text
LayerBase/ECS/Projection/ProjectedActorOptions.cs
```

```csharp
namespace LayerBase.ECS.Projection;

/// <summary>
/// ProjectedActorOptions 是 ActorOptionsAttribute 转换后的运行时缓存数据。
///
/// 约束：
/// 1. Touch / Post / Sweep / Ensure 只读该结构。
/// 2. 不在热路径读取 Attribute。
/// 3. 不在热路径使用 Dictionary。
/// </summary>
internal readonly struct ProjectedActorOptions
{
    /// <summary>
    /// RetirePolicy 字段作用：
    /// Actor 失去兴趣后的退场方式。
    /// </summary>
    public readonly ProjectedActorRetirePolicy RetirePolicy;

    /// <summary>
    /// CreatePolicy 字段作用：
    /// Actor 首次创建时机。
    /// </summary>
    public readonly ProjectedActorCreatePolicy CreatePolicy;

    /// <summary>
    /// KeepAliveTicks 字段作用：
    /// 最后一次兴趣命中后的保活时长。
    /// </summary>
    public readonly long KeepAliveTicks;

    /// <summary>
    /// TouchIntervalTicks 字段作用：
    /// 两次真实 Touch 之间的最小间隔。
    /// </summary>
    public readonly long TouchIntervalTicks;

    /// <summary>
    /// Default 属性作用：
    /// 未配置 ActorOptionsAttribute 时的默认策略。
    /// </summary>
    public static ProjectedActorOptions Default =>
        new ProjectedActorOptions(
            ProjectedActorRetirePolicy.ReturnToPool,
            ProjectedActorCreatePolicy.Lazy,
            ProjectedActorTime.SecondsToTicks(0.5f),
            ProjectedActorTime.SecondsToTicks(0.1f));

    /// <summary>
    /// 构造 ProjectedActorOptions。
    ///
    /// retirePolicy 参数作用：
    /// Actor 失去兴趣后的退场方式。
    ///
    /// createPolicy 参数作用：
    /// Actor 首次创建时机。
    ///
    /// keepAliveTicks 参数作用：
    /// 兴趣保活时长。
    ///
    /// touchIntervalTicks 参数作用：
    /// Touch 节流时长。
    /// </summary>
    public ProjectedActorOptions(
        ProjectedActorRetirePolicy retirePolicy,
        ProjectedActorCreatePolicy createPolicy,
        long keepAliveTicks,
        long touchIntervalTicks)
    {
        RetirePolicy = retirePolicy;
        CreatePolicy = createPolicy;
        KeepAliveTicks = keepAliveTicks;
        TouchIntervalTicks = touchIntervalTicks;
    }
}
```

---

## 11. ProjectedActorTypeRegistry 改造

修改文件：

```text
LayerBase/ECS/Projection/ProjectedActorTypeRegistry.cs
```

### 11.1 新增字段

在 `_typesById` 和 `_factoriesById` 附近新增：

```csharp
/// <summary>
/// _optionsById 字段作用：
/// ActorTypeId -> ProjectedActorOptions。
///
/// 旧 RegisterGenerated 会冷路径反射读取 ActorOptionsAttribute。
/// 新 RegisterGenerated overload 由源生成器直接传入 options，不反射。
/// </summary>
private static ProjectedActorOptions[] _optionsById = new ProjectedActorOptions[64];

/// <summary>
/// _optionsInitializedById 字段作用：
/// 标记某个 ActorTypeId 是否已经初始化 options。
///
/// 作用：
/// 1. 避免旧 RegisterGenerated 被重复调用时重复反射。
/// 2. 保证同一个 actorTypeId 只解析一次 ActorOptions。
/// </summary>
private static bool[] _optionsInitializedById = new bool[64];
```

### 11.2 保留旧 RegisterGenerated 并缓存 options

```csharp
public static void RegisterGenerated(
    int actorTypeId,
    Type actorType,
    ProjectedActorFactory factory)
{
    EnsureCapacity(actorTypeId);

    _typesById[actorTypeId] = actorType;
    _factoriesById[actorTypeId] = factory;

    if (!_optionsInitializedById[actorTypeId])
    {
        _optionsById[actorTypeId] =
            CreateOptionsFromAttribute(actorType);

        _optionsInitializedById[actorTypeId] = true;
    }
}
```

### 11.3 新增无反射 overload

```csharp
public static void RegisterGenerated(
    int actorTypeId,
    Type actorType,
    ProjectedActorFactory factory,
    in ProjectedActorOptions options)
{
    EnsureCapacity(actorTypeId);

    _typesById[actorTypeId] = actorType;
    _factoriesById[actorTypeId] = factory;
    _optionsById[actorTypeId] = options;
    _optionsInitializedById[actorTypeId] = true;
}
```

### 11.4 冷路径反射方法

```csharp
private static ProjectedActorOptions CreateOptionsFromAttribute(Type actorType)
{
    // 该方法只允许被旧 RegisterGenerated 调用。
    // Touch / Post / Sweep / Ensure 绝不能调用该方法。
    object[] attrs = actorType.GetCustomAttributes(
        typeof(LayerBase.Actor.ActorOptionsAttribute),
        inherit: false);

    if (attrs.Length == 0 ||
        attrs[0] is not LayerBase.Actor.ActorOptionsAttribute attr)
    {
        return ProjectedActorOptions.Default;
    }

    return new ProjectedActorOptions(
        attr.RetirePolicy,
        attr.CreatePolicy,
        ProjectedActorTime.SecondsToTicks(attr.KeepAliveSeconds),
        ProjectedActorTime.SecondsToTicks(attr.TouchIntervalSeconds));
}
```

### 11.5 GetOptions

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static ProjectedActorOptions GetOptions(int actorTypeId)
{
    if ((uint)actorTypeId >= (uint)_optionsById.Length)
    {
        return ProjectedActorOptions.Default;
    }

    if (!_optionsInitializedById[actorTypeId])
    {
        return ProjectedActorOptions.Default;
    }

    return _optionsById[actorTypeId];
}
```

### 11.6 EnsureCapacity 同步扩容

```csharp
private static void EnsureCapacity(int actorTypeId)
{
    if ((uint)actorTypeId < (uint)_factoriesById.Length)
    {
        return;
    }

    int newLength = _factoriesById.Length;
    while ((uint)actorTypeId >= (uint)newLength)
    {
        newLength <<= 1;
    }

    Array.Resize(ref _typesById, newLength);
    Array.Resize(ref _factoriesById, newLength);
    Array.Resize(ref _optionsById, newLength);
    Array.Resize(ref _optionsInitializedById, newLength);
}
```

---

## 12. GeneratedProjectedActorTypes

生成文件：

```text
LayerBase/ECS/Projection/Generated/GeneratedProjectedActorTypes.g.cs
```

命名空间：

```csharp
namespace LayerBase.ECS.Projection.Generated;
```

生成代码形态：

```csharp
// <auto-generated />

using LayerBase.Actor;

namespace LayerBase.ECS.Projection.Generated;

/// <summary>
/// GeneratedProjectedActorTypes 是源生成器生成的 ProjectedActor 类型注册入口。
///
/// 职责：
/// 1. 在 Runtime / Layer build 阶段一次性注册所有可投影 Actor。
/// 2. 调用 ProjectedActorTypeRegistry.RegisterGenerated。
/// 3. 不参与 Entity 创建热路径。
/// 4. 不参与 Touch / Post / Sweep 热路径。
/// </summary>
internal static class GeneratedProjectedActorTypes
{
    /// <summary>
    /// _registered 字段作用：
    /// 防止静态 Registry 路径下重复注册。
    ///
    /// 注意：
    /// 当前 ProjectedActorTypeRegistry 是静态数组表，因此使用静态 registered 即可。
    /// 如果未来 Registry 改成每 Runtime 独立，则该字段必须迁移到 Runtime 级别。
    /// 本任务不做多 Runtime Registry 重构。
    /// </summary>
    private static bool _registered;

    /// <summary>
    /// RegisterAll 作用：
    /// 一次性把所有可投影 Actor 注册进 ProjectedActorTypeRegistry。
    ///
    /// 调用时机：
    /// LayerRuntime.LayersBuilder.Build 中，早于 _layerChain.Build。
    /// </summary>
    public static void RegisterAll()
    {
        if (_registered)
        {
            return;
        }

        _registered = true;

        ProjectedActorTypeRegistry.RegisterGenerated(
            ActorType<global::Game.Actors.MonsterActor>.Id,
            typeof(global::Game.Actors.MonsterActor),
            static actorWorld => actorWorld.CreateProjectedActor<global::Game.Actors.MonsterActor>(),
            new ProjectedActorOptions(
                ProjectedActorRetirePolicy.Disable,
                ProjectedActorCreatePolicy.Lazy,
                ProjectedActorTime.SecondsToTicks(0.5f),
                ProjectedActorTime.SecondsToTicks(0.1f)));

        ProjectedActorTypeRegistry.RegisterGenerated(
            ActorType<global::Game.Actors.ShopNpcActor>.Id,
            typeof(global::Game.Actors.ShopNpcActor),
            static actorWorld => actorWorld.CreateProjectedActor<global::Game.Actors.ShopNpcActor>(),
            new ProjectedActorOptions(
                ProjectedActorRetirePolicy.ReturnToPool,
                ProjectedActorCreatePolicy.OnMark,
                ProjectedActorTime.SecondsToTicks(1.0f),
                ProjectedActorTime.SecondsToTicks(0.2f)));
    }
}
```

空项目也必须生成空实现：

```csharp
// <auto-generated />

namespace LayerBase.ECS.Projection.Generated;

/// <summary>
/// GeneratedProjectedActorTypes 是源生成器生成的 ProjectedActor 类型注册入口。
/// 当前项目没有可注册的 IPooledActor。
/// </summary>
internal static class GeneratedProjectedActorTypes
{
    public static void RegisterAll()
    {
    }
}
```

这样 `LayersBuilder.Build()` 可以无条件调用。

---

## 13. 源生成器核心代码

新增或扩展：

```text
LayerBase.Generator/ProjectedActorRegistryGenerator.cs
```

```csharp
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace LayerBase.Generator;

/// <summary>
/// ProjectedActorRegistryGenerator 生成 GeneratedProjectedActorTypes.RegisterAll。
///
/// 生成目标：
/// 在 Runtime / Layer build 阶段一次性注册所有 IPooledActor。
/// </summary>
[Generator]
public sealed class ProjectedActorRegistryGenerator : IIncrementalGenerator
{
    /// <summary>
    /// Initialize 参数作用：
    /// context 表示 Roslyn 增量生成器上下文。
    /// </summary>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<ProjectedActorRecord?> actors =
            context.SyntaxProvider.CreateSyntaxProvider(
                    static (node, _) => node is ClassDeclarationSyntax,
                    static (ctx, _) => BuildRecord(ctx))
                .Where(static item => item.HasValue);

        context.RegisterSourceOutput(
            actors.Collect(),
            static (spc, records) => Emit(spc, records));
    }

    /// <summary>
    /// BuildRecord 参数作用：
    /// context 表示当前语法节点和语义模型。
    ///
    /// 返回值：
    /// 返回可注册的 ProjectedActorRecord。
    /// 如果类型不是 IPooledActor，则返回 null。
    /// </summary>
    private static ProjectedActorRecord? BuildRecord(GeneratorSyntaxContext context)
    {
        if (context.Node is not ClassDeclarationSyntax classDecl)
        {
            return null;
        }

        if (context.SemanticModel.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol typeSymbol)
        {
            return null;
        }

        if (!ImplementsInterface(typeSymbol, "LayerBase.Actor.IPooledActor"))
        {
            return null;
        }

        ActorOptionsData options = ReadActorOptions(typeSymbol);

        string fullTypeName =
            typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        return new ProjectedActorRecord(fullTypeName, options);
    }

    /// <summary>
    /// ImplementsInterface 参数作用：
    /// typeSymbol 表示待检查类型。
    /// interfaceFullName 表示接口完整名称。
    ///
    /// 返回值：
    /// true 表示该类型实现了目标接口。
    /// </summary>
    private static bool ImplementsInterface(
        INamedTypeSymbol typeSymbol,
        string interfaceFullName)
    {
        foreach (INamedTypeSymbol item in typeSymbol.AllInterfaces)
        {
            if (item.ToDisplayString() == interfaceFullName)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// ReadActorOptions 参数作用：
    /// typeSymbol 表示 Actor 类型。
    ///
    /// 返回值：
    /// 返回构建期解析出的 ActorOptionsData。
    /// 没有特性时返回默认值。
    /// </summary>
    private static ActorOptionsData ReadActorOptions(INamedTypeSymbol typeSymbol)
    {
        const string attrFullName = "LayerBase.Actor.ActorOptionsAttribute";

        foreach (AttributeData attr in typeSymbol.GetAttributes())
        {
            if (attr.AttributeClass == null ||
                attr.AttributeClass.ToDisplayString() != attrFullName)
            {
                continue;
            }

            string retirePolicy = "ProjectedActorRetirePolicy.ReturnToPool";
            string createPolicy = "ProjectedActorCreatePolicy.Lazy";
            float keepAliveSeconds = 0.5f;
            float touchIntervalSeconds = 0.1f;

            ImmutableArray<TypedConstant> args = attr.ConstructorArguments;

            if (args.Length > 0 && args[0].Value != null)
            {
                retirePolicy = "ProjectedActorRetirePolicy." + args[0].Value;
            }

            if (args.Length > 1 && args[1].Value != null)
            {
                createPolicy = "ProjectedActorCreatePolicy." + args[1].Value;
            }

            if (args.Length > 2 && args[2].Value is float keepAlive)
            {
                keepAliveSeconds = keepAlive;
            }

            if (args.Length > 3 && args[3].Value is float touchInterval)
            {
                touchIntervalSeconds = touchInterval;
            }

            foreach (KeyValuePair<string, TypedConstant> named in attr.NamedArguments)
            {
                switch (named.Key)
                {
                    case "RetirePolicy":
                        if (named.Value.Value != null)
                        {
                            retirePolicy = "ProjectedActorRetirePolicy." + named.Value.Value;
                        }
                        break;

                    case "CreatePolicy":
                        if (named.Value.Value != null)
                        {
                            createPolicy = "ProjectedActorCreatePolicy." + named.Value.Value;
                        }
                        break;

                    case "KeepAliveSeconds":
                        if (named.Value.Value is float namedKeepAlive)
                        {
                            keepAliveSeconds = namedKeepAlive;
                        }
                        break;

                    case "TouchIntervalSeconds":
                        if (named.Value.Value is float namedTouchInterval)
                        {
                            touchIntervalSeconds = namedTouchInterval;
                        }
                        break;
                }
            }

            return new ActorOptionsData(
                retirePolicy,
                createPolicy,
                keepAliveSeconds,
                touchIntervalSeconds);
        }

        return ActorOptionsData.Default;
    }

    /// <summary>
    /// Emit 参数作用：
    /// context 表示源码输出上下文。
    /// records 表示收集到的 ProjectedActorRecord。
    /// </summary>
    private static void Emit(
        SourceProductionContext context,
        ImmutableArray<ProjectedActorRecord?> records)
    {
        List<ProjectedActorRecord> actors = new(records.Length);

        foreach (ProjectedActorRecord? record in records)
        {
            if (record.HasValue)
            {
                actors.Add(record.Value);
            }
        }

        actors.Sort(static (a, b) =>
            string.CompareOrdinal(a.FullTypeName, b.FullTypeName));

        string source = BuildSource(actors);

        context.AddSource(
            "GeneratedProjectedActorTypes.g.cs",
            SourceText.From(source, Encoding.UTF8));
    }

    /// <summary>
    /// BuildSource 参数作用：
    /// actors 表示所有可注册的 ProjectedActor 类型。
    ///
    /// 返回值：
    /// 返回生成的 C# 源码。
    /// </summary>
    private static string BuildSource(List<ProjectedActorRecord> actors)
    {
        StringBuilder sb = new();

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("using LayerBase.Actor;");
        sb.AppendLine();
        sb.AppendLine("namespace LayerBase.ECS.Projection.Generated;");
        sb.AppendLine();
        sb.AppendLine("internal static class GeneratedProjectedActorTypes");
        sb.AppendLine("{");

        if (actors.Count > 0)
        {
            sb.AppendLine("    private static bool _registered;");
            sb.AppendLine();
        }

        sb.AppendLine("    public static void RegisterAll()");
        sb.AppendLine("    {");

        if (actors.Count > 0)
        {
            sb.AppendLine("        if (_registered)");
            sb.AppendLine("        {");
            sb.AppendLine("            return;");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        _registered = true;");
            sb.AppendLine();
        }

        foreach (ProjectedActorRecord actor in actors)
        {
            ActorOptionsData options = actor.Options;

            sb.AppendLine("        ProjectedActorTypeRegistry.RegisterGenerated(");
            sb.AppendLine($"            ActorType<{actor.FullTypeName}>.Id,");
            sb.AppendLine($"            typeof({actor.FullTypeName}),");
            sb.AppendLine($"            static actorWorld => actorWorld.CreateProjectedActor<{actor.FullTypeName}>(),");
            sb.AppendLine("            new ProjectedActorOptions(");
            sb.AppendLine($"                {options.RetirePolicy},");
            sb.AppendLine($"                {options.CreatePolicy},");
            sb.AppendLine($"                ProjectedActorTime.SecondsToTicks({options.KeepAliveSeconds.ToString(CultureInfo.InvariantCulture)}f),");
            sb.AppendLine($"                ProjectedActorTime.SecondsToTicks({options.TouchIntervalSeconds.ToString(CultureInfo.InvariantCulture)}f)));");
            sb.AppendLine();
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private readonly record struct ProjectedActorRecord(
        string FullTypeName,
        ActorOptionsData Options);

    private readonly record struct ActorOptionsData(
        string RetirePolicy,
        string CreatePolicy,
        float KeepAliveSeconds,
        float TouchIntervalSeconds)
    {
        public static ActorOptionsData Default =>
            new(
                "ProjectedActorRetirePolicy.ReturnToPool",
                "ProjectedActorCreatePolicy.Lazy",
                0.5f,
                0.1f);
    }
}
```

生成器要求：

```text
1. 扫描所有实现 IPooledActor 的 class。
2. 不扫描普通 IActor。
3. 不扫描 Layer。
4. 不扫描 EventMetaData。
5. 不使用运行时反射。
6. 生成代码不使用 Dictionary。
7. RegisterAll 使用 ActorType<TActor>.Id，不硬编码数字 ID。
```

---

## 14. LayerRuntime.LayersBuilder.Build 接入代码

修改文件：

```text
LayerBase/Application/LayerRuntime.cs
```

默认推荐实现：

```csharp
public LayerRuntime Build()
{
    if (_built) throw new InvalidOperationException("LayersBuilder.Build can only be called once.");
    if (_layerChain == null) throw new InvalidOperationException("No layers added.");
    _built = true;

    if (_runtime._context == null)
        _runtime._context = LayerBaseSynchronizationContext.Install();

    _runtime.Tasks = new WorldTaskApi(_runtime._context);
    _layerChain.Prebuild();

    // GeneratedProjectedActorTypes.RegisterAll 由源生成器生成。
    // 它负责把所有可投影 Actor 的 Type、Factory、ActorOptions 一次性注册到 ProjectedActorTypeRegistry。
    //
    // 放在这里的原因：
    // 1. 当前 Runtime 已经确认进入 Build。
    // 2. 早于 _layerChain.Build，避免 Layer Build 中创建 Entity 时 options 未注册。
    // 3. 不放在 EntityCreateBuilder，避免每个 Entity 创建时重复注册。
    // 4. 不放在 LayerRuntime 构造函数，避免未 Build Runtime 产生注册副作用。
    LayerBase.ECS.Projection.Generated.GeneratedProjectedActorTypes.RegisterAll();

    _runtime._fixedUpdateOptions = _fixedUpdateOptions;
    _runtime.InitializeScheduler(_postOptions);
    _runtime.InitializeTimer(_timerOptions);
    _runtime.InitializeDelay(_delayOptions);
    _runtime.BuildServiceProvider();
    _runtime.Actors.PrepareRuntimeBuild();
    _layerChain.Build(1024, true);
    _runtime.Actors.CompleteRuntimeBuild();
    _runtime.BuildFullSnapCache();
    _runtime.PolicyTable.Freeze();

    if (_debugMode)
    {
        _runtime.ReportInfo(new LayerEventInfo(-1, "System", "Topology", _runtime.GetTopologySummary(),
            LayerEventInfoType.Info));
        _runtime.ReportInfo(new LayerEventInfo(-1, "System", "TopologySnapshot", _runtime.GetTopologyMarkdown(),
            LayerEventInfoType.Info));
        _runtime.ReportInfo(new LayerEventInfo(-1, "System", "PolicyDump", _runtime.GetPolicyMarkdown(),
            LayerEventInfoType.Info));
    }

    return _runtime;
}
```

如果 agent 确认 `Prebuild()` 可能创建 Entity，则改成：

```csharp
_runtime.Tasks = new WorldTaskApi(_runtime._context);

LayerBase.ECS.Projection.Generated.GeneratedProjectedActorTypes.RegisterAll();

_layerChain.Prebuild();
```

---

## 15. EntityCreateBuilder 迁移

当前 `WithProjectedActor<TActor>()` 可能调用 `RegisterGenerated`。迁移分两步。

### 15.1 兼容阶段

暂时保留：

```csharp
// TODO(ProjectedActor Registry Migration):
// 该 RegisterGenerated 仅用于兼容旧路径。
// 最终注册应由 GeneratedProjectedActorTypes.RegisterAll 在 LayersBuilder.Build 阶段完成。
// EntityCreateBuilder 不应长期承担 ProjectedActor 类型注册职责。
ProjectedActorTypeRegistry.RegisterGenerated(
    actorTypeId,
    typeof(TActor),
    static actorWorld => actorWorld.CreateProjectedActor<TActor>());
```

此阶段依赖 `_optionsInitializedById` 避免重复反射。

### 15.2 最终阶段

移除 `RegisterGenerated`，保留：

```csharp
public EntityCreateBuilder WithProjectedActor<TActor>()
    where TActor : class, IPooledActor, new()
{
    int actorTypeId = ActorType<TActor>.Id;

    ProjectedActorOptions options =
        ProjectedActorTypeRegistry.GetOptions(actorTypeId);

    _actorTypeId = actorTypeId;
    _projectedActorOptions = options;
    _isCreatedActor = true;
    _componentTypes.Add(typeof(ProjectedActorRef));

    return this;
}
```

可选 Debug 校验：

```csharp
if (!ProjectedActorTypeRegistry.HasFactory(actorTypeId))
{
    throw new InvalidOperationException(
        $"ProjectedActor type {typeof(TActor).Name} was not registered. " +
        "Make sure GeneratedProjectedActorTypes.RegisterAll is called during LayerRuntime build.");
}
```

---

## 16. ProjectedActorRef / ProjectedActorMeta

### 16.1 ProjectedActorRef

新增字段：

```csharp
internal long TouchIntervalTicks;
internal long NextTouchTicks;
internal ProjectedActorRetirePolicy RetirePolicy;
internal ProjectedActorCreatePolicy CreatePolicy;
```

要求：

```text
1. Disable 不调用 ClearActor。
2. ReturnToPool / Destroy / Detach 才调用 ClearActor。
3. ActorId 在 Disabled 状态下保持有效。
```

### 16.2 ProjectedActorMeta

新增或扩展状态：

```csharp
internal enum ProjectedActorState : byte
{
    None = 0,
    Projectable = 1,
    Active = 2,
    Disabled = 3,
    Released = 4
}
```

新增字段：

```text
RetirePolicy
CreatePolicy
TouchIntervalTicks
NextTouchTicks
```

内部 Mark 工具接收 options：

```csharp
public static void MarkProjected(
    World world,
    Entity entity,
    ref ProjectedActorMeta meta,
    int actorTypeId,
    in ProjectedActorOptions options)
```

---

## 17. ProjectedActorBinding：Touch 节流与 Disabled 恢复

新增核心方法：

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static bool RefreshProjectedActorInterest(
    World world,
    ActorWorld actorWorld,
    Entity entity,
    ref ProjectedActorRef actorRef,
    long nowTicks)
{
    ActorId actorId = actorRef.ActorId;

    if (!actorId.IsValid)
    {
        actorId = EnsureProjectedActor(
            world,
            actorWorld,
            entity,
            ref actorRef,
            nowTicks);

        return actorId.IsValid;
    }

    if (actorWorld.IsProjectedActorDisabled(actorId))
    {
        if (!actorWorld.EnableProjectedActorIfDisabled(actorId))
        {
            ClearByEntity(world, entity, ref actorRef);
            return false;
        }

        RefreshDeadline(
            actorWorld,
            actorId,
            ref actorRef,
            nowTicks);

        return true;
    }

    if (nowTicks < actorRef.NextTouchTicks)
    {
        return true;
    }

    RefreshDeadline(
        actorWorld,
        actorId,
        ref actorRef,
        nowTicks);

    return true;
}

private static void RefreshDeadline(
    ActorWorld actorWorld,
    ActorId actorId,
    ref ProjectedActorRef actorRef,
    long nowTicks)
{
    if (!actorWorld.TryGetPooledActor(actorId, out IPooledActor pooledActor))
    {
        return;
    }

    pooledActor.RecycleDeadlineTicks =
        ProjectedActorTime.BuildDeadline(
            nowTicks,
            actorRef.KeepAliveTicks);

    actorRef.NextTouchTicks =
        nowTicks + actorRef.TouchIntervalTicks;
}
```

要求：

```text
1. ActorId 无效时不能被节流跳过，必须 Ensure。
2. Disabled 状态不能因为 NextTouchTicks 跳过 Enable。
3. Active 状态才允许节流直接 return true。
4. 不使用反射。
5. 不使用 Dictionary。
```

---

## 18. ActorWorld Disable / Enable

在现有 ActorWorld / BehaviourArchetype / Lifecycle 结构中增量实现，不另建管理器。

新增状态：

```csharp
internal enum ProjectedRuntimeState : byte
{
    None = 0,
    Active = 1,
    Disabled = 2,
    Released = 3
}
```

核心方法：

```csharp
internal bool DisableProjectedActor(ActorId actorId)
{
    if (!TryGetPooledActor(actorId, out IPooledActor actor))
    {
        return false;
    }

    if (IsProjectedActorDisabled(actorId))
    {
        return true;
    }

    actor.OnDisable();
    SetEnable(actorId, false);
    SetProjectedRuntimeState(actorId, ProjectedRuntimeState.Disabled);
    return true;
}

internal bool EnableProjectedActorIfDisabled(ActorId actorId)
{
    if (!IsProjectedActorDisabled(actorId))
    {
        return true;
    }

    if (!TryGetPooledActor(actorId, out IPooledActor actor))
    {
        return false;
    }

    SetEnable(actorId, true);
    actor.OnEnable();
    SetProjectedRuntimeState(actorId, ProjectedRuntimeState.Active);
    return true;
}
```

说明：

```text
1. 仓库当前 ActorWorld 已有 SetEnable / IsEnable 能力，应优先复用。
2. 不新增独立 lifecycle manager。
3. 不使用 callback。
4. 不使用反射。
5. 不使用 Dictionary。
```

---

## 19. Retire 处理

到期后：

```csharp
private static void RetireProjectedActor(
    World world,
    ActorWorld actorWorld,
    Entity entity,
    ref ProjectedActorMeta meta,
    ref ProjectedActorRef actorRef)
{
    switch (meta.RetirePolicy)
    {
        case ProjectedActorRetirePolicy.Disable:
            actorWorld.DisableProjectedActor(meta.ActorId);
            meta.State = ProjectedActorState.Disabled;
            return;

        case ProjectedActorRetirePolicy.ReturnToPool:
            actorWorld.ReleaseProjectedActor(
                meta.ActorId,
                ProjectedActorReleasePolicy.ReturnToPool);

            actorRef.ClearActor();
            meta.ClearActor();
            return;

        case ProjectedActorRetirePolicy.DestroyImmediately:
            actorWorld.DestroyActor(meta.ActorId);
            actorRef.ClearActor();
            meta.ClearActor();
            return;

        case ProjectedActorRetirePolicy.DetachAndLetActorFinish:
            actorWorld.DetachProjectedActor(meta.ActorId);
            actorRef.ClearActor();
            meta.ClearActor();
            return;
    }
}
```

---

## 20. Sweep 预算化

`ActiveProjectedActorList` 增加：

```csharp
private int _sweepCursor;
```

新增或替换 Sweep 签名：

```csharp
public void Sweep(
    World world,
    ActorWorld actorWorld,
    int maxCount)
```

要求：

```text
1. 单帧最多处理 maxCount 个。
2. 多帧轮转处理所有 active projected actor。
3. 不使用 Dictionary。
4. Disable 不清 ActorId。
```

`LayerRuntime.Pump` 中当前在 `Actors.Pump` 后调用：

```csharp
EcsWorld.SweepProjectedActors();
```

应改成带预算版本，例如：

```csharp
EcsWorld.SweepProjectedActors(maxCount: 512);
```

或把 512 配置为 Runtime / PostSchedulerOptions 中的字段。

---

## 21. ProjectionBatchBuffer 容量预测

修改：

```text
LayerBase/ECS/Projection/Flow/ProjectionBatchBuffer.cs
```

```csharp
public static ProjectionBatchBuffer<TEvent> Rent(int initialCapacity = 64)
{
    int safeCapacity = initialCapacity <= 0
        ? 64
        : initialCapacity;

    return new ProjectionBatchBuffer<TEvent>(
        ArrayPool<ActorId>.Shared.Rent(safeCapacity),
        ArrayPool<TEvent>.Shared.Rent(safeCapacity));
}
```

推荐增加测试用 internal 字段：

```csharp
internal int GrowCount { get; private set; }
```

`Grow()` 中：

```csharp
GrowCount++;
```

---

## 22. DirtyProjectionSet

新增文件：

```text
LayerBase/ECS/Projection/DirtyProjectionSet.cs
```

DirtyProjectionSet 只用于 Post 投影，不用于 Touch 保活。

```csharp
using System.Buffers;
using Arch.Core;

namespace LayerBase.ECS.Projection;

/// <summary>
/// DirtyProjectionSet 用于保存需要执行 Post 投影的 Entity。
///
/// 该结构不负责 Touch 保活。
/// 该结构不负责去重。
/// 去重由 DirtyTag / DirtyVersion 或调用方保证。
/// </summary>
internal sealed class DirtyProjectionSet : IDisposable
{
    private Entity[] _items;
    private int _count;

    public int Count => _count;

    public DirtyProjectionSet(int initialCapacity = 64)
    {
        _items = ArrayPool<Entity>.Shared.Rent(initialCapacity);
        _count = 0;
    }

    public void Add(Entity entity)
    {
        if ((uint)_count >= (uint)_items.Length)
        {
            Grow();
        }

        _items[_count++] = entity;
    }

    public ReadOnlySpan<Entity> AsSpan()
    {
        return _items.AsSpan(0, _count);
    }

    public void Clear()
    {
        _count = 0;
    }

    private void Grow()
    {
        int newLength = _items.Length << 1;
        Entity[] newItems = ArrayPool<Entity>.Shared.Rent(newLength);
        Array.Copy(_items, newItems, _count);
        ArrayPool<Entity>.Shared.Return(_items, clearArray: false);
        _items = newItems;
    }

    public void Dispose()
    {
        ArrayPool<Entity>.Shared.Return(_items, clearArray: false);
        _items = Array.Empty<Entity>();
        _count = 0;
    }
}
```

---

## 23. ProjectionExecutor 模板修改

修改文件：

```text
LayerBase/ECS/Projection/Templates/ProjectionExecutor.tt
```

顺序必须是：

```text
predicate
job.Execute / forEach
result == Fail -> continue
RefreshProjectedActorInterest
result == Touch -> continue
batch.Add
```

要求：

```text
1. predicate 过滤失败不会 Touch。
2. ProjectResult.Fail 不会 Touch。
3. ProjectResult.Touch 只 Touch，不 Add batch。
4. ProjectResult.Post Touch 后 Add batch。
5. Touch / Post 路径都走 RefreshProjectedActorInterest。
```

---

## 24. Agent 实施顺序

### Phase 1：基础类型与接口

```text
1. 新增 ActorOptionsAttribute。
2. 新增 ProjectedActorPolicies。
3. 新增 ProjectedActorOptions。
4. 修改 IPooledActor，加入 OnEnable / OnDisable。
5. 批量修复所有 IPooledActor 实现。
```

### Phase 2：Registry options 缓存

```text
1. ProjectedActorTypeRegistry 新增 _optionsById。
2. ProjectedActorTypeRegistry 新增 _optionsInitializedById。
3. 旧 RegisterGenerated 缓存 options。
4. 新增带 options 的 RegisterGenerated overload。
5. 新增 CreateOptionsFromAttribute。
6. 新增 GetOptions。
7. EnsureCapacity 同步扩容 options 数组。
```

### Phase 3：源生成器与 RegisterAll

```text
1. 新增或扩展 ProjectedActorRegistryGenerator。
2. 生成 GeneratedProjectedActorTypes.g.cs。
3. 生成 RegisterAll。
4. RegisterAll 调用带 options 的 RegisterGenerated overload。
5. 空项目生成空 RegisterAll。
```

### Phase 4：Build 接入

```text
1. 检查 LayerChain.Prebuild 是否创建 Entity。
2. 在 LayersBuilder.Build 接入 GeneratedProjectedActorTypes.RegisterAll。
3. 保证 RegisterAll 早于 _layerChain.Build。
4. 不在 LayerHub.CreateLayers / LayerRuntime 构造函数 / InitializeEcsWorld 接入。
```

### Phase 5：EntityCreateBuilder 迁移

```text
1. 第一阶段保留 RegisterGenerated 兼容调用，并加 TODO。
2. RegisterAll 接入稳定后，移除 EntityCreateBuilder 中 RegisterGenerated。
3. WithProjectedActor 最终只读取 ActorType<TActor>.Id 和 GetOptions。
```

### Phase 6：Projection 运行时

```text
1. 扩展 ProjectedActorRef。
2. 扩展 ProjectedActorMeta。
3. MarkProjected 内部工具接收 ProjectedActorOptions。
4. 新增 RefreshProjectedActorInterest。
5. ProjectionExecutor 模板改用 RefreshProjectedActorInterest。
```

### Phase 7：Disable / Sweep / Batch / Dirty

```text
1. ActorWorld 增加 DisableProjectedActor / EnableProjectedActorIfDisabled。
2. 复用 SetEnable / IsEnable。
3. ActiveProjectedActorList Sweep 预算化。
4. ProjectionBatchBuffer 支持 initialCapacity。
5. 新增 DirtyProjectionSet。
```

### Phase 8：测试与 Benchmark

```text
1. RegisterGenerated options 缓存测试。
2. GeneratedProjectedActorTypes.RegisterAll 接入测试。
3. EntityCreateBuilder 迁移测试。
4. Disable / Enable 测试。
5. ReturnToPool 测试。
6. Touch 节流测试。
7. Predicate 过滤测试。
8. Sweep 预算测试。
9. BatchBuffer 容量预测测试。
10. DirtyProjectionSet 测试。
```

---

## 25. 测试要求

### 25.1 RegisterGenerated options 缓存测试

```text
1. 定义带 [ActorOptions] 的 IPooledActor。
2. 调用旧 RegisterGenerated。
3. GetOptions 返回特性配置。
4. 第二次 RegisterGenerated 不重复解析特性。
```

### 25.2 源生成器注册测试

```text
1. 生成 GeneratedProjectedActorTypes.RegisterAll。
2. RegisterAll 调用带 options 的 RegisterGenerated overload。
3. 生成代码不反射。
4. 生成代码不使用 Dictionary。
5. 没有 IPooledActor 时仍生成空 RegisterAll。
```

### 25.3 Build 接入测试

```text
1. 调用 LayerHub.CreateLayers().Push(...).Build()。
2. 断言 GeneratedProjectedActorTypes.RegisterAll 被调用。
3. 创建带 WithProjectedActor<TActor> 的 Entity。
4. 断言 ProjectedActorTypeRegistry.GetOptions(actorTypeId) 返回 [ActorOptions] 配置。
```

### 25.4 不 Build 不注册测试

```text
1. 调用 LayerHub.CreateLayers()。
2. 不调用 Build。
3. 断言 RegisterAll 不作为 CreateLayers 副作用执行。
```

### 25.5 Disable 策略测试

```text
1. RetirePolicy.Disable 到期后调用 OnDisable。
2. 不调用 OnReturn。
3. ActorId 保持有效。
4. 再次 Touch 调用 OnEnable。
5. 不调用 OnRent。
```

### 25.6 ReturnToPool 策略测试

```text
1. ReturnToPool 到期后调用 OnReturn。
2. ActorId 被清理。
3. 再次 Touch 调用 OnRent。
```

### 25.7 Touch 节流测试

```text
1. TouchInterval 内重复 Touch 不刷新 deadline。
2. 超过 TouchInterval 后刷新。
3. Disabled 状态 Touch 必须 Enable，不能被节流跳过。
```

### 25.8 Predicate 过滤测试

```text
1. Where 返回 false。
2. TouchProjectedActor 后不创建 Actor。
3. 已有 Actor 不刷新 deadline。
```

### 25.9 Sweep 预算测试

```text
1. 创建 2000 个 ProjectedActor。
2. 单帧预算 512。
3. 单帧 Sweep 处理数量不超过 512。
4. 多帧后全部到期 Actor 被处理。
```

### 25.10 BatchBuffer 容量预测测试

```text
1. DirtyCount = 1000。
2. Rent(initialCapacity: 1000)。
3. Post 1000 个事件。
4. GrowCount == 0。
```

---

## 26. 验收标准

```text
1. RegisterGenerated 仍是 ProjectedActorTypeRegistry 的注册入口。
2. 旧 RegisterGenerated 可冷路径反射读取 ActorOptions 并缓存。
3. 新 RegisterGenerated overload 可直接接收 ProjectedActorOptions，完全无反射。
4. GetOptions 只做数组读取。
5. GeneratedProjectedActorTypes.RegisterAll 在 LayersBuilder.Build 阶段调用。
6. RegisterAll 早于 _layerChain.Build。
7. RegisterAll 不放在 EntityCreateBuilder。
8. RegisterAll 不放在 LayerRuntime 构造函数。
9. RegisterAll 不放在 InitializeEcsWorld。
10. RegisterAll 不放在 LayerHub.CreateLayers。
11. EntityCreateBuilder 最终不再负责 RegisterGenerated。
12. TouchProjectedActor 是唯一兴趣刷新 API。
13. predicate 过滤不会 Touch。
14. ProjectResult.Fail 不会 Touch。
15. ProjectResult.Touch 会 Touch 但不 Post。
16. ProjectResult.Post 会 Touch 并 Post。
17. IPooledActor 内化 OnEnable / OnDisable。
18. 不存在 IEnable / IDisable / IActorEnableDisable。
19. Disable 不触发 OnReturn。
20. Enable 不触发 OnRent。
21. Disable 保留 ActorId 与 Entity 绑定。
22. ReturnToPool 保持原回池语义。
23. Touch 节流生效。
24. Touch / Post / Sweep / Ensure 热路径无反射。
25. 运行时热路径不使用 Dictionary。
26. DirtyProjectionSet 不使用 HashSet / Dictionary。
27. ProjectionBatchBuffer 支持 initialCapacity。
28. SweepProjectedActors 支持预算化。
29. 所有没有明确标注为重构的修改都延续 LayerBase 既有设计。
```

---

## 27. 最终结果

最终结构应为：

```text
Actor 类：
  [ActorOptions(...)]
  sealed class MonsterActor : IActor, IPooledActor

源生成器：
  读取 [ActorOptions]
  生成 GeneratedProjectedActorTypes.RegisterAll()

Build 阶段：
  LayersBuilder.Build()
  → GeneratedProjectedActorTypes.RegisterAll()
  → ProjectedActorTypeRegistry.RegisterGenerated(..., options)

Entity 创建阶段：
  WithProjectedActor<TActor>()
  → ActorType<TActor>.Id
  → ProjectedActorTypeRegistry.GetOptions(actorTypeId)
  → 写入 ProjectedActorRef / ProjectedActorMeta

运行时兴趣刷新：
  TouchProjectedActor()
  → RefreshProjectedActorInterest()
  → Active 刷新 deadline
  → Disabled 触发 OnEnable
  → 无 Actor 时 Lazy Ensure

失去兴趣：
  Sweep 到期
  → RetirePolicy.Disable: OnDisable，保留 ActorId
  → RetirePolicy.ReturnToPool: OnReturn，清理 ActorId
```

核心原则：

```text
构建期生成注册代码。
Build 阶段一次性注册。
EntityCreateBuilder 不承担类型注册。
热路径只读缓存。
Touch 表达兴趣命中。
Disable 表达轻量挂起。
ReturnToPool 表达重退场。
```
