# LayerBase：`[Mount(typeof(TImpl))]` 支持接口 / 抽象类型自动挂载设计文档

## 0. 背景

当前计划已经支持：

```csharp
public partial class CombatService : IService
{
    [Mount] private DamageManager _damageManager;
}
```

当字段类型 `DamageManager` 是具体 `ILayerContext` 实现类时，源生成器可以自动生成：

```csharp
services.TryAddScoped<DamageManager, DamageManager>();
```

但以下写法暂时无法自动注册：

```csharp
public partial class CombatService : IService
{
    [Mount] private IDamageManager _damageManager;
}
```

原因是：

```text
字段类型是接口，生成器不知道应该注册哪个实现类。
```

因此新增设计：

```csharp
public partial class CombatService : IService
{
    [Mount(typeof(DamageManager))]
    private IDamageManager _damageManager;
}
```

含义：

```text
字段 / 属性类型：
  IDamageManager

实际注册实现：
  DamageManager

生成注册：
  services.TryAddScoped<IDamageManager, DamageManager>();
```

---

## 1. 目标

本次改造目标：

```text
1. 支持 IService 内 [Mount(typeof(TImpl))] 字段 / 属性。
2. 字段 / 属性类型可以是 interface 或 abstract。
3. TImpl 必须是具体 class。
4. TImpl 必须能赋值给字段 / 属性类型。
5. TImpl 必须实现 ILayerContext。
6. 自动生成 TryAddScoped<TService, TImpl>()。
7. 继续复用现有 ServiceProvider.InjectMembers 完成字段注入。
```

示例：

```csharp
public interface IDamageManager
{
    void ApplyDamage(int targetId, int amount);
}

public sealed class DamageManager : IDamageManager, ILayerContext
{
    public int LayerIndex { get; set; }

    public void ApplyDamage(int targetId, int amount)
    {
    }
}

public partial class CombatService : IService
{
    [Mount(typeof(DamageManager))]
    private IDamageManager _damageManager;
}
```

生成结果：

```csharp
public partial class CombatService : IAutoServiceMount
{
    public void __AutoMountContexts(IServiceCollection services)
    {
        services.TryAddScoped<global::UserNamespace.IDamageManager, global::UserNamespace.DamageManager>();
    }
}
```

---

## 2. 不做的事情

本次不做：

```text
- 不支持多个实现自动选择。
- 不做运行时扫描所有实现类。
- 不根据命名约定推断实现类。
- 不允许 TImpl 不是 ILayerContext。
- 不改变 Layer 上 [Mount] IService 的现有语义。
- 不改变构造函数 [Mount] 的选择构造器语义。
- 不修改 Send/Post/Call 热路径。
```

---

## 3. 新增 MountAttribute 构造函数

### 3.1 当前问题

当前 `[Mount]` 只表示“需要挂载 / 注入”，但不能指定实现类型。

新增构造函数：

```csharp
[Mount(typeof(DamageManager))]
private IDamageManager _damageManager;
```

需要让 `MountAttribute` 保存实现类型。

---

### 3.2 推荐 API

修改或新增文件：

```text
LayerBase/DI/Options/MountAttribute.cs
```

建议实现：

```csharp
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
```

---

## 4. 概念说明

### 4.1 interface 是什么

`interface` 是 C# 中的接口类型。  
它只描述“对象应该提供哪些成员”，不描述“对象怎么创建”。

例如：

```csharp
public interface IDamageManager
{
    void ApplyDamage(int targetId, int amount);
}
```

它不能直接 `new IDamageManager()`。

所以当字段是接口时：

```csharp
[Mount] private IDamageManager _damageManager;
```

框架无法知道应该使用哪个实现类。

---

### 4.2 abstract 是什么

`abstract` 是抽象类型。  
抽象类型可以包含部分实现，但它仍然不能直接创建实例。

例如：

```csharp
public abstract class DamageManagerBase
{
    public abstract void ApplyDamage(int targetId, int amount);
}
```

所以当字段是抽象类型时，也需要用户显式指定实现类：

```csharp
[Mount(typeof(DamageManager))]
private DamageManagerBase _damageManager;
```

---

### 4.3 assignable 是什么

`assignable` 表示“一个类型的实例能不能赋值给另一个类型”。

例如：

```csharp
IDamageManager manager = new DamageManager();
```

如果 `DamageManager : IDamageManager`，则 `DamageManager` 可以赋值给 `IDamageManager`。

生成器必须检查：

```text
implementationType 是否能赋值给 fieldType。
```

也就是：

```text
DamageManager 是否实现 IDamageManager。
```

---

## 5. 生成器规则

### 5.1 扫描范围

源生成器扫描：

```text
所有实现 IService 的 partial class。
```

然后寻找其中：

```text
带 [Mount] 的字段 / 属性。
```

---

### 5.2 字段 / 属性类型解析

对于每个 `[Mount]` 字段 / 属性：

```csharp
[Mount(typeof(DamageManager))]
private IDamageManager _damageManager;
```

生成器需要得到：

```text
serviceType:
  字段 / 属性类型。
  这里是 IDamageManager。

implementationType:
  MountAttribute 中指定的 typeof(DamageManager)。
```

如果没有指定：

```csharp
[Mount]
private DamageManager _damageManager;
```

则：

```text
serviceType = DamageManager
implementationType = DamageManager
```

---

### 5.3 自动注册条件

只有满足以下条件才自动注册：

```text
1. 当前类实现 IService。
2. 当前类是 partial class。
3. 成员是字段或属性。
4. 成员标记了 [Mount]。
5. implementationType 是具体 class。
6. implementationType 不是 abstract。
7. implementationType 不是 interface。
8. implementationType 不是 open generic。
9. implementationType 实现 ILayerContext。
10. implementationType 可以赋值给 serviceType。
```

如果满足，生成：

```csharp
services.TryAddScoped<TService, TImpl>();
```

---

### 5.4 诊断规则

#### LBMOUNT001：Service 类型必须 partial

场景：

```csharp
public class CombatService : IService
{
    [Mount(typeof(DamageManager))]
    private IDamageManager _damageManager;
}
```

诊断：

```text
LBMOUNT001:
IService type 'CombatService' contains [Mount] ILayerContext members and must be declared partial.
```

建议 severity：

```text
Warning 或 Error。
```

推荐第一版用 Warning，降低破坏性。

---

#### LBMOUNT002：实现类型不是具体类型

场景：

```csharp
[Mount(typeof(IDamageManager))]
private IDamageManager _damageManager;
```

诊断：

```text
LBMOUNT002:
Mount implementation type 'IDamageManager' must be a concrete class.
```

---

#### LBMOUNT003：实现类型不能赋值给字段类型

场景：

```csharp
[Mount(typeof(MoveManager))]
private IDamageManager _damageManager;
```

但 `MoveManager` 没有实现 `IDamageManager`。

诊断：

```text
LBMOUNT003:
Mount implementation type 'MoveManager' is not assignable to field type 'IDamageManager'.
```

---

#### LBMOUNT004：实现类型必须实现 ILayerContext

场景：

```csharp
[Mount(typeof(DamageRepository))]
private IDamageRepository _repo;
```

但 `DamageRepository` 不是 `ILayerContext`。

诊断：

```text
LBMOUNT004:
Mount implementation type 'DamageRepository' must implement ILayerContext to be auto-registered from IService.
```

普通依赖仍然可以通过 `ConfigureServices` 手动注册，不由这个生成器自动注册。

---

#### LBMOUNT005：未指定实现类型但字段类型不可实例化

场景：

```csharp
[Mount]
private IDamageManager _damageManager;
```

诊断：

```text
LBMOUNT005:
Mount field type 'IDamageManager' is interface or abstract. Use [Mount(typeof(ImplementationType))] or register it manually in ConfigureServices.
```

---

## 6. 生成代码形态

### 6.1 简单具体类型

用户代码：

```csharp
public partial class CombatService : IService
{
    [Mount] private DamageManager _damageManager;
}
```

生成：

```csharp
public partial class CombatService : IAutoServiceMount
{
    public void __AutoMountContexts(IServiceCollection services)
    {
        services.TryAddScoped<
            global::UserNamespace.DamageManager,
            global::UserNamespace.DamageManager>();
    }
}
```

---

### 6.2 接口字段 + 显式实现类型

用户代码：

```csharp
public partial class CombatService : IService
{
    [Mount(typeof(DamageManager))]
    private IDamageManager _damageManager;
}
```

生成：

```csharp
public partial class CombatService : IAutoServiceMount
{
    public void __AutoMountContexts(IServiceCollection services)
    {
        services.TryAddScoped<
            global::UserNamespace.IDamageManager,
            global::UserNamespace.DamageManager>();
    }
}
```

---

### 6.3 抽象字段 + 显式实现类型

用户代码：

```csharp
public partial class CombatService : IService
{
    [Mount(typeof(DamageManager))]
    private DamageManagerBase _damageManager;
}
```

生成：

```csharp
public partial class CombatService : IAutoServiceMount
{
    public void __AutoMountContexts(IServiceCollection services)
    {
        services.TryAddScoped<
            global::UserNamespace.DamageManagerBase,
            global::UserNamespace.DamageManager>();
    }
}
```

---

## 7. 去重规则

### 7.1 同一 service 内重复字段

用户代码：

```csharp
public partial class CombatService : IService
{
    [Mount(typeof(DamageManager))]
    private IDamageManager _a;

    [Mount(typeof(DamageManager))]
    private IDamageManager _b;
}
```

只生成一次：

```csharp
services.TryAddScoped<IDamageManager, DamageManager>();
```

---

### 7.2 同一个实现注册到不同 service type

用户代码：

```csharp
public partial class CombatService : IService
{
    [Mount(typeof(DamageManager))]
    private IDamageManager _a;

    [Mount(typeof(DamageManager))]
    private DamageManagerBase _b;
}
```

可以生成两条：

```csharp
services.TryAddScoped<IDamageManager, DamageManager>();
services.TryAddScoped<DamageManagerBase, DamageManager>();
```

但需要注意：这可能导致同一个实现类型通过两个 service type 注册成两个不同 scoped 实例。

第一版建议允许，因为它符合 DI 容器通常语义。  
如果项目希望一个实现类型只创建一次，需要额外设计 implementation-level 去重或 alias 注册，不建议本次做。

---

## 8. DI 运行时要求

### 8.1 TryAddScoped

需要支持：

```csharp
services.TryAddScoped<TService, TImpl>();
```

接口：

```csharp
public interface IServiceCollection
{
    IServiceCollection TryAddScoped<TService, TImpl>()
        where TImpl : TService;
}
```

实现：

```csharp
public IServiceCollection TryAddScoped<TService, TImpl>()
    where TImpl : TService
{
    var serviceType = typeof(TService);

    for (var i = 0; i < _descriptors.Count; i++)
    {
        if (_descriptors[i].ServiceType == serviceType)
        {
            return this;
        }
    }

    return AddScoped<TService, TImpl>();
}
```

参数说明：

```text
TService:
  对外解析类型。
  例如 IDamageManager。

TImpl:
  实际创建类型。
  例如 DamageManager。
```

---

### 8.2 生成器使用 TryAddScoped

生成器必须使用：

```csharp
services.TryAddScoped<TService, TImpl>();
```

不要用：

```csharp
services.AddScoped<TService, TImpl>();
```

原因：

```text
避免重复 Mount 或用户手写注册导致重复 descriptor。
```

---

### 8.3 ServiceProvider.InjectMembers 不需要修改

原因：

```text
InjectMembers 已经会根据字段 / 属性类型解析依赖。
```

例如：

```csharp
[Mount(typeof(DamageManager))]
private IDamageManager _damageManager;
```

生成器注册：

```csharp
services.TryAddScoped<IDamageManager, DamageManager>();
```

运行时注入：

```text
GetServiceInternal(typeof(IDamageManager))
  -> 创建 DamageManager
  -> 写入 _damageManager
```

---

## 9. 与现有 Mount 语义的关系

### 9.1 Layer 字段 / 属性

```csharp
public partial class GameLayer : Layer
{
    [Mount] private CombatService _combatService;
}
```

含义：

```text
挂载 IService 到 Layer。
```

不变。

---

### 9.2 IService 字段 / 属性

```csharp
public partial class CombatService : IService
{
    [Mount(typeof(DamageManager))]
    private IDamageManager _damageManager;
}
```

含义：

```text
注册并注入 Layer-scoped ILayerContext。
```

新增。

---

### 9.3 构造函数

```csharp
public class DamageManager
{
    [Mount]
    private DamageManager(SomeDependency dep)
    {
    }
}
```

含义：

```text
选择 DI 构造函数。
```

不变。

---

## 10. 示例

### 10.1 接口挂载

```csharp
public interface IDamageManager
{
    void ApplyDamage(int targetId, int amount);
}

public sealed class DamageManager : IDamageManager, ILayerContext
{
    public int LayerIndex { get; set; }

    public void ApplyDamage(int targetId, int amount)
    {
    }
}

public partial class CombatService : IService
{
    [Mount(typeof(DamageManager))]
    private IDamageManager _damageManager = null!;

    public void ConfigureServices(IServiceCollection services)
    {
        // 不需要手写：
        // services.TryAddScoped<IDamageManager, DamageManager>();
    }
}
```

---

### 10.2 抽象基类挂载

```csharp
public abstract class DamageManagerBase : ILayerContext
{
    public int LayerIndex { get; set; }

    public abstract void ApplyDamage(int targetId, int amount);
}

public sealed class DamageManager : DamageManagerBase
{
    public override void ApplyDamage(int targetId, int amount)
    {
    }
}

public partial class CombatService : IService
{
    [Mount(typeof(DamageManager))]
    private DamageManagerBase _damageManager = null!;
}
```

生成：

```csharp
services.TryAddScoped<DamageManagerBase, DamageManager>();
```

---

## 11. NUnit 测试建议

新增测试文件：

```text
EventsTest/ServiceMountImplementationTypeTests.cs
```

---

### 11.1 接口字段应注册并注入实现

```csharp
using LayerBase;
using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.Layers;
using NUnit.Framework;

namespace EventsTest;

[TestFixture]
public partial class ServiceMountImplementationTypeTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Mount_With_ImplementationType_Should_Register_And_Inject_Interface_Field()
    {
        var layer = new TestLayer();

        LayerHub.CreateLayers()
            .Push(layer)
            .Build();

        Assert.That(layer.Service, Is.Not.Null);
        Assert.That(layer.Service!.DamageManager, Is.Not.Null);
        Assert.That(layer.Service.DamageManager, Is.TypeOf<DamageManager>());
    }

    private partial class TestLayer : Layer
    {
        [Mount] private CombatService _service = null!;

        public CombatService? Service => _service;
    }

    private partial class CombatService : IService
    {
        [Mount(typeof(DamageManager))]
        private IDamageManager _damageManager = null!;

        public IDamageManager? DamageManager => _damageManager;

        public void ConfigureServices(IServiceCollection services)
        {
        }
    }

    private interface IDamageManager
    {
        void ApplyDamage(int targetId, int amount);
    }

    private sealed partial class DamageManager : IDamageManager, ILayerContext
    {
        public int LayerIndex { get; set; }

        public void ApplyDamage(int targetId, int amount)
        {
        }
    }
}
```

---

### 11.2 抽象字段应注册并注入实现

```csharp
[Test]
public void Mount_With_ImplementationType_Should_Register_And_Inject_Abstract_Field()
{
    var layer = new AbstractMountLayer();

    LayerHub.CreateLayers()
        .Push(layer)
        .Build();

    Assert.That(layer.Service, Is.Not.Null);
    Assert.That(layer.Service!.Manager, Is.Not.Null);
    Assert.That(layer.Service.Manager, Is.TypeOf<AbstractDamageManagerImpl>());
}

private partial class AbstractMountLayer : Layer
{
    [Mount] private AbstractMountService _service = null!;

    public AbstractMountService? Service => _service;
}

private partial class AbstractMountService : IService
{
    [Mount(typeof(AbstractDamageManagerImpl))]
    private DamageManagerBase _manager = null!;

    public DamageManagerBase? Manager => _manager;

    public void ConfigureServices(IServiceCollection services)
    {
    }
}

private abstract partial class DamageManagerBase : ILayerContext
{
    public int LayerIndex { get; set; }

    public abstract void ApplyDamage(int targetId, int amount);
}

private sealed partial class AbstractDamageManagerImpl : DamageManagerBase
{
    public override void ApplyDamage(int targetId, int amount)
    {
    }
}
```

---

### 11.3 自动注册对象应参与生命周期

```csharp
[Test]
public void Mount_With_ImplementationType_Should_Run_Lifecycle()
{
    var trace = new List<string>();
    var layer = new LifecycleMountLayer(trace);

    LayerHub.CreateLayers()
        .Push(layer)
        .Build();

    Assert.That(trace, Does.Contain("Init_DamageManager"));

    LayerHub.Pump(0.016f);

    Assert.That(trace, Does.Contain("Update_DamageManager"));
}

private partial class LifecycleMountLayer : Layer
{
    [Mount] private LifecycleCombatService _service = null!;

    public LifecycleMountLayer(List<string> trace)
    {
        Trace = trace;
    }

    public List<string> Trace { get; }
}

private partial class LifecycleCombatService : IService
{
    [Mount(typeof(LifecycleDamageManager))]
    private ILifecycleDamageManager _manager = null!;

    public void ConfigureServices(IServiceCollection services)
    {
    }
}

private interface ILifecycleDamageManager
{
}

private sealed partial class LifecycleDamageManager :
    ILifecycleDamageManager,
    ILayerContext,
    IInitializable,
    IUpdate
{
    private readonly LifecycleMountLayer _layer;

    public LifecycleDamageManager(LifecycleMountLayer layer)
    {
        _layer = layer;
    }

    public int LayerIndex { get; set; }

    public void Initialize()
    {
        _layer.Trace.Add("Init_DamageManager");
    }

    public void Update()
    {
        _layer.Trace.Add("Update_DamageManager");
    }
}
```

如果当前 DI 无法注入 `LifecycleMountLayer`，则改为静态 trace 或手动注册 trace。

---

### 11.4 重复 Mount 不应重复生命周期

```csharp
[Test]
public void Duplicate_Mount_With_Same_ServiceType_Should_Register_Once()
{
    var trace = new List<string>();
    var layer = new DuplicateMountLayer(trace);

    LayerHub.CreateLayers()
        .Push(layer)
        .Build();

    var count = trace.Count(x => x == "Init_DuplicateDamageManager");

    Assert.That(count, Is.EqualTo(1));
}

private partial class DuplicateMountLayer : Layer
{
    [Mount] private DuplicateMountService _service = null!;

    public DuplicateMountLayer(List<string> trace)
    {
        Trace = trace;
    }

    public List<string> Trace { get; }
}

private partial class DuplicateMountService : IService
{
    [Mount(typeof(DuplicateDamageManager))]
    private IDuplicateDamageManager _a = null!;

    [Mount(typeof(DuplicateDamageManager))]
    private IDuplicateDamageManager _b = null!;

    public void ConfigureServices(IServiceCollection services)
    {
    }
}

private interface IDuplicateDamageManager
{
}

private sealed partial class DuplicateDamageManager :
    IDuplicateDamageManager,
    ILayerContext,
    IInitializable
{
    private readonly DuplicateMountLayer _layer;

    public DuplicateDamageManager(DuplicateMountLayer layer)
    {
        _layer = layer;
    }

    public int LayerIndex { get; set; }

    public void Initialize()
    {
        _layer.Trace.Add("Init_DuplicateDamageManager");
    }
}
```

---

## 12. 推荐提交顺序

### Commit 1：扩展 MountAttribute

```text
add implementation type to MountAttribute
```

内容：

```text
- MountAttribute 增加 Type? ImplementationType
- 增加 MountAttribute(Type implementationType) 构造函数
```

---

### Commit 2：TryAddScoped

```text
add TryAddScoped to DI service collection
```

内容：

```text
- IServiceCollection.TryAddScoped<TService, TImpl>()
- ServiceCollection.TryAddScoped<TService, TImpl>()
```

---

### Commit 3：Service-level auto mount hook

```text
add auto service mount hook
```

内容：

```text
- 新增 IAutoServiceMount
- Layer.AddActiveService 中调用 __AutoMountContexts
```

---

### Commit 4：Generator 支持 `[Mount(typeof(TImpl))]`

```text
generate service scoped mounts with implementation type
```

内容：

```text
- 扫描 IService 中 [Mount] 字段 / 属性
- 支持 MountAttribute.ImplementationType
- 生成 TryAddScoped<TService, TImpl>()
- 生成诊断 LBMOUNT001~005
```

---

### Commit 5：NUnit 回归测试

```text
add implementation type mount tests
```

内容：

```text
- interface field injection
- abstract field injection
- lifecycle
- duplicate registration
```

---

### Commit 6：文档

```text
document Mount implementation type
```

内容：

```text
- README 或 docs 中说明 [Mount(typeof(TImpl))]
- 说明 Layer / IService / Constructor 三种 Mount 语义
```

---

## 13. 验收标准

完成后应满足：

```text
1. [Mount] private ConcreteManager _manager; 继续可用。
2. [Mount(typeof(DamageManager))] private IDamageManager _manager; 可用。
3. [Mount(typeof(DamageManager))] private DamageManagerBase _manager; 可用。
4. TImpl 不是具体 class 时生成诊断。
5. TImpl 不能赋值给字段类型时生成诊断。
6. TImpl 不实现 ILayerContext 时生成诊断。
7. 自动注册使用 TryAddScoped，避免重复生命周期。
8. 自动注册对象参与 AutoBind / Initialize / Update / FixedUpdate / PostBuild / RuntimeStart / RuntimeStop。
9. Layer 上 [Mount] IService 旧语义不受影响。
10. Constructor 上 [Mount] 旧语义不受影响。
```
