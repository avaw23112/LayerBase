# MainScope Default Runtime Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make `MainScope` the mandatory default execution domain of every successfully built `LayerRuntime`.

**Architecture:** Both legacy and Module build paths always create a `ScopeRuntimeHost`. Unannotated Services are grouped into the built-in MainScope, while explicit Scope annotations create additional execution domains. Layer event and lifecycle entry points resolve MainScope instead of using the parallel Runtime business domain.

**Tech Stack:** C# 12, .NET 8/9, NUnit, Roslyn incremental generators.

## Global Constraints

- MainScope always uses `ScopeId == 0`.
- Custom Scope IDs start at 1.
- No new runtime reflection on generated Module hot paths.
- Existing explicit custom Scope behavior must remain compatible.
- Production changes follow failing tests first.

---

### Task 1: Add MainScope construction regression tests

**Files:**
- Modify: `LayerBase.Test/ScopeRuntimeFoundationTests.cs`

**Interfaces:**
- Consumes: `LayerHub.CreateLayers()`, `LayerRuntime.ScopeHost`, `ScopeRuntimeHost.Scopes`.
- Produces: regression contracts for mandatory MainScope creation and default Service placement.

- [ ] Add a test that builds a Runtime with an empty Layer and asserts `ScopeHost` exists with exactly one MainScope at ID 0.
- [ ] Add a test that registers an unannotated Service and asserts the same instance is present in MainScope.
- [ ] Add a test that combines an unannotated Service and an explicitly scoped Service and asserts correct partitioning.
- [ ] Confirm these tests fail against `b02d461` because `InitializeScopeHost()` returns when no explicitly scoped Service exists.

### Task 2: Make non-Module ScopeHost and MainScope mandatory

**Files:**
- Modify: `LayerBase/Application/LayerRuntime.cs`
- Modify: `LayerBase/Layer/Layer.cs`

**Interfaces:**
- Consumes: `ScopeRuntimePlanner.Build(IReadOnlyList<IService>)`.
- Produces: non-null `ScopeHost`; all resolved Services receive a `ScopeObjectBinding`.

- [ ] Change `InitializeScopeHost()` to collect every unique resolved Service.
- [ ] Remove the empty scoped-Service early return.
- [ ] Always invoke generated factory or `ScopeRuntimeHost.Create(ScopeRuntimePlanner.Build(...))`.
- [ ] Change Layer lifecycle collection to skip every Service with `ScopeObjectBinding`, preventing duplicate MainScope lifecycle execution.
- [ ] Run the new ScopeRuntimeFoundation tests and existing Scope lifecycle tests.

### Task 3: Add Module MainScope regression tests

**Files:**
- Modify: `LayerBase.Test/ModuleRuntimeBuilderTests.cs`
- Modify: `LayerBase.Test/ScopeRuntimeFoundationTests.cs`

**Interfaces:**
- Consumes: `ModuleRuntimeBuilder.Build`, `ScopeCompositionBuilder.Build`, `LayerRuntime.TryBuildFromInstalledModules`.
- Produces: built-in MainScope ID and Module build behavior without custom Scope definitions.

- [ ] Add a builder test asserting MainScope maps to ID 0 without a contributed definition.
- [ ] Add a composition test asserting a MainScope-only plan contains one Scope.
- [ ] Add a Runtime test installing a Module with MainScope Services but no custom Scope definition.
- [ ] Confirm failures against current validation and `TryBuildFromInstalledModules()` early return.

### Task 4: Reserve MainScope in Module metadata

**Files:**
- Modify: `LayerBase/Modules/ModuleRuntimeBuilder.cs`
- Modify: `LayerBase/Application/LayerRuntime.cs`

**Interfaces:**
- Produces: `catalog.ScopeIds[typeof(MainScope).TypeHandle] == 0`.

- [ ] Seed Scope ID allocation with built-in MainScope ID 0.
- [ ] Treat MainScope as valid during Service validation without requiring a Module definition.
- [ ] Keep custom Scope allocation stable from ID 1.
- [ ] Remove the `ScopeDefinitions.Count == 0` fallback in `TryBuildFromInstalledModules()`.
- [ ] Run ModuleRuntimeBuilder and Scope composition tests.

### Task 5: Route Layer default events through MainScope

**Files:**
- Modify: `LayerBase/Application/LayerRuntime.cs`
- Modify: `LayerBase/Layer/Layer.cs`
- Modify: `LayerBase.Test/ScopeRuntimeFoundationTests.cs`

**Interfaces:**
- Produces: `LayerRuntime.MainScope`; Layer Send/Post/Subscribe defaults use MainScope event resources.

- [ ] Add a `MainScope` accessor that resolves Scope ID 0 and throws if the Runtime is not built.
- [ ] Add failing tests proving a Layer subscription receives a Layer Send through MainScope and not Runtime EventCenter.
- [ ] Route Layer subscription center, Send, Post and Delay Publisher selection to MainScope.
- [ ] Keep Scope-bound Service/Context selection unchanged.
- [ ] Run event, delay, lifecycle and full test suites.

### Task 6: Verify and publish

**Files:**
- Review all changed files.

- [ ] Build Release on .NET 8 and .NET 9.
- [ ] Run the full NUnit suite.
- [ ] Inspect the branch diff for unrelated changes.
- [ ] Open a pull request against `faster` with the matching analysis and known compatibility boundary.
