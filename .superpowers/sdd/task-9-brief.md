## Task 9：所有 Scope 共用 Runtime Post 工作量预算

主 Runtime 当前只把 MainScope Post 消耗计入 RuntimeFrameBudget，Inline Scope 会各自完整 Pump。

### Files

* Modify: `LayerBase/Actor/Pump/RuntimeFrameBudget.cs`
* Modify: `LayerBase/Event/PostScheduler/PostScheduler.cs`
* Modify: `LayerBase/Scope/ScopeRuntime.cs`
* Modify: `LayerBase/Scope/ScopeRuntimeHost.cs`
* Modify: `LayerBase/Application/LayerRuntime.cs`
* Create: `LayerBase.Test/RuntimeScopeBudgetTests.cs`

### Required behavior

增加：

```csharp
PostPumpStats PostScheduler.Pump(ref RuntimeFrameBudget budget)
```

规则：
* Scope 的 Ingress、Control、Timer、Update 仍正常推进。
* Post Dispatch 消耗统一 `RuntimeFrameBudget`。
* MainScope、Inline Scope、Actor Runtime 使用同一预算实例。
* Inline Scope 按轮转起点处理，避免总是由第一个 Scope 抢完预算。
* 预算为 0 表示无限制，保持当前兼容行为。

### `RuntimeFrameBudget`

Add fields for tracking the shared post budget:

```csharp
internal struct RuntimeFrameBudget
{
    public int RemainingPostCount;
    public int StartingScopeIndex; // For round-robin rotation
}
```

### Implementation

1. **RuntimeFrameBudget** - Add `RemainingPostCount` and `StartingScopeIndex` fields
2. **PostScheduler.Pump(ref RuntimeFrameBudget)** - new overload that respects the budget
3. **ScopeRuntimeHost.PumpInlineScopes** - pass shared budget reference
4. **LayerRuntime.Pump** - create shared budget, pass to ScopeHost

### Verification

```powershell
dotnet test LayerBase.Test/LayerBase.Test.csproj -c Debug --filter "FullyQualifiedName~RuntimeScopeBudgetTests"
```

### Commit

```powershell
git add LayerBase/Actor/Pump/RuntimeFrameBudget.cs LayerBase/Event/PostScheduler/PostScheduler.cs LayerBase/Scope/ScopeRuntime.cs LayerBase/Scope/ScopeRuntimeHost.cs LayerBase/Application/LayerRuntime.cs LayerBase.Test/RuntimeScopeBudgetTests.cs
git commit -m "fix(runtime): share post budget across scopes"
```
