# Task 2：修�?Shutdown 控制结果和关闭期故障路径

当前 Host 一进入 `Dispose()` 就将自身视为 disposed，但 Scope 关闭过程中仍可能调用 `ApplyFaultPolicy()`；同�?Worker Scope �?Dispose Task 完成后没有调�?`GetResult()`，异常和响应状态都可能被忽略�?
## 文件

* 修改：`LayerBase/Scope/ScopeRuntimeHost.cs`
* 修改：`LayerBase/Application/LayerRuntime.cs`
* 新增：`LayerBase.Test/ScopeShutdownStateTests.cs`
* 修改：`LayerBase.Test/WorkerShutdownTimeoutTests.cs`

## 测试一：关闭过程中 Fault Policy 不得抛出 ObjectDisposedException

测试应：

1. 创建 Worker Scope�?2. �?Dispose 生命周期中抛出指定异常�?3. 启动 Host shutdown�?4. 确认 Fault 记录包含原始异常�?5. 确认没有�?`ObjectDisposedException` 替换�?
## 测试二：Dispose Control 异常必须被消�?
创建一�?`DisposeReverse()` 抛异常的 Worker Scope，断言�?
```csharp
Assert.That(
    recordedException,
    Is.TypeOf<InvalidOperationException>());

Assert.That(
    recordedException!.Message,
    Does.Contain("dispose failed"));
```

## 实现要求

将状态拆分为�?
```csharp
private int _shutdownStarted;
private int _disposed;
```

规则�?
```text
shutdownStarted:
    不再接受新的 Host 级业务操�?    仍允许内部目录查询、Fault Policy 和控制消�?
disposed:
    Worker 已退�?    Scope 资源已清�?    不再允许任何操作
```

`ApplyFaultPolicy()` 不再调用会触�?`ThrowIfDisposed()` 的公开查询路径�?
```csharp
public void ApplyFaultPolicy(in ScopeFaultRecord record)
{
    if (!_directory.TryGetRuntime(
            record.SourceScopeId,
            out ScopeRuntime sourceScope))
    {
        return;
    }

    switch (sourceScope.Options.FaultPolicy)
    {
        case ScopeFaultPolicy.ReportAndContinue:
            return;

        case ScopeFaultPolicy.StopScope:
            _ = sourceScope.RequestStopAsync();
            return;

        case ScopeFaultPolicy.StopRuntime:
            _ = MainScope.RequestStopAsync();
            return;

        default:
            throw new ArgumentOutOfRangeException();
    }
}
```

Worker Dispose 必须读取结果�?
```csharp
ScopeDisposeResponse response =
    ScopeControlBarrier.Wait(
        scope.RequestDisposeAsync(),
        in deadline,
        $"{scope.Descriptor.Name}.Dispose");

ScopeControlBarrier.EnsureSucceeded(
    response.State,
    "Dispose",
    scope);
```

Shutdown 可以捕获异常并继续清理其�?Scope，但不能跳过 `GetResult()`�?
## 验收

* 控制 Task 不存在未观察异常�?* Shutdown 故障不会被二�?`ObjectDisposedException` 覆盖�?* 正常关闭行为保持不变�?
提交�?
```powershell
git add LayerBase/Scope/ScopeRuntimeHost.cs `
        LayerBase/Application/LayerRuntime.cs `
        LayerBase.Test

git commit -m "fix(scope): preserve control and fault handling during shutdown"
```

---

