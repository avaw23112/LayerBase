# Task 6：将 Scope Fault 移入可靠 Completion 通道

当前�?Main Scope �?Fault 作为 Critical Event 投递到有界 EventInbox，并忽略失败结果�?现有测试也仍�?EventInbox 中出�?Fault Event 为验收�?
## 文件

* 修改：`LayerBase/Scope/ScopeCompletionInbox.cs`
* 修改：`LayerBase/Scope/ScopeRuntime.cs`
* 修改：`LayerBase.Test/ScopeFaultPropagationTests.cs`
* 修改：`LayerBase.Test/WorkerCompletionInboxTests.cs`

## Envelope

```csharp
internal enum ScopeCompletionKind : byte
{
    WorkerExecutionCompleted = 0,
    WorkerCancelRequested = 1,
    WorkerExecutionStarted = 2,
    ScopeFault = 3
}
```

增加�?
```csharp
public ScopeFaultRecord FaultRecord { get; }
```

以及工厂�?
```csharp
public static ScopeCompletionEnvelope ScopeFault(
    in ScopeFaultRecord record)
{
    var emptyCompletion =
        default(WorkerExecutionCompletedScopeEvent);

    return new ScopeCompletionEnvelope(
        ScopeCompletionKind.ScopeFault,
        in emptyCompletion,
        WorkerHandle.Invalid,
        in record);
}
```

## Fault 投�?
�?Main Scope�?
```csharp
ScopeCompletionEnvelope envelope =
    ScopeCompletionEnvelope.ScopeFault(in record);

mainEndpoint.Transport.EnqueueCompletion(in envelope);
```

Main Scope Drain�?
```csharp
case ScopeCompletionKind.ScopeFault:
    _runtime.ReportScopeFault(envelope.FaultRecord);
    break;
```

Source Scope 仍然在本地执�?`ApplyFaultPolicy()`，Main Scope 只负责统一报告，不重复执行策略�?
## 测试修改

原测试：

```csharp
Assert.That(
    host.MainScope.Transport.EventInbox.TryDequeue(out var envelope),
    Is.True);
```

改为检查：

```csharp
Assert.That(
    host.MainScope.Transport.CompletionInbox.Count,
    Is.EqualTo(1));
```

再调用：

```csharp
host.MainScope.PumpIngress();
```

确认 `runtime.Faulted` 被调用�?
新增关键测试�?
```text
填满 Main EventInbox
�?Worker/Inline Scope 报错
�?Fault 仍进�?CompletionInbox
�?Main Pump �?Faulted 回调被调�?```

提交�?
```powershell
git add LayerBase/Scope `
        LayerBase.Test/ScopeFaultPropagationTests.cs `
        LayerBase.Test/WorkerCompletionInboxTests.cs

git commit -m "fix(scope): route fault records through reliable completion inbox"
```

---

