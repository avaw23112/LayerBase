# LayerBase：IService 内 `[Mount] ILayerContext` 自动注册改造指导文档

## 0. 背景

当前 LayerBase 中，`[Mount]` 已经支持在 `Layer` 中挂载 `IService`：

```csharp
public partial class GameLayer : Layer
{
    [Mount] private CombatService _combatService;
}
```

并且当前 DI 运行时已经支持字段 / 属性注入：

```csharp
public partial class SomeManager : ILayerContext
{
    [Mount] private SomeDependency _dependency;
}
```

`ServiceProvider.InjectMembers` 会扫描实例字段和属性，只要标记了 `[Mount]`，就会从 DI 容器中解析依赖并写入字段或属性。

当前缺少的是：

```text
在 IService 实现类中声明 [Mount] ILayerContext 字段时，
自动把该 ILayerContext 注册到当前 Service 所属 Layer scope。
```

目标写法：

```csharp
public partial class CombatService : IService
{
    [Mount] private DamageManager _damageManager;
    [Mount] private MoveManager _moveManager;

    public void ConfigureServices(IServiceCollection services)
    {
        // 用户不再需要手动写：
        // services.AddScoped<DamageManager, DamageManager>();
        // services.AddScoped<MoveManager, MoveManager>();
    }
}
```

期望等价于：

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddScoped<DamageManager, DamageManager>();
    services.AddScoped<MoveManager, MoveManager>();
}
```

更准确地说，建议生成器生成 `TryAddScoped<T, T>()`，避免重复注册。

---

## 1. 改造目标

本次改造只做一件事：

```text
让 IService 实现类中的 [Mount] ILayerContext 字段 / 属性，
自动注册为当前 Layer scope 中的 scoped service。
```

例如：

```csharp
public partial class CombatService : IService
{
    [Mount] private DamageManager _damageManager;
}
```

自动生成等价逻辑：

```csharp
services.TryAddScoped<DamageManager, DamageManager>();
```

然后当前已有的 `ServiceProvider.InjectMembers` 继续负责把实例注入回 `_damageManager` 字段。

---

## 2. 不做的事情

本任务不做以下内容：

```text
- 不改变 Layer 上 [Mount] 挂载 IService 的现有语义。
- 不改变 [Mount] 构造函数选择语义。
- 不自动注册 interface / abstract 类型字段。
- 不自动注册普通非 ILayerContext 依赖。
- 不引入运行时全量反射扫描。
- 不修改热路径 Send/Post/Call。
- 不改变 IService.ConfigureServices 的用户手写能力。
```

---

## 3. 当前代码事实

### 3.1 Layer 侧 Service 注册流程

当前 `Layer.AddActiveService` 大致流程是：

```csharp
private void AddActiveService(RegisteredService registration)
{
    m_activeServices.Add(registration);
    ServiceLayerBinder.Attach(registration.Service, this);

    using var _ = m_serviceCollection.PushRegistrationScope(registration.ScopeId);
    registration.Service.ConfigureServices(m_serviceCollection);
}
```

关键点：

```text
1. 每个 IService 都有自己的 registration.ScopeId。
2. ConfigureServices 执行时，ServiceCollection 已经进入该 service 的注册作用域。
3. AddScoped 注册出来的 ILayerContext 会带上当前 RegistrationScopeId。
4. 后续 ServiceProvider.ResolveOrderedServices 会解析这些注册项。
```

所以自动注册 `ILayerContext` 的正确位置是：

```text
AddActiveService 中，位于 PushRegistrationScope 之后。
```

---

### 3.2 ServiceCollection 当前能力

当前 `ServiceCollection` 支持：

```csharp
services.AddScoped<TService, TImpl>();
services.AddScoped<TService>(Func<IServiceProvider, TService> factory);
```

其中 `AddScoped` 实际会创建 `ServiceLifetime.Scoped` 的 `ServiceDescriptor`。

本次建议补充：

```csharp
services.TryAddScoped<TService, TImpl>();
```

用于自动生成代码，避免重复注册。

---

### 3.3 ServiceProvider 已经支持 `[Mount]` 注入

当前 `ServiceProvider.InjectMembers` 会扫描字段和属性：

```text
字段有 [Mount]：
  从 DI 容器解析字段类型并写入字段。

属性有 [Mount] 且可写：
  从 DI 容器解析属性类型并写入属性。
```

所以本任务不需要重新实现注入逻辑。

自动注册后，已有注入流程即可生效。

---

## 4. 推荐方案

### 4.1 新增 service 级自动挂载接口

新增文件建议：

```text
LayerBase/DI/IAutoServiceMount.cs
```

内容：

```csharp
namespace LayerBase.DI;

/// <summary>
/// 由源生成器实现的 Service 级自动挂载接口。
///
/// 作用：
/// 当 IService 实现类中存在 [Mount] ILayerContext 字段 / 属性时，
/// 生成器会生成该接口实现，并在其中把这些 ILayerContext 自动注册进当前 Layer scope。
/// </summary>
public interface IAutoServiceMount
{
    /// <summary>
    /// 自动注册当前 IService 内通过 [Mount] 声明的 ILayerContext 依赖。
    /// </summary>
    /// <param name="services">
    /// 当前 Layer 的 IServiceCollection。
    /// 调用时已经处于当前 IService 的 registration scope 中。
    /// </param>
    void __AutoMountContexts(IServiceCollection services);
}
```

命名可以按项目现有风格调整，例如：

```text
IAutoServiceMount
IAutoContextMount
IAutoServiceContextMount
```

推荐使用 `IAutoServiceMount`，和当前 `IAutoLayerMount` 对应。

---

### 4.2 修改 Layer.AddActiveService

在 `Layer.AddActiveService` 中，用户 `ConfigureServices` 之前调用自动挂载。

推荐顺序：

```csharp
private void AddActiveService(RegisteredService registration)
{
    m_activeServices.Add(registration);
    ServiceLayerBinder.Attach(registration.Service, this);

    using var _ = m_serviceCollection.PushRegistrationScope(registration.ScopeId);

    if (registration.Service is IAutoServiceMount autoMount)
    {
        autoMount.__AutoMountContexts(m_serviceCollection);
    }

    registration.Service.ConfigureServices(m_serviceCollection);
}
```

#### 为什么放在 ConfigureServices 之前？

因为自动挂载是默认注册，用户手写 `ConfigureServices` 可以继续补充复杂依赖。

如果实现了 `TryAddScoped`，则顺序更安全：

```text
AutoMountContexts:
  TryAddScoped<DamageManager, DamageManager>()

ConfigureServices:
  用户可以手写更多注册。
```

如果当前 DI 容器没有覆盖语义，必须避免重复 descriptor 导致重复生命周期调用。

---

### 4.3 新增 TryAddScoped

修改：

```text
LayerBase/DI/IServiceCollection.cs
LayerBase/DI/ServiceCollection.cs
```

新增接口：

```csharp
IServiceCollection TryAddScoped<TService, TImpl>()
    where TImpl : TService;
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

如果希望只在同一 registration scope 内去重，可以改为：

```csharp
if (_descriptors[i].ServiceType == serviceType &&
    _descriptors[i].RegistrationScopeId == _currentRegistrationScopeId)
{
    return this;
}
```

推荐第一版采用 **同一 ServiceType 全局去重**，更简单，也能避免 `ResolveOrderedServices` 重复解析同一服务类型。

如果项目需要同一类型在不同 service scope 中分别注册，则改成 scope 内去重。

---

## 5. 源生成器改造

### 5.1 扫描对象

生成器需要扫描所有 `IService` 实现类。

目标类必须满足：

```text
- 实现 LayerBase.DI.IService。
- 是 partial class。
- 其中存在 [Mount] 字段或属性。
```

字段 / 属性筛选条件：

```text
- 标记 [Mount]。
- 类型实现 ILayerContext。
- 类型是具体 class。
- 类型不是 interface。
- 类型不是 abstract。
- 类型不是 open generic。
```

满足条件才自动注册。

---

### 5.2 不支持的类型

以下情况不要自动注册：

```csharp
[Mount] private IDamageManager _damageManager;
```

原因：

```text
字段类型是接口，生成器不知道应该注册哪个实现类型。
```

以下情况也不要自动注册：

```csharp
[Mount] private AbstractManager _manager;
```

原因：

```text
字段类型是抽象类型，不能直接 new，也不能 AddScoped<T, T>()。
```

如果需要支持这种写法，未来可以扩展：

```csharp
[Mount(typeof(DamageManager))]
private IDamageManager _damageManager;
```

但本任务不做。

---

### 5.3 生成代码形态

用户代码：

```csharp
public partial class CombatService : IService
{
    [Mount] private DamageManager _damageManager;
    [Mount] private MoveManager MoveManager { get; set; }
}
```

生成代码：

```csharp
// <auto-generated/>

using LayerBase.DI;

namespace UserNamespace;

public partial class CombatService : IAutoServiceMount
{
    public void __AutoMountContexts(IServiceCollection services)
    {
        services.TryAddScoped<global::UserNamespace.DamageManager, global::UserNamespace.DamageManager>();
        services.TryAddScoped<global::UserNamespace.MoveManager, global::UserNamespace.MoveManager>();
    }
}
```

注意事项：

```text
1. 类型名必须使用 fully qualified name。
2. 同一个类型出现多次，只生成一次 TryAddScoped。
3. 如果用户类已经实现 IAutoServiceMount，需要避免重复生成冲突。
4. 如果类不是 partial，应报告诊断，不生成代码。
```

---

### 5.4 生成器去重规则

同一个 service 内：

```csharp
[Mount] private DamageManager _a;
[Mount] private DamageManager _b;
```

只生成一次：

```csharp
services.TryAddScoped<DamageManager, DamageManager>();
```

建议生成器内部使用：

```text
HashSet<ITypeSymbol>(SymbolEqualityComparer.Default)
```

---

### 5.5 partial 检查

如果用户写：

```csharp
public class CombatService : IService
{
    [Mount] private DamageManager _damageManager;
}
```

但没有 `partial`，生成器无法生成同名 partial 类型。

建议报告诊断：

```text
LBMOUNT001:
IService type 'CombatService' contains [Mount] ILayerContext members and must be declared partial.
```

Severity 建议：

```text
Warning 或 Error
```

推荐先用 Warning，避免破坏现有用户代码。

---

## 6. 语义说明

### 6.1 Layer 上的 `[Mount]`

保持现有语义：

```csharp
public partial class GameLayer : Layer
{
    [Mount] private CombatService _combatService;
}
```

含义：

```text
把 IService 挂载到当前 Layer。
```

---

### 6.2 IService 上的 `[Mount] ILayerContext`

新增语义：

```csharp
public partial class CombatService : IService
{
    [Mount] private DamageManager _damageManager;
}
```

含义：

```text
1. 自动注册 DamageManager 为当前 Layer scope 的 scoped service。
2. ServiceProvider 创建 CombatService 或相关对象时，会把 DamageManager 注入到字段。
```

等价于：

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.TryAddScoped<DamageManager, DamageManager>();
}
```

---

### 6.3 Constructor 上的 `[Mount]`

保持当前语义：

```csharp
public class SomeManager
{
    [Mount]
    private SomeManager(SomeDependency dep)
    {
    }
}
```

含义：

```text
选择该构造函数作为 DI 构造器。
```

不要和字段挂载语义混淆。

---

## 7. 生命周期预期

自动注册的 `ILayerContext` 应走完整现有生命周期：

```text
IService 挂载到 Layer
  -> AddActiveService
  -> PushRegistrationScope(service scope)
  -> __AutoMountContexts(services)
  -> service.ConfigureServices(services)
  -> new ServiceProvider(...)
  -> ResolveOrderedServices(...)
  -> BuildAutoBinding
      -> IAutoSubscribe.AutoBind
  -> LifecycleBuild
      -> IInitializable.Initialize
      -> IUpdate 收集
      -> IFixedUpdate 收集
      -> IPostBuild 收集
      -> IRuntimeStart 收集
      -> IRuntimeStop 收集
```

也就是说，被 `[Mount]` 自动注册的 `DamageManager` 应该和用户手写 `services.AddScoped<DamageManager, DamageManager>()` 完全一致。

---

## 8. 测试要求

测试使用 NUnit 风格。

建议新增测试文件：

```text
EventsTest/ServiceMountContextTests.cs
```

---

### 8.1 IService 中 Mount ILayerContext 应自动注册并注入

测试目标：

```text
[Mount] private DamageManager _damageManager;
```

应自动：

```text
1. 注册 DamageManager。
2. 创建 DamageManager。
3. 注入 CombatService._damageManager。
```

测试草案：

```csharp
using LayerBase;
using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.Layers;
using NUnit.Framework;

namespace EventsTest;

[TestFixture]
public partial class ServiceMountContextTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Service_Mount_ILayerContext_Should_Register_And_Inject_Manager()
    {
        var layer = new TestLayer();

        LayerHub.CreateLayers()
            .Push(layer)
            .Build();

        Assert.That(layer.Service, Is.Not.Null);
        Assert.That(layer.Service!.MountedManager, Is.Not.Null);
        Assert.That(layer.Service.MountedManager!.LayerIndex, Is.EqualTo(layer.RouteIndex));
    }

    private partial class TestLayer : Layer
    {
        [Mount] private TestService _service = null!;

        public TestService? Service => _service;
    }

    private partial class TestService : IService
    {
        [Mount] private TestManager _manager = null!;

        public TestManager? MountedManager => _manager;

        public void ConfigureServices(IServiceCollection services)
        {
        }
    }

    private partial class TestManager : ILayerContext
    {
        public int LayerIndex { get; set; }
    }
}
```

---

### 8.2 自动注册的 ILayerContext 应参与生命周期

测试目标：

```text
自动注册的 manager 实现 IInitializable / IUpdate，
应该被 Initialize 和 Update。
```

测试草案：

```csharp
[Test]
public void AutoMounted_ILayerContext_Should_Run_Lifecycle()
{
    var trace = new List<string>();

    var layer = new LifecycleLayer(trace);

    LayerHub.CreateLayers()
        .Push(layer)
        .Build();

    Assert.That(trace, Does.Contain("Init_Manager"));

    LayerHub.Pump(0.016f);

    Assert.That(trace, Does.Contain("Update_Manager"));
}

private partial class LifecycleLayer : Layer
{
    [Mount] private LifecycleService _service = null!;

    public LifecycleLayer(List<string> trace)
    {
        Trace = trace;
    }

    public List<string> Trace { get; }
}

private partial class LifecycleService : IService
{
    [Mount] private LifecycleManager _manager = null!;

    public void ConfigureServices(IServiceCollection services)
    {
    }
}

private partial class LifecycleManager : ILayerContext, IInitializable, IUpdate
{
    private readonly LifecycleLayer _layer;

    public LifecycleManager(LifecycleLayer layer)
    {
        _layer = layer;
    }

    public int LayerIndex { get; set; }

    public void Initialize()
    {
        _layer.Trace.Add("Init_Manager");
    }

    public void Update()
    {
        _layer.Trace.Add("Update_Manager");
    }
}
```

如果当前 DI 不能注入 `LifecycleLayer`，则改成用静态 trace 或手动传入 trace 的服务方式。

---

### 8.3 重复 Mount 同一类型不应重复生命周期

测试目标：

```text
同一个 IService 中两个 [Mount] 字段指向同一个 Manager 类型，
应该只注册一次。
```

测试草案：

```csharp
[Test]
public void Duplicate_Mounted_Manager_Type_Should_Register_Only_Once()
{
    var trace = new List<string>();

    var layer = new DuplicateMountLayer(trace);

    LayerHub.CreateLayers()
        .Push(layer)
        .Build();

    var initCount = trace.Count(x => x == "Init_DuplicateManager");

    Assert.That(initCount, Is.EqualTo(1));
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
    [Mount] private DuplicateManager _a = null!;
    [Mount] private DuplicateManager _b = null!;

    public void ConfigureServices(IServiceCollection services)
    {
    }
}

private partial class DuplicateManager : ILayerContext, IInitializable
{
    private readonly DuplicateMountLayer _layer;

    public DuplicateManager(DuplicateMountLayer layer)
    {
        _layer = layer;
    }

    public int LayerIndex { get; set; }

    public void Initialize()
    {
        _layer.Trace.Add("Init_DuplicateManager");
    }
}
```

如果当前 DI 不能注入 Layer，则用静态计数器或通过 service 提供 trace。

---

### 8.4 interface / abstract Mount 不应自动注册

测试目标：

```text
[Mount] private ITestManager _manager;
```

不应自动注册，因为没有实现类型。

建议测试生成器诊断，或者运行时 Build 失败。

如果项目当前没有 generator diagnostic 测试框架，可以暂不写这条运行时测试。

---

## 9. 文档更新

README 或 docs 中补充 `[Mount]` 的三种语义：

```markdown
## Mount Attribute

`[Mount]` 在不同位置有不同语义：

### Layer 字段 / 属性

```csharp
public partial class GameLayer : Layer
{
    [Mount] private CombatService _combatService;
}
```

表示把 `IService` 挂载到当前 Layer。

### IService 字段 / 属性

```csharp
public partial class CombatService : IService
{
    [Mount] private DamageManager _damageManager;
}
```

如果字段类型是具体的 `ILayerContext` 实现，则生成器会自动注册：

```csharp
services.TryAddScoped<DamageManager, DamageManager>();
```

随后 DI 会把实例注入到字段。

### 构造函数

```csharp
public class DamageManager
{
    [Mount]
    private DamageManager(SomeDependency dep)
    {
    }
}
```

表示选择该构造函数作为 DI 构造器。
```

---

## 10. 高危点与规避

### 10.1 重复注册

风险：

```text
重复生成 AddScoped 导致同一个 manager 被 ResolveOrderedServices 多次收集，
从而 AutoBind / Initialize / Update 重复执行。
```

规避：

```text
生成器按类型去重。
自动注册使用 TryAddScoped。
```

---

### 10.2 interface / abstract 类型

风险：

```text
字段是接口或抽象类型，生成器无法知道实现类。
```

规避：

```text
第一版不自动注册。
用户手写 ConfigureServices。
未来可支持 [Mount(typeof(ImplType))]。
```

---

### 10.3 与用户 ConfigureServices 冲突

风险：

```text
生成器自动注册，用户又手写注册同类型。
```

规避：

```text
自动注册使用 TryAddScoped。
用户需要替换实现时，后续可设计 Replace 或显式 Add 优先级。
```

---

### 10.4 运行时反射扫描

风险：

```text
Build 阶段重复反射扫描所有 service，增加复杂度。
```

规避：

```text
使用源生成器生成 __AutoMountContexts，不在运行时扫描。
```

---

## 11. 推荐提交拆分

### Commit 1：DI TryAddScoped

```text
add TryAddScoped to service collection
```

内容：

```text
- IServiceCollection.TryAddScoped<TService, TImpl>()
- ServiceCollection.TryAddScoped<TService, TImpl>()
- 基础单元测试
```

---

### Commit 2：IAutoServiceMount 接口与 Layer 接入

```text
add auto service mount hook
```

内容：

```text
- 新增 IAutoServiceMount
- Layer.AddActiveService 调用 __AutoMountContexts
- 不改现有 IService.ConfigureServices 语义
```

---

### Commit 3：Generator 支持 IService 内 [Mount] ILayerContext

```text
generate service mounted contexts
```

内容：

```text
- 扫描 IService partial class
- 找 [Mount] 字段 / 属性
- 筛选具体 ILayerContext 类型
- 生成 IAutoServiceMount 实现
- 同类型去重
- 非 partial 诊断
```

---

### Commit 4：NUnit 回归测试

```text
add service mount context tests
```

内容：

```text
- Service_Mount_ILayerContext_Should_Register_And_Inject_Manager
- AutoMounted_ILayerContext_Should_Run_Lifecycle
- Duplicate_Mounted_Manager_Type_Should_Register_Only_Once
```

---

### Commit 5：文档更新

```text
document service-level Mount semantics
```

内容：

```text
- README 或 docs 补充 Mount Attribute 三种语义
- 给出 IService 内 Mount ILayerContext 示例
```

---

## 12. 验收标准

完成后应满足：

```text
1. Layer 上 [Mount] IService 旧功能不受影响。
2. IService 内 [Mount] 具体 ILayerContext 字段可自动注册并注入。
3. 自动注册的 ILayerContext 会参与 AutoBind / Initialize / Update / FixedUpdate / PostBuild / RuntimeStart / RuntimeStop。
4. 同一 IService 内重复挂载同一 Manager 类型不会重复注册。
5. interface / abstract 类型不会被错误自动注册。
6. 用户仍可在 ConfigureServices 中手动注册复杂依赖。
7. README/docs 明确说明 [Mount] 在 Layer、IService、constructor 上的不同语义。
```
