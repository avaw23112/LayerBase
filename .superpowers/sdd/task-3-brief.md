# Task 3：ScopeWorker 超时后的延迟资源回收

先纠正执行目标：当前 Timeout 分支**不会立即释放正在使用�?WaitHandle**；真正的问题是线程稍后退出后，没有可靠的后续回收路径。当前资源只在成�?Join 或未启动时释放�?
## 文件

* 修改：`LayerBase/Scope/ScopeWorker.cs`
* 修改：`LayerBase.Test/WorkerShutdownTimeoutTests.cs`

## 设计

增加握手状态：

```csharp
private int _startWaitCompleted;
private int _threadExited;
private int _resourcesReleased;
```

增加安全唤醒入口�?
```csharp
private void SignalWork()
{
    if (Volatile.Read(ref _resourcesReleased) != 0)
        return;

    try
    {
        _workSignal.Set();
    }
    catch (ObjectDisposedException)
    {
        // Thread has already exited and released the wake handle.
    }
}
```

构造函数改为：

```csharp
_runtime.BindWorkerWakeSignal(SignalWork);
```

`Start()` 必须使用 finally 完成握手�?
```csharp
public void Start(in ShutdownDeadline deadline)
{
    if (_startedThread)
        return;

    _startedThread = true;
    _thread.Start();

    try
    {
        int remaining = deadline.RemainingMilliseconds;

        if (remaining <= 0 || !_ready.Wait(remaining))
        {
            throw new TimeoutException(
                $"Scope worker `{_runtime.Descriptor.Name}` did not become ready before the build deadline.");
        }

        Exception? startupException =
            Volatile.Read(ref _startupException);

        if (startupException != null)
        {
            throw new InvalidOperationException(
                $"Scope worker `{_runtime.Descriptor.Name}` failed during startup.",
                startupException);
        }
    }
    finally
    {
        Volatile.Write(ref _startWaitCompleted, 1);
        TryReleaseResourcesAfterExit();
    }
}
```

Worker `Run()` �?finally�?
```csharp
finally
{
    try
    {
        if (_runtime.State != ScopeRuntimeState.Disposed)
            _runtime.RunRuntimeStop();
    }
    finally
    {
        SynchronizationContext.SetSynchronizationContext(previousContext);
        Volatile.Write(ref _threadExited, 1);
        TryReleaseResourcesAfterExit();
    }
}
```

释放逻辑�?
```csharp
private void TryReleaseResourcesAfterExit()
{
    if (Volatile.Read(ref _startWaitCompleted) == 0 ||
        Volatile.Read(ref _threadExited) == 0)
    {
        return;
    }

    ReleaseResources();
}

private void ReleaseResources()
{
    if (Interlocked.Exchange(ref _resourcesReleased, 1) != 0)
        return;

    _ready.Dispose();
    _workSignal.Dispose();
}
```

## 测试

增加可解除阻塞的 Worker�?
```text
开始阻�?�?Stop 超时
�?确认尚未释放资源
�?解除阻塞
�?Worker 退�?�?最终资源自动释�?```

允许添加内部诊断属性：

```csharp
internal bool ResourcesReleased =>
    Volatile.Read(ref _resourcesReleased) != 0;
```

不得添加公开 API�?
提交�?
```powershell
git add LayerBase/Scope/ScopeWorker.cs `
        LayerBase.Test/WorkerShutdownTimeoutTests.cs

git commit -m "fix(scope): release worker signals after delayed thread exit"
```

---

