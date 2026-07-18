## Task 6：把 Timer Policy 编译到 Build 阶段

当前 Schedule 在 Payload 已 Store 后才验证 ExpiredPostPolicy，并在热路径构造事件类型字符串。

### Files

* Create: `LayerBase/Event/TimeScheduler/TimerPostTypePlan.cs`
* Modify: `LayerBase/Event/TimeScheduler/PostTimerScheduler.cs`
* Modify: `LayerBase/Application/LayerRuntime.cs`
* Modify: `LayerBase/Scope/ScopeRuntime.cs`
* Create: `LayerBase.Test/SchedulePostAllocationTests.cs` (or modify existing)

### Required design

1. **Create `TimerPostTypePlan` struct** - precompiled plan for timer->post routing:

```csharp
internal readonly struct TimerPostTypePlan
{
    public TimerPostTypePlan(
        int eventTypeId,
        bool hasExpiredOverride,
        PostTypePlan expiredPlan,
        TimerRepeatMode? repeatMode,
        TimerCatchUpPolicy? catchUpPolicy)
    {
        EventTypeId = eventTypeId;
        HasExpiredOverride = hasExpiredOverride;
        ExpiredPlan = expiredPlan;
        RepeatMode = repeatMode;
        CatchUpPolicy = catchUpPolicy;
    }

    public int EventTypeId { get; }
    public bool HasExpiredOverride { get; }
    public PostTypePlan ExpiredPlan { get; }
    public TimerRepeatMode? RepeatMode { get; }
    public TimerCatchUpPolicy? CatchUpPolicy { get; }
}
```

2. **Modify `PostTimerScheduler`**:
   - Add field `private TimerPostTypePlan[] _timerPlans = Array.Empty<TimerPostTypePlan>();`
   - Add method to install plans at build time
   - In `Schedule` hot path, use `ref readonly TimerPostTypePlan plan = ref GetPlan(eventId);` instead of querying PolicyTable
   - Remove hot-path validation of ExpiredPostPolicy

3. **Build phase integration**:
   - In `ScopeRuntime`, compile timer policies into `TimerPostTypePlan[]` during build
   - Install plans on the PostTimerScheduler
   - Verify `ExpiredPostPolicy` validity during build (fail fast)

4. **SchedulePostAllocationTests**: Add zero-alloc assertion after prewarm.

### Verification

```powershell
dotnet test LayerBase.Test/LayerBase.Test.csproj -c Debug --filter "FullyQualifiedName~SchedulePost"
```

### Commit

```powershell
git add LayerBase/Event/TimeScheduler/TimerPostTypePlan.cs LayerBase/Event/TimeScheduler/PostTimerScheduler.cs LayerBase/Application/LayerRuntime.cs LayerBase/Scope/ScopeRuntime.cs
git commit -m "perf(timer): compile timer post policies at build"
```
