# MainScope Scope-first Runtime Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make MainScope the mandatory default execution domain and remove duplicate business-resource ownership from LayerRuntime.

**Architecture:** LayerRuntime remains the application aggregate root and Pump root, while every business resource belongs to a ScopeRuntime. MainScope is always created at Scope ID 0 and receives unannotated Services, default Layer execution, events, timers, delay, ECS and continuations. Runtime-level APIs that must remain temporarily are stateless forwards to MainScope rather than owned resources.

**Tech Stack:** C# 12, .NET 8/9, NUnit, Roslyn incremental generators, GitHub Actions.

## Global Constraints

- MainScope always uses `ScopeId == 0`.
- Custom Scope IDs start at 1.
- LayerRuntime must not allocate, Pump or Dispose a second EventCenter, PostScheduler, Timer, DelayManager, ECS World, ECS Scheduler or business SynchronizationContext.
- Scope-owned Services and Contexts have exactly one lifecycle owner.
- ActorWorld remains Runtime-owned and is Pumped exactly once.
- Existing explicit custom Scope routing remains valid.
- No new runtime reflection on generated Module hot paths.

---

### Task 1: Establish failing ownership tests

**Files:**
- Create: `LayerBase.Test/MainScopeOwnershipTests.cs`

- [ ] Verify an empty Layer Runtime still creates exactly one MainScope.
- [ ] Verify an unannotated Service receives a `ScopeObjectBinding` whose Scope is MainScope.
- [ ] Verify Runtime event, timer, delay and ECS entry points reference MainScope instances.
- [ ] Verify a Layer Pump executes with `ScopeExecution.Current.ScopeId == 0`.
- [ ] Verify MainScope Service Initialize and Update execute once.
- [ ] Verify Module metadata reserves MainScope ID 0 without a contributed definition.

### Task 2: Make ScopeHost and MainScope mandatory

**Files:**
- Modify: `LayerBase/Application/LayerRuntime.cs`
- Modify: `LayerBase/Scope/ScopeRuntimePlanner.cs` only if existing MainScope plan behavior is insufficient.

- [ ] Collect all unique resolved Services in `InitializeScopeHost()`.
- [ ] Remove the early return for an empty explicitly-scoped Service list.
- [ ] Pass Post, Timer, Delay and ECS configuration through `ScopeRuntimeOptions` when creating the Host.
- [ ] Remove the Module-path `ScopeDefinitions.Count == 0` fallback.
- [ ] Add a strict `MainScope` accessor resolving Scope ID 0.

### Task 3: Transfer event, time and continuation ownership

**Files:**
- Modify: `LayerBase/Application/LayerRuntime.cs`
- Modify: `LayerBase/Scope/ScopeRuntime.cs`
- Modify: `LayerBase/Event/Delay/DelayPublisherManager.cs`

- [ ] Remove Runtime EventCenter, PostScheduler, TimeScheduler, DelayManager, PostIngress and business SynchronizationContext fields and construction.
- [ ] Move full EventBuildPolicyTable construction and Post plan prewarming into ScopeRuntime.
- [ ] Move cross-thread PostIngressQueue to ScopeRuntime.
- [ ] Route default Runtime/Layer Send, Post, Schedule and Delay operations to MainScope.
- [ ] Ensure ScopeRuntime owns continuation draining and close semantics.
- [ ] Keep any temporary Runtime compatibility member as a getter or forwarding method only.

### Task 4: Transfer ECS ownership

**Files:**
- Modify: `LayerBase/Application/LayerRuntime.ECS.cs`
- Modify: `LayerBase/Application/LayerRuntime.cs`
- Modify: `LayerBase/Scope/ScopeRuntime.cs`

- [ ] Remove Runtime ECS World/Scheduler initialization and disposal.
- [ ] Store pre-Build ECS configuration in LayerRuntime and supply it to ScopeRuntimeOptions.
- [ ] Make Runtime ECS accessors resolve MainScope ECS resources.
- [ ] Remove Runtime ECS Start, frame notifications, flush and sweep from Runtime Pump.
- [ ] Preserve test fence helpers by forwarding to MainScope scheduler.

### Task 5: Run Layer and Service lifecycle inside MainScope

**Files:**
- Modify: `LayerBase/Application/LayerRuntime.cs`
- Modify: `LayerBase/Scope/ScopeRuntime.cs`
- Modify: `LayerBase/Layer/Layer.cs`
- Modify: `LayerBase/DI/ServiceProvider.cs`
- Modify: `LayerBase/DI/WorldServiceRoot.cs`

- [ ] Let MainScope invoke LayerChain Update and FixedUpdate from inside Scope execution.
- [ ] Extend Scope Service/Context lifecycle to Initializable, PostBuild, RuntimeStart, Update, FixedUpdate, RuntimeStop and Dispose.
- [ ] Skip Layer lifecycle collection for every ScopeObjectBinder-bound Service.
- [ ] Prevent legacy DI containers from disposing Scope-owned objects.
- [ ] Keep Layer logical membership, handles and diagnostics intact.

### Task 6: Make Module composition include MainScope objects

**Files:**
- Modify: `LayerBase/Modules/ModuleRuntimeBuilder.cs`
- Modify: `LayerBase/Scope/ScopeCompositionBuilder.cs`

- [ ] Seed `ScopeIds` with `typeof(MainScope).TypeHandle -> 0`.
- [ ] Allow MainScope-targeting Service contributions without an explicit Scope definition.
- [ ] Keep custom Scope IDs stable from 1.
- [ ] Materialize MainScope Services, Contexts and ResourcePlan in `Scopes[0]`.
- [ ] Preserve route validation and custom Scope composition.

### Task 7: Simplify Runtime Pump and disposal

**Files:**
- Modify: `LayerBase/Application/LayerRuntime.cs`

- [ ] Runtime Pump drains Worker events to MainScope, calls ScopeHost.Pump, drains Actor commands, Pumps ActorWorld once and drains exceptions.
- [ ] Remove Runtime Timer, Delay, Scheduler, ECS and SynchronizationContext Pump paths.
- [ ] Remove disposal of resources now owned by ScopeHost.
- [ ] Verify Build abort disposes ScopeHost and leaves no duplicate-resource cleanup path.

### Task 8: Verify the branch

- [ ] Apply exact source patch in a clean checkout.
- [ ] Build Release with .NET 9.
- [ ] Run the complete NUnit suite.
- [ ] Run repository CI with .NET 8 and .NET 9 through the draft PR.
- [ ] Inspect changed files for unrelated edits and temporary automation files.
- [ ] Keep the PR in draft until all checks pass.
