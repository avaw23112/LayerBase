# Task 4：修�?Inline Scope 公平轮转和预算统�?
当前公平游标存放在每帧重新创建的 `RuntimeFrameBudget.StartingScopeIndex` 中，因此下一帧重新从 0 开始�?
同时 Inline Scope 调用 `PostScheduler.Pump(ref budget)` 后没有将处理数量计入 `UsedWorkItems`�?�?Scope 则会显式消费数量�?
## 文件

* 修改：`LayerBase/Scope/ScopeRuntimeHost.cs`
* 修改：`LayerBase/Scope/ScopeRuntime.cs`
* 修改：`LayerBase.Test/RuntimeScopeBudgetTests.cs`
* 新增：`LayerBase.Test/InlineScopeFairnessTests.cs`

## 公平轮转实现

Host 新增�?
```csharp
private int _nextInlineScopeIndex;
```

替换�?
```csharp
int startIndex =
    budget.StartingScopeIndex % _inlineScopes.Length;
```

为：

```csharp
int startIndex =
    _nextInlineScopeIndex % _inlineScopes.Length;
```

结束后：

```csharp
_nextInlineScopeIndex =
    (startIndex + 1) % _inlineScopes.Length;
```

保留 `RuntimeFrameBudget.StartingScopeIndex` 字段，避免公开结构体破坏性变更，但不再将它作�?Host 持久状态�?
## 预算消费实现

`ScopeRuntime.PumpScopeResourcesCore()`�?
```csharp
PostPumpStats postStats =
    PostScheduler?.Pump(ref budget)
    ?? new PostPumpStats(0, 0, 0, 0);

budget.Consume(postStats.ProcessedCount);
```

## 公平测试

测试逻辑必须是：

```text
Scope A �?B 各有一个事�?第一帧预�?1：A 被处理，B 保留
再次�?A 投递一个事�?第二帧预�?1�?    正确实现必须先处�?B
    若仍�?A 开始，B 会继续饥�?```

最终断言�?
```csharp
Assert.That(scopeB.PostScheduler!.HasPendingWork, Is.False);
Assert.That(scopeA.PostScheduler!.HasPendingWork, Is.True);
```

## 预算测试

断言 Inline Scope 处理三个 Post 后：

```csharp
Assert.That(budget.UsedWorkItems, Is.EqualTo(3));
Assert.That(budget.RemainingPostCount, Is.EqualTo(0));
```

提交�?
```powershell
git add LayerBase/Scope `
        LayerBase.Test/RuntimeScopeBudgetTests.cs `
        LayerBase.Test/InlineScopeFairnessTests.cs

git commit -m "fix(scope): persist inline fairness and consume shared work budget"
```

---

