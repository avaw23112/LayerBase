## Task 2：让 ScopeDefinitionRegistry 成为 Scope 配置唯一来源

当前 `GeneratedScopeDefinition` 保存了 ScopeId、Identity、Factory，且 Factory 能创建带 `Options` 的 Scope 定义；但 CompositionPlan 只收集 ScopeType，最后把所有非 Main Scope 强制设成 `ScopeOptions.Inline`。

仓库已有完整的 `ScopeDefinitionRegistry` 冲突检测逻辑，应直接复用，而不是重新实现。

### Files

* Modify: `LayerBase/Scope/RuntimeCompositionPlan.cs`
* Modify: `LayerBase/Scope/ScopeDefinitionRegistry.cs`，仅在确有必要时增加只读查询
* Create: `LayerBase.Test/ScopeDefinitionOptionsTests.cs`

### Required behavior

* Main、Inline、Manual、Worker Scope 都保留定义中声明的 Options。
* 模块声明和 Layer 本地声明进入同一个 Registry。
* ScopeType、ScopeId、Identity 冲突必须在 Build 阶段抛错。
* Service、Context、Call、Event 的 OwnerScope 必须已注册。
* 不再根据 ScopeType 临时猜测或创建未声明 Scope。

### Step 1：写失败测试

至少增加：

```csharp
[TestFixture]
[Category("ProductionHardening")]
public sealed class ScopeDefinitionOptionsTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        EventMetaDataHandler.Clear();
    }

    [Test]
    public void Worker_scope_preserves_declared_options()
    {
        using LayerRuntime runtime = LayerHub.CreateLayers()
            .Push(new WorkerScopeLayer())
            .Build();

        ScopeExecutionPlan plan = runtime.CompositionPlan.Scopes
            .Single(p => p.Descriptor.ScopeType == typeof(TestWorkerScope));

        Assert.That(plan.Options.Threading, Is.EqualTo(ScopeThreadingMode.Worker));
        Assert.That(plan.Options.Clock, Is.EqualTo(ScopeClockMode.FixedRate));
        Assert.That(plan.Options.TickRateHz, Is.EqualTo(30));
        Assert.That(
            plan.Options.FaultPolicy,
            Is.EqualTo(ScopeFaultPolicy.StopScope));
        Assert.That(runtime.ScopeHost.HasWorkerScopes, Is.True);
    }

    [Test]
    public void Conflicting_scope_ids_are_rejected_during_build()
    {
        var conflictingModule = new ConflictingScopeIdsModule();

        Assert.That(
            () => LayerHub.CreateLayers()
                .Push(new WorkerScopeLayer())
                .AddAssemblyModule(conflictingModule)
                .Build(),
            Throws.TypeOf<ScopeDefinitionConflictException>());
    }

    [Test]
    public void Manual_scope_preserves_declared_options()
    {
        using LayerRuntime runtime = LayerHub.CreateLayers()
            .Push(new ManualScopeLayer())
            .Build();

        ScopeExecutionPlan plan = runtime.CompositionPlan.Scopes
            .Single(p => p.Descriptor.ScopeType == typeof(TestManualScope));

        Assert.That(plan.Options.Threading, Is.EqualTo(ScopeThreadingMode.Inline));
        Assert.That(plan.Options.Clock, Is.EqualTo(ScopeClockMode.Manual));
        Assert.That(plan.Options.FaultPolicy, Is.EqualTo(ScopeFaultPolicy.StopScope));
    }

    private sealed class TestWorkerScope : IScopeDefinition
    {
        public ScopeOptions Options =>
            ScopeOptions.Worker(
                tickRateHz: 30,
                faultPolicy: ScopeFaultPolicy.StopScope);
    }

    private sealed class TestManualScope : IScopeDefinition
    {
        public ScopeOptions Options =>
            ScopeOptions.Manual(
                faultPolicy: ScopeFaultPolicy.StopScope);
    }

    private sealed class WorkerScopeLayer : Layer, IGeneratedScopeDefinitionProvider
    {
        public GeneratedScopeDefinition[] __GetScopeDefinitions()
        {
            return new[]
            {
                new GeneratedScopeDefinition(
                    scopeId: 481,
                    identity: "scope:test:TestWorkerScope",
                    scopeType: typeof(TestWorkerScope),
                    factory: static () => new TestWorkerScope())
            };
        }
    }

    private sealed class ManualScopeLayer : Layer, IGeneratedScopeDefinitionProvider
    {
        public GeneratedScopeDefinition[] __GetScopeDefinitions()
        {
            return new[]
            {
                new GeneratedScopeDefinition(
                    scopeId: 482,
                    identity: "scope:test:TestManualScope",
                    scopeType: typeof(TestManualScope),
                    factory: static () => new TestManualScope())
            };
        }
    }

    private sealed class ConflictingScopeIdsModule : IAssemblyModule
    {
        public AssemblyModuleId Id => new("conflicting-scope-ids");

        public AssemblyModuleManifest Manifest { get; } =
            new AssemblyModuleManifest(
                new AssemblyModuleId("conflicting-scope-ids"),
                Array.Empty<ServiceContribution>(),
                Array.Empty<ContextContribution>(),
                Array.Empty<LocalCallContribution>(),
                Array.Empty<EventHandlerContribution>(),
                Array.Empty<LayerToolContribution>(),
                Array.Empty<EventContribution>(),
                new[]
                {
                    new GeneratedScopeDefinition(
                        scopeId: 481,
                        identity: "scope:test:ConflictingScope",
                        scopeType: typeof(ConflictingScope),
                        factory: static () => new ConflictingScope())
                });
        }

        private sealed class ConflictingScope : IScopeDefinition
        {
            public ScopeOptions Options => ScopeOptions.Inline;
        }
    }
}
```

### Step 2：确认失败

预期：Worker Scope 被构建为 Inline，或者冲突未被正确捕获。

### Step 3：实现 Registry 构建

在 `RuntimeCompositionPlan.Build` 开始阶段创建：

```csharp
var scopeRegistry = new ScopeDefinitionRegistry();
```

按顺序注册：

1. `CompositionContributions.ScopeDefinitions`
2. 所有 `IGeneratedScopeDefinitionProvider.__GetScopeDefinitions()`

模块来源字符串：

```csharp
$"module:{plan.ModuleId}"
```

本地来源字符串：

```csharp
$"layer:{layer.GetType().FullName}"
```

把原来的：

```csharp
Dictionary<Type, int> scopeIdsByType
```

替换为 `ScopeDefinitionRegistry`。

Owner Scope 解析必须使用：

```csharp
GeneratedScopeDefinition definition = scopeRegistry.Require(scopeType);
return definition.ScopeId;
```

### Step 4：构建真实 ScopeExecutionPlan

```csharp
private static ScopeExecutionPlan[] BuildScopeExecutionPlans(
    LayerBuildPlan[] layerPlans,
    ScopeDefinitionRegistry registry)
{
    int[] layerIndexes = layerPlans
        .OrderBy(static layer => layer.LayerIndex)
        .Select(static layer => layer.LayerIndex)
        .ToArray();

    return registry.OrderedDefinitions
        .Select(definition =>
        {
            IScopeDefinition instance = definition.CreateDefinition();

            return new ScopeExecutionPlan(
                new ScopeDescriptor(
                    definition.ScopeId,
                    definition.ScopeType.Name,
                    definition.ScopeType),
                instance.Options,
                layerSlices: layerIndexes
                    .Select(static index => new ScopeLayerSlice(index))
                    .ToArray(),
                lifecyclePlan:
                    ScopeLifecyclePlan.EmptyForLayerIndexes(layerIndexes));
        })
        .ToArray();
}
```

### Step 5：验证

```bash
dotnet test LayerBase.Test/LayerBase.Test.csproj -c Debug \
  --filter "FullyQualifiedName~ScopeDefinitionOptionsTests"
```

### Step 6：提交

```bash
git add LayerBase/Scope/RuntimeCompositionPlan.cs \
        LayerBase/Scope/ScopeDefinitionRegistry.cs \
        LayerBase.Test/ScopeDefinitionOptionsTests.cs
git commit -m "fix(scope): preserve generated scope options"
```
