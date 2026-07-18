## Task 1：统一 EventTypeId 的唯一来源

当前 `EventTypeIdAllocator.Resolve(Type)` 和 `EventTypeId<TEvent>.Id` 会分别调用 `Allocate()`，两者没有共享映射。CompositionPlan 使用前者，EventMetaData 使用后者，而 Build 又要求两者相等。

### Files

* Modify: `LayerBase/Event/Event/EventTypeId.cs`
* Modify: `LayerBase/Scope/RuntimeCompositionPlan.cs`
* Create: `LayerBase.Test/EventTypeIdUnificationTests.cs`

### Required behavior

* `EventTypeId<TEvent>.Id` 是唯一运行时 EventId 来源。
* `RuntimeCompositionPlan` 从元数据实例读取 `EventId`。
* 不再通过 `Type` 单独分配第二个 ID。
* 元数据声明的事件类型必须和 `EventContribution.EventType` 一致。

### Step 1：写失败测试

新增：

```csharp
[TestFixture]
[Category("ProductionHardening")]
public sealed class EventTypeIdUnificationTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        EventMetaDataHandler.Clear();
    }

    [Test]
    public void Module_event_metadata_uses_generic_event_id()
    {
        using LayerRuntime runtime = LayerHub.CreateLayers()
            .Push(new TestLayer())
            .AddAssemblyModule(new TestModule())
            .Build();

        int eventId = EventTypeId<TestEvent>.Id;
        EventPostPolicy? policy = runtime.PolicyTable.GetPostPolicy(eventId);

        Assert.That(policy, Is.Not.Null);
        Assert.That(policy!.Value.Mode, Is.EqualTo(PostDeliveryMode.Latest));
    }

    private readonly struct TestEvent;

    private sealed class TestEventMetaData : EventMetaData<TestEvent>
    {
        public override EventPostPolicy? PostPolicy =>
            new EventPostPolicy(
                PostDeliveryMode.Latest,
                BackpressurePolicy.RejectNew,
                maxPending: 1);
    }

    private sealed class TestLayer : Layer
    {
    }

    private sealed class TestModule : IAssemblyModule
    {
        public AssemblyModuleId Id => new("event-id-unification");

        public AssemblyModuleManifest Manifest { get; } =
            new AssemblyModuleManifest(
                new AssemblyModuleId("event-id-unification"),
                Array.Empty<ServiceContribution>(),
                Array.Empty<ContextContribution>(),
                Array.Empty<LocalCallContribution>(),
                Array.Empty<EventHandlerContribution>(),
                Array.Empty<LayerToolContribution>(),
                new[]
                {
                    EventContribution.ForTypes(
                        typeof(TestEvent),
                        typeof(TestLayer),
                        typeof(MainScope),
                        static () => new TestEventMetaData())
                });
    }
}
```

### Step 2：确认测试失败

```bash
dotnet test LayerBase.Test/LayerBase.Test.csproj -c Debug \
  --filter "FullyQualifiedName~EventTypeIdUnificationTests"
```

预期：Build 抛出 metadata EventId mismatch。

### Step 3：实现唯一 ID 来源

在 `ResolveEventPlans` 中先实例化元数据原型：

```csharp
IEventMetaData metaData = ev.MetaDataFactory()
    ?? throw new InvalidOperationException(
        $"Event metadata factory for `{ev.EventType.FullName}` returned null.");

int eventId = metaData.EventId;
EventIdentity identity = metaData.GetIdentity();

if (identity.EventType != ev.EventType)
{
    throw new InvalidOperationException(
        $"Event metadata `{metaData.GetType().FullName}` represents " +
        $"`{identity.EventType.FullName}`, but contribution declares " +
        $"`{ev.EventType.FullName}`.");
}
```

使用该 `eventId` 创建 `EventMetaDataBuildPlan`。

从 `EventTypeIdAllocator` 删除：

```csharp
private static readonly Dictionary<Type, int> s_typeToId;
private static readonly object s_lock;
public static int Resolve(Type eventType);
```

保留：

```csharp
public static int Allocate();
public static int MaxId;
```

### Step 4：验证

```bash
dotnet test LayerBase.Test/LayerBase.Test.csproj -c Debug \
  --filter "FullyQualifiedName~EventTypeIdUnificationTests"
```

预期：PASS。

### Step 5：提交

```bash
git add LayerBase/Event/Event/EventTypeId.cs \
        LayerBase/Scope/RuntimeCompositionPlan.cs \
        LayerBase.Test/EventTypeIdUnificationTests.cs
git commit -m "fix(event): unify generated metadata event ids"
```
