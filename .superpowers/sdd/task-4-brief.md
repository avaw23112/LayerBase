## Task 4：Timer 输入验证、Catch-up 上限与容量溢出

当前 Tick 会持续消费全部 accumulator，配置和输入没有完整有限值边界。

### Files

* Modify: `LayerBase/Event/TimeScheduler/TimeSchedulerOptions.cs`
* Modify: `LayerBase/Event/TimeScheduler/TimeScheduler.cs`
* Modify: `LayerBase/DataStruct/BitHelper.cs`
* Create: `LayerBase.Test/TimeSchedulerBoundaryTests.cs`

### Required behavior

构造阶段拒绝：

```
TickDurationSeconds <= 0
非有限 TickDurationSeconds
InitialTimerCapacity <= 0
MaxPromotePerTick <= 0
MaxExpiredPerTick <= 0
容量 > 1 << 30
```

Tick 阶段拒绝：

```
deltaTime < 0
NaN
PositiveInfinity
NegativeInfinity
```

在 `TimeSchedulerOptions` 尾部增加：

```csharp
public readonly int MaxCatchUpTicksPerPump = 8;
```

`TickCatchUpSlow` 最多执行该次数，剩余 accumulator 保留到下一 Pump。

### Safe NextPowerOfTwo

```csharp
public static int NextPowerOfTwo(int value)
{
    if (value <= 1)
        return 1;

    if (value > 1 << 30)
        throw new ArgumentOutOfRangeException(nameof(value));

    return (int)BitOperations.RoundUpToPowerOf2((uint)value);
}
```

### Changes needed

1. **TimeSchedulerOptions**: Add validation in constructor. Add `MaxCatchUpTicksPerPump` field. Update `Default`.
2. **TimeScheduler**: In `Tick()`, validate deltaTime. In `TickCatchUpSlow()`, add max iteration cap using `MaxCatchUpTicksPerPump`. Use `_options.MaxCatchUpTicksPerPump`.
3. **BitHelper.NextPowerOfTwo**: Add overflow guard.
4. **Tests**: Create boundary tests for all validation cases.

### Tests

```csharp
[TestFixture]
[Category("ProductionHardening")]
public sealed class TimeSchedulerBoundaryTests
{
    [Test]
    public void Invalid_timer_options_are_rejected()
    {
        Assert.That(() => new TimeSchedulerOptions(0, 512, 256, 0, 1024, 64, TimerRepeatMode.Once, TimerCatchUpPolicy.SkipMissed),
            Throws.ArgumentException);
        Assert.That(() => new TimeSchedulerOptions(float.NaN, 512, 256, 0, 1024, 64, TimerRepeatMode.Once, TimerCatchUpPolicy.SkipMissed),
            Throws.ArgumentException);
        Assert.That(() => new TimeSchedulerOptions(1/60f, 512, 0, 0, 1024, 64, TimerRepeatMode.Once, TimerCatchUpPolicy.SkipMissed),
            Throws.ArgumentException);
        Assert.That(() => new TimeSchedulerOptions(1/60f, 512, 256, 0, 0, 64, TimerRepeatMode.Once, TimerCatchUpPolicy.SkipMissed),
            Throws.ArgumentException);
        Assert.That(() => new TimeSchedulerOptions(1/60f, 512, 256, 0, 1024, 0, TimerRepeatMode.Once, TimerCatchUpPolicy.SkipMissed),
            Throws.ArgumentException);
    }

    [Test]
    public void Next_power_of_two_rejects_overflow()
    {
        Assert.That(() => BitHelper.NextPowerOfTwo(1 << 30 + 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(BitHelper.NextPowerOfTwo(3), Is.EqualTo(4));
        Assert.That(BitHelper.NextPowerOfTwo(1), Is.EqualTo(1));
        Assert.That(BitHelper.NextPowerOfTwo(0), Is.EqualTo(1));
    }

    [Test]
    public void Non_finite_delta_time_is_rejected()
    {
        using var scheduler = new TimeScheduler<int>(TimeSchedulerOptions.Default);
        Assert.That(() => scheduler.Tick(float.NaN, null), Throws.ArgumentException);
        Assert.That(() => scheduler.Tick(float.NegativeInfinity, null), Throws.ArgumentException);
        Assert.That(() => scheduler.Tick(float.PositiveInfinity, null), Throws.ArgumentException);
        Assert.That(() => scheduler.Tick(-1f, null), Throws.ArgumentException);
    }

    [Test]
    public void Catch_up_is_limited_per_pump()
    {
        var options = new TimeSchedulerOptions(
            1/60f, 512, 256, 0, 1024, 64,
            TimerRepeatMode.Once, TimerCatchUpPolicy.SkipMissed);
        using var scheduler = new TimeScheduler<int>(options);
        // Advance 100 ticks worth of time in one call - catch-up should be capped
        scheduler.Tick(100f / 60f, null);
        // After one pump, accumulator should still have remaining time
        Assert.That(scheduler.PendingCount, Is.LessThan(100)); // Not all ticks consumed
    }
}
```

### Step 2: Confirm test failure

```powershell
dotnet test LayerBase.Test/LayerBase.Test.csproj -c Debug --filter "FullyQualifiedName~TimeSchedulerBoundaryTests"
```

### Step 3: Commit

```powershell
git add LayerBase/Event/TimeScheduler/TimeSchedulerOptions.cs LayerBase/Event/TimeScheduler/TimeScheduler.cs LayerBase/DataStruct/BitHelper.cs LayerBase.Test/TimeSchedulerBoundaryTests.cs
git commit -m "fix(timer): validate inputs and cap catch-up"
```
