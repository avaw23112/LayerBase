## Task 10：为 WorkerJob 和 ScopeWorker 增加有界停机

当前 WorkerJobScheduler 和 ScopeWorker 都使用无超时 `Thread.Join()`。

### Files

* Modify: `LayerBase/Worker/WorkerJobSchedulerOptions.cs`
* Modify: `LayerBase/Worker/WorkerJobScheduler.cs`
* Modify: `LayerBase/Scope/ScopeWorker.cs`
* Modify: `LayerBase/Application/LayerRuntime.cs`
* Create: `LayerBase/Worker/WorkerShutdownTimeoutException.cs`
* Create: `LayerBase.Test/WorkerShutdownTests.cs`

### Configuration

```csharp
int shutdownTimeoutMilliseconds = 5000
```

### Required behavior

* 先请求协作式取消。
* 使用统一 deadline Join 所有线程。
* 超时后不得无限阻塞。
* 不使用 `Thread.Abort`。
* 未退出线程仍是 Background Thread。
* 超时异常必须包含线程名、运行 Job Handle 和等待时间。
* 线程尚未退出时不得 Dispose 它仍可能访问的 Signal。
* LayerRuntime 即使停机异常，也必须在 finally 中清除 Hub 注册和 Runtime Cache。

### Tests

```csharp
Worker_scheduler_dispose_is_bounded_when_job_ignores_cancellation
Scope_worker_dispose_is_bounded_when_scope_is_blocked
Normal_worker_shutdown_releases_all_threads
Runtime_unregisters_even_when_worker_shutdown_times_out
```

### Verification

```powershell
dotnet test LayerBase.Test/LayerBase.Test.csproj -c Debug --filter "FullyQualifiedName~WorkerShutdownTests"
```

### Commit

```powershell
git add LayerBase/Worker/WorkerJobSchedulerOptions.cs LayerBase/Worker/WorkerJobScheduler.cs LayerBase/Scope/ScopeWorker.cs LayerBase/Application/LayerRuntime.cs LayerBase/Worker/WorkerShutdownTimeoutException.cs LayerBase.Test/WorkerShutdownTests.cs
git commit -m "fix(worker): bound cooperative shutdown"
```
