## Task 8：修正 ReportAndContinue 和处理器级异常隔离

`ReportFault` 当前会无条件把 Scope 设为 Faulted 并关闭入口，导致 `ReportAndContinue` 实际停止 Scope。

`DispatchNotifySafe` 和多 Handler `DispatchSync` 都用一个 try/catch 包围整个循环，一个 Handler 抛异常后会跳过后续 Handler。

### Files

* Modify: `LayerBase/Scope/ScopeRuntime.cs`
* Modify: `LayerBase/Scope/ScopeRuntimeHost.cs`
* Modify: `LayerBase/Event/Event/EventCenter.cs`
* Create: `LayerBase.Test/ScopeReportAndContinueTests.cs`
* Modify: `LayerBase.Test/Safety/EventCenterSafetyTests.cs`

### Fault policy behavior

`ReportFault` 只负责：
* 创建记录。
* 增加故障计数。
* 报告记录。
* 交给 Host 应用策略。

不得提前修改 Scope State。

Host 根据策略处理：
```
ReportAndContinue → 保持 Running，入口保持开启
StopScope        → RequestStopAsync
StopRuntime      → 请求 Runtime 停止
```

仅内部不可恢复的不变量错误使用独立方法：
```csharp
internal void ReportFatalFault(Exception exception, ScopeFaultPhase phase)
```
该方法才允许进入 `Faulted` 并关闭入口。

### Handler isolation

Safe Subscribe 和 Sync Flow 都逐 Handler catch：
```csharp
for (int i = start; i < end; i++)
{
    try { handlers[i](in value); }
    catch (Exception ex) { HandleFault(...); }
}
```

Flow 必须保留：
* `Handled` 立即结束。
* `HandledAndContinue` 累积。
* 抛异常的 Handler 按 Continue 处理。
* 后续 Handler 继续执行。

Unsafe Notify 保持 fail-fast，不改变语义。

### Verification

```powershell
dotnet test LayerBase.Test/LayerBase.Test.csproj -c Debug --filter "FullyQualifiedName~ScopeReportAndContinueTests"
```

### Commit

```powershell
git add LayerBase/Scope/ScopeRuntime.cs LayerBase/Scope/ScopeRuntimeHost.cs LayerBase/Event/Event/EventCenter.cs LayerBase.Test/ScopeReportAndContinueTests.cs
git commit -m "fix(scope): honor fault policy and isolate handlers"
```
