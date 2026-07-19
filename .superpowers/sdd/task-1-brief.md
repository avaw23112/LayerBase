# Task 1：补�?Worker Pending �?Running �?Terminal 状�?
`MarkExecutionStarted()` 已经存在，并负责设置 `WorkerState.Running` 和增�?`_runningCount`，但 Worker 执行路径没有发布"开始执�?通知�?当前 `WorkerExecutionItem.Execute()` 直接执行 Job 并只发布最终完成事件�?
## 文件

* 修改：`LayerBase/Scope/ScopeCompletionInbox.cs`
* 修改：`LayerBase/Scope/ScopeRuntime.cs`
* 修改：`LayerBase/Worker/WorkerExecutionItem.cs`
* 修改：`LayerBase/Worker/WorkerJobCoordinator.cs`
* 修改：`LayerBase.Test/WorkerCoordinatorRaceTests.cs`
* 修改：`LayerBase.Test/WorkerCompletionInboxTests.cs`

## 先写失败测试

新增�?
```csharp
[Test]
public void Blocking_job_enters_running_before_physical_completion()
{
    using var entered = new ManualResetEventSlim(false);
    using var release = new ManualResetEventSlim(false);

    var service = new BlockingWorkerService(entered, release);
    var layer = new BlockingWorkerLayer();
    layer.RegisterService(service);

    using LayerRuntime runtime = LayerHub.CreateLayers()
        .Push(layer)
        .Build();

    WorkerHandle handle = service.Run(CancellationToken.None);

    Assert.That(entered.Wait(TimeSpan.FromSeconds(5)), Is.True);

    Assert.That(
        SpinUntil(() =>
        {
            runtime.Pump(0f);
            return runtime.WorkerJobs.GetState(handle) == WorkerState.Running;
        }),
        Is.True);

    Assert.That(runtime.WorkerJobs.RunningCount, Is.EqualTo(1));

    release.Set();

    Assert.That(
        SpinUntil(() =>
        {
            runtime.Pump(0f);
            return runtime.WorkerJobs.GetState(handle) == WorkerState.Completed;
        }),
        Is.True);

    Assert.That(runtime.WorkerJobs.RunningCount, Is.EqualTo(0));
}
```

先运行：

```powershell
dotnet test LayerBase.Test/LayerBase.Test.csproj `
  -c Release `
  --filter "FullyQualifiedName~Blocking_job_enters_running_before_physical_completion"
```

预期：失败，因为当前状态不会进�?`Running`�?
## 实现要求

扩展枚举�?
```csharp
internal enum ScopeCompletionKind : byte
{
    WorkerExecutionCompleted = 0,
    WorkerCancelRequested = 1,
    WorkerExecutionStarted = 2
}
```

增加工厂�?
```csharp
public static ScopeCompletionEnvelope WorkerExecutionStarted(
    WorkerHandle handle)
{
    var emptyCompletion = default(WorkerExecutionCompletedScopeEvent);

    return new ScopeCompletionEnvelope(
        ScopeCompletionKind.WorkerExecutionStarted,
        in emptyCompletion,
        handle);
}
```

`WorkerExecutionItem.Execute()` 中：

```csharp
if (_token.IsCancellationRequested)
{
    completion = CreateCancelledCompletion();
}
else
{
    SubmitExecutionStarted();

    try
    {
        var context = new WorkerJobContext(workerIndex, _token);
        TEvent result = _job.Execute(in _input, in context);

        completion = _token.IsCancellationRequested
            ? CreateCancelledCompletion()
            : new WorkerExecutionCompletedScopeEvent(
                _handle,
                WorkerExecutionCompletionKind.Succeeded,
                new WorkerExecutionResult<TEvent>(in result),
                _options,
                WorkerJobExceptionInfo.None);
    }
    // 保留现有异常处理
}
```

新增�?
```csharp
private void SubmitExecutionStarted()
{
    ScopeCompletionEnvelope envelope =
        ScopeCompletionEnvelope.WorkerExecutionStarted(_handle);

    _origin.Transport.EnqueueCompletion(in envelope);
}
```

`ScopeRuntime.DrainCompletionInbox()` 增加�?
```csharp
case ScopeCompletionKind.WorkerExecutionStarted:
    WorkerJobs.MarkExecutionStarted(envelope.WorkerHandle);
    break;
```

## 禁止方案

禁止�?
```csharp
coordinator.MarkExecutionStarted(handle);
```

�?Worker Thread 直接调用 Coordinator。这样会破坏 Owner Thread 单写模型�?
## 验收

```powershell
dotnet test LayerBase.Test/LayerBase.Test.csproj `
  -c Release `
  --filter "FullyQualifiedName~WorkerCoordinatorRaceTests|FullyQualifiedName~WorkerCompletionInboxTests"
```

提交�?
```powershell
git add LayerBase/Scope LayerBase/Worker LayerBase.Test
git commit -m "fix(worker): publish physical execution start to origin scope"
```

---

