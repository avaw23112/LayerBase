# Task 0 Brief - Failing Scope Ownership/Shutdown Regression Tests

Implement only tests and test helpers for Task 0. Do not modify production code in this task. The tests may fail on current production code; capture the focused failure output.

# 四、Task 0：建立失败测试基�?

**测试文件�?*

```text
LayerBase.Test/ScopeLifecycleConcurrencyTests.cs
LayerBase.Test/ScopePromiseShutdownTests.cs
LayerBase.Test/ProjectedActorOwnershipTests.cs
LayerBase.Test/ScopeResourceGenerationTests.cs
LayerBase.Test/ModuleRuntimeIsolationTests.cs
LayerBase.Test/ScopeDiGenerationTests.cs
```

## Step 1：Start/Stop 竞态测�?

创建一个阻塞在 Start 中的 Service�?

```csharp
private sealed class BlockingStartService : IService, IInitializable, IDisposable
{
    public readonly ManualResetEventSlim StartEntered = new(false);
    public readonly ManualResetEventSlim AllowStartReturn = new(false);

    public int InitializeCount;
    public int DisposeCount;

    public void ConfigureServices(IServiceCollection services)
    {
    }

    public void Initialize()
    {
        Interlocked.Increment(ref InitializeCount);
        StartEntered.Set();
        AllowStartReturn.Wait();
    }

    public void Dispose()
    {
        Interlocked.Increment(ref DisposeCount);
    }
}
```

测试�?

```csharp
[Test]
public void Start_and_stop_must_not_initialize_disposed_service()
{
    var service = new BlockingStartService();
    using var scope = CreateWorkerScope(service);

    Task start = Task.Run(scope.Start);
    Assert.That(service.StartEntered.Wait(TimeSpan.FromSeconds(2)), Is.True);

    Task stop = Task.Run(scope.Stop);

    service.AllowStartReturn.Set();

    Assert.That(Task.WaitAll(new[] { start, stop }, TimeSpan.FromSeconds(5)), Is.True);
    Assert.That(service.InitializeCount, Is.EqualTo(1));
    Assert.That(service.DisposeCount, Is.EqualTo(1));
}
```

预期：当前实现应在压力循环下暴露顺序问题�?

---

## Step 2：Stop/Dispose 同步测试

```csharp
private sealed class BlockingDisposeService : IService, IDisposable
{
    public readonly ManualResetEventSlim DisposeEntered = new(false);
    public readonly ManualResetEventSlim AllowDisposeReturn = new(false);

    public void ConfigureServices(IServiceCollection services)
    {
    }

    public void Dispose()
    {
        DisposeEntered.Set();
        AllowDisposeReturn.Wait();
    }
}
```

测试必须验证 `Dispose()` 不会�?Stop Cleanup 完成前返回�?

---

## Step 3：Continuation 原子关闭测试

使用 Barrier 控制�?

```text
生产者通过 IsClosed 检�?
关闭线程执行 CloseAndDrain
生产者继�?Enqueue
```

最终必须满足：

```text
Enqueue 成功 -> Continuation 一定被执行
Enqueue 失败 -> Continuation 从未进入 Inbox
```

不存在成功返回但未执行的情况�?

---

## Step 4：ProjectedActor Owner Thread 测试

创建 Worker Scope + Shared ActorWorld，记录：

```text
DisableProjectedActor 调用线程
ReleaseProjectedActor 调用线程
LayerRuntime Owner Thread
Scope Worker Thread
```

要求 Disable/Release 只能出现�?LayerRuntime Owner Thread�?

---

## Step 5：运行测试并确认失败

```bash
dotnet test LayerBase.Test/LayerBase.Test.csproj -c Release
```

提交�?

```bash
git commit -m "test: add scope ownership and shutdown regressions"
```

---

