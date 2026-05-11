# LayerBase Projection Fix — Task Plan

## TL;DR

> **Quick Summary:** Add Query0 (0-component query), EntityCreateFlow chain API, and convert placeholder T4 templates into real code generators. Keep existing Multi* files for multi-event support.
>
> **Deliverables:**
> - `EntityCreateFlow.tt` + `.g.cs` (0..8 components)
> - `EntityCreateWorldExtensions.tt` + `.g.cs`
> - Updated `Helpers.ttinclude` with batch helper functions
> - Real `ProjectionWorldExtensions.tt` (Query() + Query<T0..T7>())
> - Real `ProjectionQueryFlow.tt` (c=0..8 single-event flows)
> - Real `ProjectionExecutor.tt` (c=0..8 single-event executors)
> - Add c=0 multi-event support to `ProjectionMultiFlows.tt` and `ProjectionMultiExecutors.tt`
> - All `.g.cs` files regenerated from templates
>
> **Estimated Effort:** Medium
> **Parallel Execution:** YES - 3 waves
> **Critical Path:** Helpers.ttinclude → Templates → .g.cs regeneration → Build verification

---

## Context

### Original Request
根据 `layerbase-projection-fix-plan.md` 文档完成 LayerBase Projection 系统修复。

### Interview Summary
**Key Discussions:**
- Codebase exploration revealed existing architecture: single-event flows in `ProjectionQueryFlow.g.cs`/`ProjectionExecutor.g.cs`, multi-event flows in separate `ProjectionMultiFlows.g.cs`/`ProjectionMultiExecutors.g.cs`
- User decision: **Keep existing Multi* files as-is**, only add missing pieces
- T4 templates for QueryFlow, Executor, WorldExtensions are currently just placeholder comments

**Research Findings:**
- `Helpers.ttinclude` has: `CG()`, `EG()`, `AG()`, `PredP()`, `FEP()`, `FEC()` — needs additional batch helpers
- `ProjectionDelegates.tt` is the working reference for T4 generation patterns
- `World.Create()` API uses `in` parameters for components
- `ProjectedActorWorldExtensions.WithProjectedActor<TActor>()` already exists
- `ProjectionBatchBuffer<TEvent>` is the batch collection mechanism
- Existing Multi* templates generate for c=1..8, e=2..10 — need to extend to c=0

### Self-Review Applied
- Gap: Need `ProjectionPredicate` (non-generic) for c=0 case — confirmed in plan section 13.2
- Gap: `ProjectionExecutor0` must NOT read component columns (no `chunk.GetFirst<T0>()`) — confirmed in plan section 13.3
- Gap: Multi-event batch must use `try/finally Dispose` pattern — confirmed in plan section 13.4

---

## Work Objectives

### Core Objective
Add Query0, EntityCreateFlow, and make placeholder T4 templates into real generators while preserving existing Multi* files.

### Concrete Deliverables
- `LayerBase/ECS/Projection/Templates/EntityCreateFlow.tt`
- `LayerBase/ECS/Projection/Templates/EntityCreateWorldExtensions.tt`
- `LayerBase/ECS/Projection/Create/EntityCreateFlow.g.cs`
- `LayerBase/ECS/Projection/Create/EntityCreateWorldExtensions.g.cs`
- `LayerBase/ECS/Projection/Templates/Helpers.ttinclude` (updated)
- `LayerBase/ECS/Projection/Templates/ProjectionWorldExtensions.tt` (rewritten)
- `LayerBase/ECS/Projection/Templates/ProjectionQueryFlow.tt` (rewritten)
- `LayerBase/ECS/Projection/Templates/ProjectionExecutor.tt` (rewritten)
- `LayerBase/ECS/Projection/Templates/ProjectionMultiFlows.tt` (extended for c=0)
- `LayerBase/ECS/Projection/Templates/ProjectionMultiExecutors.tt` (extended for c=0)
- `LayerBase/ECS/Projection/Flow/ProjectionWorldExtensions.g.cs` (regenerated)
- `LayerBase/ECS/Projection/Flow/ProjectionQueryFlow.g.cs` (regenerated)
- `LayerBase/ECS/Projection/Flow/ProjectionExecutor.g.cs` (regenerated)
- `LayerBase/ECS/Projection/Flow/ProjectionMultiFlows.g.cs` (regenerated with c=0)
- `LayerBase/ECS/Projection/Flow/ProjectionMultiExecutors.g.cs` (regenerated with c=0)

### Definition of Done
- [ ] `world.CreateEntity(c0, c1).WithProjectedActor<MyActor>().Entity` compiles and chains correctly
- [ ] `world.Query().Bring<E0>().ForEach(...).Batch().Post()` compiles (0-component query)
- [ ] `world.Query<C0, C1>().Bring<E0, E1, E2>().ForEach(...).Batch().Post()` compiles (multi-event via Multi*)
- [ ] All `.g.cs` files are regenerated from `.tt` templates
- [ ] `dotnet build LayerBase.sln -c Debug` passes

### Must Have
- Query0 (0-component query with `ProjectionPredicate` non-generic delegate)
- Single-event Bring<TEvent>() on all QueryFlow types (c=0..8)
- Multi-event Bring on c=0 via Multi* files (extend ProjectionMultiFlows.tt/ProjectionMultiExecutors.tt for c=0)
- EntityCreateFlow chain API: `CreateEntity(...).WithProjectedActor<TActor>()`
- T4 templates that can regenerate ALL .g.cs files from scratch

### Must NOT Have (Guardrails)
- Do NOT change `ProjectedActorTypeRegistry` to global static — keep runtime-local
- Do NOT make `Post()` consume `RuntimeFrameBudget`
- Do NOT make `ForEach` return bool — `Where` handles filtering
- Do NOT use `chunk.GetFirst<T0>()` in `ProjectionExecutor0` (0-component)
- Do NOT use empty generic `ProjectionPredicate<>` for c=0 — use non-generic `ProjectionPredicate`
- Do NOT use `using` nesting for multi-event batch disposal — use `try/finally`
- Do NOT modify existing `ProjectionMultiFlows.g.cs`/`ProjectionMultiExecutors.g.cs` content (only extend templates for c=0)

---

## Verification Strategy

> **ZERO HUMAN INTERVENTION** — ALL verification is agent-executed.

### Test Decision
- **Infrastructure exists:** YES (NUnit in `LayerBase.Test`)
- **Automated tests:** Tests-after (existing test patterns, add new if needed)
- **Framework:** NUnit

### QA Policy
Every task MUST include agent-executed QA scenarios.
Evidence saved to `.sisyphus/evidence/task-{N}-{scenario-slug}.{ext}`.

- **Build:** `dotnet build LayerBase.sln -c Debug` — automated via Bash
- **T4 Regeneration:** Delete .g.cs, run T4, verify regenerated files compile — automated via Bash

---

## Execution Strategy

### Parallel Execution Waves

```
Wave 1 (Start Immediately — foundation):
├── Task 1: Update Helpers.ttinclude with batch helper functions [quick]
├── Task 2: Create EntityCreateFlow.tt + .g.cs [quick]
└── Task 3: Create EntityCreateWorldExtensions.tt + .g.cs [quick]

Wave 2 (After Wave 1 — core templates, parallelizable):
├── Task 4: Rewrite ProjectionWorldExtensions.tt (real generation) [unspecified-high]
├── Task 5: Rewrite ProjectionQueryFlow.tt (real generation, c=0..8 single-event) [deep]
├── Task 6: Rewrite ProjectionExecutor.tt (real generation, c=0..8 single-event) [deep]
├── Task 7: Extend ProjectionMultiFlows.tt for c=0 [quick]
└── Task 8: Extend ProjectionMultiExecutors.tt for c=0 [quick]

Wave 3 (After Wave 2 — regeneration + verification):
├── Task 9: Regenerate all .g.cs files [quick]
└── Task 10: Build verification + API smoke test [unspecified-high]

Wave FINAL (After ALL tasks — independent review):
├── Task F1: Plan compliance audit (oracle)
├── Task F2: Code quality review (unspecified-high)
└── Task F3: Build + regeneration verification (unspecified-high)
→ Present results → Get user okay
```

### Dependency Matrix

| Task | Depends On | Blocks |
|------|------------|--------|
| 1    | —          | 4,5,6,7,8 |
| 2    | —          | 9,10   |
| 3    | —          | 9,10   |
| 4    | 1          | 9,10   |
| 5    | 1          | 9,10   |
| 6    | 1          | 9,10   |
| 7    | 1          | 9,10   |
| 8    | 1          | 9,10   |
| 9    | 2,3,4,5,6,7,8 | 10 |
| 10   | 9          | F1-F3  |

### Agent Dispatch Summary

- **Wave 1:** 3 tasks — T1 → `quick`, T2 → `quick`, T3 → `quick`
- **Wave 2:** 5 tasks — T4 → `unspecified-high`, T5 → `deep`, T6 → `deep`, T7 → `quick`, T8 → `quick`
- **Wave 3:** 2 tasks — T9 → `quick`, T10 → `unspecified-high`
- **FINAL:** 3 tasks — F1 → `oracle`, F2 → `unspecified-high`, F3 → `unspecified-high`

---

## TODOs

- [ ] 1. Update Helpers.ttinclude with batch helper functions

  **What to do:**
  - Add the following helper functions to `Helpers.ttinclude` (after existing functions):
    - `EventName(int eventCount)` — returns "ProjectionForEach" for 1, "ProjectionForEach{N}" for N>1
    - `WhereConstraints(int eventCount)` — generates `where TEvent0 : struct` etc.
    - `FirstComponentRefs(int componentCount)` — generates `ref T0 first0 = ref chunk.GetFirst<T0>();` etc.
    - `RowComponentRefs(int componentCount)` — generates `ref T0 c0 = ref Unsafe.Add(ref first0, row);` etc.
    - `PredicateArgs(int componentCount)` — generates `in entity, in c0, in c1` etc.
    - `ForEachArgs(int componentCount, int eventCount)` — generates `in entity, ref c0, ref e0` etc.
    - `EventDefaults(int eventCount)` — generates `TEvent0 e0 = default;` etc.
    - `BatchDeclarations(int eventCount)` — generates `ProjectionBatchBuffer<TEvent0> batch0 = ...Rent();` etc.
    - `BatchRefArgs(int eventCount)` — generates `ref batch0, ref batch1` etc.
    - `BatchMethodParams(int eventCount)` — generates `ref ProjectionBatchBuffer<TEvent0> batch0` etc.
    - `BatchPosts(int eventCount)` — generates `batch0.PostTo(actorWorld);` etc.
    - `BatchDisposes(int eventCount)` — generates `batch0.Dispose();` in reverse order
    - `BatchAdds(int eventCount)` — generates `batch0.Add(actorId, in e0);` etc.
  - Reference: Plan sections 11

  **Must NOT do:**
  - Do NOT modify existing helper functions (CG, EG, AG, PredP, FEP, FEC)
  - Do NOT change ComponentAmount or EventAmount constants

  **Recommended Agent Profile:**
  - **Category:** `quick`
    - Reason: Single file edit, adding helper functions following existing patterns
  - **Skills:** []
  - **Skills Evaluated but Omitted:**
    - None relevant

  **Parallelization:**
  - **Can Run In Parallel:** YES
  - **Parallel Group:** Wave 1 (with Tasks 2, 3)
  - **Blocks:** Tasks 4, 5, 6, 7, 8
  - **Blocked By:** None

  **References:**

  **Pattern References:**
  - `LayerBase/ECS/Projection/Templates/Helpers.ttinclude` — existing helper functions (CG, EG, AG, PredP, FEP, FEC), add new functions after line 55
  - `LayerBase/ECS/Projection/Templates/ProjectionDelegates.tt` — working T4 template showing loop patterns

  **API/Type References:**
  - `LayerBase/ECS/Projection/Flow/ProjectionBatchBuffer.cs` — ProjectionBatchBuffer<TEvent> API (Rent, Add, PostTo, Dispose)

  **WHY Each Reference Matters:**
  - `Helpers.ttinclude`: The file to modify — understand existing function signatures and style
  - `ProjectionDelegates.tt`: Reference for T4 loop syntax and code generation patterns
  - `ProjectionBatchBuffer.cs`: The batch API that the generated code will call

  **Acceptance Criteria:**

  **QA Scenarios (MANDATORY):**

  ```
  Scenario: Verify Helpers.ttinclude has all new functions
    Tool: Bash (grep)
    Preconditions: Task 1 completed
    Steps:
      1. grep -c "string EventName" Helpers.ttinclude → expect >= 1
      2. grep -c "string BatchDeclarations" Helpers.ttinclude → expect >= 1
      3. grep -c "string BatchDisposes" Helpers.ttinclude → expect >= 1
    Expected Result: All new helper functions present
    Failure Indicators: Any grep returns 0
    Evidence: .sisyphus/evidence/task-1-helpers-check.txt

  Scenario: Verify existing helpers untouched
    Tool: Bash (diff)
    Preconditions: Git repo clean before task
    Steps:
      1. git diff Helpers.ttinclude — check only additions, no modifications to existing lines
    Expected Result: Only new lines added after line 55
    Failure Indicators: Modifications to lines 1-55
    Evidence: .sisyphus/evidence/task-1-helpers-diff.txt
  ```

  **Commit:** YES (groups with Wave 1)
  - Message: `feat(projection): add batch helper functions to Helpers.ttinclude`
  - Files: `LayerBase/ECS/Projection/Templates/Helpers.ttinclude`

---

- [ ] 2. Create EntityCreateFlow.tt + .g.cs

  **What to do:**
  - Create `LayerBase/ECS/Projection/Templates/EntityCreateFlow.tt` — T4 template generating EntityCreateFlow0..8
  - Generate `LayerBase/ECS/Projection/Create/EntityCreateFlow.g.cs` from the template
  - EntityCreateFlow0 (0 components): holds `World` + `Entity`, exposes `.Entity` property and `.WithProjectedActor<TActor>()` method
  - EntityCreateFlowN<T0..TN> (1..8 components): same shape, generic on component types
  - `WithProjectedActor<TActor>()` calls `world.WithProjectedActor<TActor>(entity, keepAliveSeconds, releasePolicy)` and returns `this`
  - Reference: Plan sections 3.1–3.4

  **Must NOT do:**
  - Do NOT save component values in the Flow struct — components are already in Arch chunks
  - Do NOT create the Actor immediately — only write ProjectedActorMeta

  **Recommended Agent Profile:**
  - **Category:** `quick`
    - Reason: Single template + generated file, following existing T4 patterns
  - **Skills:** []
  - **Skills Evaluated but Omitted:**
    - None relevant

  **Parallelization:**
  - **Can Run In Parallel:** YES
  - **Parallel Group:** Wave 1 (with Tasks 1, 3)
  - **Blocks:** Tasks 9, 10
  - **Blocked By:** None

  **References:**

  **Pattern References:**
  - `LayerBase/ECS/Projection/Templates/ProjectionDelegates.tt` — T4 template structure (template directive, include, namespace, loop)
  - `LayerBase/ECS/Projection/Templates/Helpers.ttinclude` — CG() function for component generics

  **API/Type References:**
  - `LayerBase/ECS/Projection/ProjectedActorWorldExtensions.cs` — `WithProjectedActor<TActor>(this World, Entity, float, ProjectedActorReleasePolicy)` API
  - `LayerBase/ECS/Projection/ProjectedActorMeta.cs` — ProjectedActorReleasePolicy enum
  - `LayerBase/Actor/IPooledActor.cs` — TActor constraint: `class, IPooledActor, new()`

  **WHY Each Reference Matters:**
  - `ProjectionDelegates.tt`: The template to mirror for T4 syntax and structure
  - `ProjectedActorWorldExtensions.cs`: The API that `WithProjectedActor` will delegate to
  - `IPooledActor.cs`: The generic constraint on TActor

  **Acceptance Criteria:**

  **QA Scenarios (MANDATORY):**

  ```
  Scenario: EntityCreateFlow0 compiles and has correct members
    Tool: Bash (dotnet build + grep)
    Preconditions: .g.cs file created
    Steps:
      1. grep "public readonly struct EntityCreateFlow0" EntityCreateFlow.g.cs → expect match
      2. grep "public Entity Entity" EntityCreateFlow.g.cs → expect match
      3. grep "WithProjectedActor<TActor>" EntityCreateFlow.g.cs → expect match
      4. dotnet build LayerBase.sln -c Debug → expect success
    Expected Result: Flow0 has Entity property and WithProjectedActor method
    Failure Indicators: Missing members or build failure
    Evidence: .sisyphus/evidence/task-2-entity-flow.txt

  Scenario: EntityCreateFlow2 has generic component parameters
    Tool: Bash (grep)
    Preconditions: .g.cs file created
    Steps:
      1. grep "EntityCreateFlow2<T0, T1>" EntityCreateFlow.g.cs → expect match
    Expected Result: Flow2 exists with generic params
    Failure Indicators: No match
    Evidence: .sisyphus/evidence/task-2-flow2-generic.txt
  ```

  **Commit:** YES (groups with Wave 1)
  - Message: `feat(projection): add EntityCreateFlow chain API`
  - Files: `LayerBase/ECS/Projection/Templates/EntityCreateFlow.tt`, `LayerBase/ECS/Projection/Create/EntityCreateFlow.g.cs`

---

- [ ] 3. Create EntityCreateWorldExtensions.tt + .g.cs

  **What to do:**
  - Create `LayerBase/ECS/Projection/Templates/EntityCreateWorldExtensions.tt` — T4 template generating extension methods
  - Generate `LayerBase/ECS/Projection/Create/EntityCreateWorldExtensions.g.cs` from the template
  - `CreateEntity(this World world)` → returns `EntityCreateFlow0` (0 components, calls `world.Create()`)
  - `CreateEntity<T0, T1>(this World world, in T0 c0, in T1 c1)` → returns `EntityCreateFlow2<T0, T1>` (calls `world.Create(c0, c1)`)
  - Generate for 0..8 components
  - Reference: Plan section 3.5

  **Must NOT do:**
  - Do NOT use non-`in` parameter signatures if Arch's `World.Create` uses `in`

  **Recommended Agent Profile:**
  - **Category:** `quick`
    - Reason: Single template + generated file, straightforward extension method generation
  - **Skills:** []
  - **Skills Evaluated but Omitted:**
    - None relevant

  **Parallelization:**
  - **Can Run In Parallel:** YES
  - **Parallel Group:** Wave 1 (with Tasks 1, 2)
  - **Blocks:** Tasks 9, 10
  - **Blocked By:** None

  **References:**

  **Pattern References:**
  - `LayerBase/ECS/Projection/Templates/ProjectionDelegates.tt` — T4 template structure
  - `LayerBase/ECS/Projection/Templates/Helpers.ttinclude` — CG() for component generics

  **API/Type References:**
  - `LayerBase/ECS/Projection/ProjectedActorWorldExtensions.cs` — existing extension method pattern (static class, this World)
  - Arch.Core.World — `World.Create()` and `World.Create(in T0, in T1)` API signatures

  **WHY Each Reference Matters:**
  - `ProjectedActorWorldExtensions.cs`: Pattern for extension methods on World
  - Arch.Core.World: The API being extended — must match Create() signatures

  **Acceptance Criteria:**

  **QA Scenarios (MANDATORY):**

  ```
  Scenario: CreateEntity extension methods exist for 0 and 2 components
    Tool: Bash (grep)
    Preconditions: .g.cs file created
    Steps:
      1. grep "public static EntityCreateFlow0 CreateEntity" EntityCreateWorldExtensions.g.cs → expect match
      2. grep "public static EntityCreateFlow2<T0, T1> CreateEntity<T0, T1>" EntityCreateWorldExtensions.g.cs → expect match
      3. dotnet build LayerBase.sln -c Debug → expect success
    Expected Result: Both extension methods present and compile
    Failure Indicators: Missing methods or build failure
    Evidence: .sisyphus/evidence/task-3-extensions.txt
  ```

  **Commit:** YES (groups with Wave 1)
  - Message: `feat(projection): add EntityCreateWorldExtensions`
  - Files: `LayerBase/ECS/Projection/Templates/EntityCreateWorldExtensions.tt`, `LayerBase/ECS/Projection/Create/EntityCreateWorldExtensions.g.cs`

---

- [ ] 4. Rewrite ProjectionWorldExtensions.tt (real generation)

  **What to do:**
  - Replace placeholder `ProjectionWorldExtensions.tt` with real T4 generation logic
  - Generate `Query()` (0-component) → returns `ProjectionQueryFlow0`
  - Generate `Query<T0>()` through `Query<T0..T7>()` → returns `ProjectionQueryFlow1..8`
  - `Query()` uses empty `QueryDescription` (no `WithAll`)
  - `Query<T0>()` uses `description.WithAll<T0>()`
  - Regenerate `ProjectionWorldExtensions.g.cs`
  - Reference: Plan section 5

  **Must NOT do:**
  - Do NOT generate `Query<>` with empty generic brackets for c=0

  **Recommended Agent Profile:**
  - **Category:** `unspecified-high`
    - Reason: Template rewrite with careful T4 generation logic
  - **Skills:** []
  - **Skills Evaluated but Omitted:**
    - None relevant

  **Parallelization:**
  - **Can Run In Parallel:** YES
  - **Parallel Group:** Wave 2 (with Tasks 5, 6, 7, 8)
  - **Blocks:** Tasks 9, 10
  - **Blocked By:** Task 1

  **References:**

  **Pattern References:**
  - `LayerBase/ECS/Projection/Templates/ProjectionDelegates.tt` — working T4 template with loops
  - `LayerBase/ECS/Projection/Flow/ProjectionWorldExtensions.g.cs` — current hand-written code to match/replace

  **API/Type References:**
  - Arch.Core — `QueryDescription`, `World.Query(in QueryDescription)`, `WithAll<T>()`

  **WHY Each Reference Matters:**
  - `ProjectionDelegates.tt`: The T4 loop pattern to follow
  - Current `.g.cs`: The code shape to replicate (now generated)

  **Acceptance Criteria:**

  **QA Scenarios (MANDATORY):**

  ```
  Scenario: Query() 0-component entry exists
    Tool: Bash (grep)
    Preconditions: Template rewritten and .g.cs regenerated
    Steps:
      1. grep "public static ProjectionQueryFlow0 Query(" ProjectionWorldExtensions.g.cs → expect match
      2. grep "new QueryDescription()" ProjectionWorldExtensions.g.cs → expect match (for Query())
    Expected Result: Query() returns ProjectionQueryFlow0
    Failure Indicators: No match
    Evidence: .sisyphus/evidence/task-4-query0.txt

  Scenario: Query<T0>() through Query<T0..T7>() all exist
    Tool: Bash (grep)
    Preconditions: .g.cs regenerated
    Steps:
      1. grep -c "public static ProjectionQueryFlow" ProjectionWorldExtensions.g.cs → expect 9 (0..8)
    Expected Result: 9 Query overloads
    Failure Indicators: Count != 9
    Evidence: .sisyphus/evidence/task-4-query-count.txt
  ```

  **Commit:** YES (groups with Wave 2)
  - Message: `feat(projection): rewrite ProjectionWorldExtensions.tt with real generation`
  - Files: `LayerBase/ECS/Projection/Templates/ProjectionWorldExtensions.tt`, `LayerBase/ECS/Projection/Flow/ProjectionWorldExtensions.g.cs`

---

- [ ] 5. Rewrite ProjectionQueryFlow.tt (real generation, c=0..8)

  **What to do:**
  - Replace placeholder `ProjectionQueryFlow.tt` with real T4 generation logic
  - Generate `ProjectionQueryFlow0` (non-generic predicate, no component reads)
  - Generate `ProjectionQueryFlow1..8` (generic predicate, component reads)
  - Each QueryFlow has: `Where()`, `Bring<TEvent>()`, `TouchProjectedActor()`
  - `Bring<TEvent>()` returns `ProjectionBringFlow{N}<...>` which has `ForEach()` returning `ProjectionPostFlow{N}<...>`
  - `ProjectionPostFlow{N}` has `Batch()` and `Post()`
  - Regenerate `ProjectionQueryFlow.g.cs`
  - Reference: Plan sections 4, 6

  **Must NOT do:**
  - Do NOT generate `ProjectionPredicate<>` with empty generic for c=0 — use non-generic `ProjectionPredicate`
  - Do NOT generate multi-event Bring methods here (those stay in Multi* files)

  **Recommended Agent Profile:**
  - **Category:** `deep`
    - Reason: Complex T4 template with c=0 special case and multiple struct types per component count
  - **Skills:** []
  - **Skills Evaluated but Omitted:**
    - None relevant

  **Parallelization:**
  - **Can Run In Parallel:** YES
  - **Parallel Group:** Wave 2 (with Tasks 4, 6, 7, 8)
  - **Blocks:** Tasks 9, 10
  - **Blocked By:** Task 1

  **References:**

  **Pattern References:**
  - `LayerBase/ECS/Projection/Templates/ProjectionMultiFlows.tt` — working T4 template for flow generation (BringFlow + PostFlow)
  - `LayerBase/ECS/Projection/Flow/ProjectionQueryFlow.g.cs` — current hand-written code to match/replace
  - `LayerBase/ECS/Projection/Templates/ProjectionDelegates.tt` — T4 loop patterns

  **API/Type References:**
  - `LayerBase/ECS/Projection/Flow/ProjectionBatchBuffer.cs` — batch API
  - `LayerBase/ECS/Projection/ProjectedActorBinding.cs` — EnsureProjectedActor, TouchProjectedActor
  - `LayerBase/ECS/Projection/Chunk.Projection.cs` — FirstProjection()
  - `LayerBase/ECS/Projection/Flow/ProjectionMultiFlows.g.cs` — existing multi-event naming convention (e.g., `ProjectionBringFlow1_2e`)

  **WHY Each Reference Matters:**
  - `ProjectionMultiFlows.tt`: The template pattern for generating BringFlow + PostFlow structs
  - Current `.g.cs`: The code shape to replicate for single-event flows
  - `ProjectedActorBinding.cs`: The API called by executor Touch/Post methods

  **Acceptance Criteria:**

  **QA Scenarios (MANDATORY):**

  ```
  Scenario: ProjectionQueryFlow0 exists with non-generic predicate
    Tool: Bash (grep)
    Preconditions: Template rewritten and .g.cs regenerated
    Steps:
      1. grep "public readonly struct ProjectionQueryFlow0" ProjectionQueryFlow.g.cs → expect match
      2. grep "ProjectionPredicate?" ProjectionQueryFlow.g.cs → expect match (non-generic)
      3. grep "ProjectionBringFlow0<TEvent>" ProjectionQueryFlow.g.cs → expect match
    Expected Result: QueryFlow0 with non-generic predicate and Bring<TEvent>
    Failure Indicators: Missing types or generic predicate
    Evidence: .sisyphus/evidence/task-5-queryflow0.txt

  Scenario: ProjectionQueryFlow1..8 all exist
    Tool: Bash (grep)
    Preconditions: .g.cs regenerated
    Steps:
      1. grep -c "public readonly struct ProjectionQueryFlow" ProjectionQueryFlow.g.cs → expect 9 (0..8)
    Expected Result: 9 QueryFlow types
    Failure Indicators: Count != 9
    Evidence: .sisyphus/evidence/task-5-queryflow-count.txt

  Scenario: c=0 Executor has no component column reads
    Tool: Bash (grep)
    Preconditions: .g.cs regenerated
    Steps:
      1. Check ProjectionExecutor0 does NOT contain "chunk.GetFirst<T0>()"
      2. Check ProjectionExecutor0 DOES contain "chunk.FirstProjection()"
    Expected Result: No component reads, only projection meta reads
    Failure Indicators: Component reads present in Executor0
    Evidence: .sisyphus/evidence/task-5-executor0-no-comp.txt
  ```

  **Commit:** YES (groups with Wave 2)
  - Message: `feat(projection): rewrite ProjectionQueryFlow.tt with Query0 support`
  - Files: `LayerBase/ECS/Projection/Templates/ProjectionQueryFlow.tt`, `LayerBase/ECS/Projection/Flow/ProjectionQueryFlow.g.cs`

---

- [ ] 6. Rewrite ProjectionExecutor.tt (real generation, c=0..8)

  **What to do:**
  - Replace placeholder `ProjectionExecutor.tt` with real T4 generation logic
  - Generate `ProjectionExecutor0` (no component reads, only ProjectionMeta + Entity)
  - Generate `ProjectionExecutor1..8` (with component reads)
  - Each executor has: `Post<TEvent>()` and `Touch()` methods
  - `Post` uses `ProjectionBatchBuffer<TEvent>.Rent()`, collects via `CollectPostChunk`, then `batch.PostTo(actorWorld)`
  - `Touch` iterates chunks, calls `EnsureProjectedActor` or `TouchProjectedActor`
  - Use `try/finally` for batch disposal
  - Regenerate `ProjectionExecutor.g.cs`
  - Reference: Plan section 9

  **Must NOT do:**
  - Do NOT use `chunk.GetFirst<T0>()` in ProjectionExecutor0
  - Do NOT use `using` for batch disposal — use `try/finally`

  **Recommended Agent Profile:**
  - **Category:** `deep`
    - Reason: Complex T4 template with c=0 special case (no component reads)
  - **Skills:** []
  - **Skills Evaluated but Omitted:**
    - None relevant

  **Parallelization:**
  - **Can Run In Parallel:** YES
  - **Parallel Group:** Wave 2 (with Tasks 4, 5, 7, 8)
  - **Blocks:** Tasks 9, 10
  - **Blocked By:** Task 1

  **References:**

  **Pattern References:**
  - `LayerBase/ECS/Projection/Templates/ProjectionMultiExecutors.tt` — working T4 template for executor generation
  - `LayerBase/ECS/Projection/Flow/ProjectionExecutor.g.cs` — current hand-written code to match/replace

  **API/Type References:**
  - `LayerBase/ECS/Projection/Flow/ProjectionBatchBuffer.cs` — Rent, Add, PostTo, Dispose
  - `LayerBase/ECS/Projection/ProjectedActorBinding.cs` — EnsureProjectedActor, TouchProjectedActor
  - `LayerBase/ECS/Projection/Chunk.Projection.cs` — FirstProjection(), ProjectionAt()
  - `LayerBase/Actor/ActorWorld.cs` — PostTo API

  **WHY Each Reference Matters:**
  - `ProjectionMultiExecutors.tt`: The template pattern for executor generation
  - Current `.g.cs`: The code shape to replicate
  - `ProjectedActorBinding.cs`: The core actor lifecycle API

  **Acceptance Criteria:**

  **QA Scenarios (MANDATORY):**

  ```
  Scenario: ProjectionExecutor0 has no component reads
    Tool: Bash (grep)
    Preconditions: Template rewritten and .g.cs regenerated
    Steps:
      1. Find ProjectionExecutor0 section in .g.cs
      2. Verify NO "chunk.GetFirst<" in Executor0 section
      3. Verify YES "chunk.FirstProjection()" in Executor0 section
    Expected Result: Executor0 only reads ProjectionMeta and Entity
    Failure Indicators: Component reads present
    Evidence: .sisyphus/evidence/task-6-executor0.txt

  Scenario: All executors use try/finally for batch disposal
    Tool: Bash (grep)
    Preconditions: .g.cs regenerated
    Steps:
      1. grep -c "finally" ProjectionExecutor.g.cs → expect >= 9 (one per executor)
      2. grep -c "\.Dispose()" ProjectionExecutor.g.cs → expect >= 9
    Expected Result: All executors have finally+Dispose
    Failure Indicators: Missing finally blocks
    Evidence: .sisyphus/evidence/task-6-finally.txt
  ```

  **Commit:** YES (groups with Wave 2)
  - Message: `feat(projection): rewrite ProjectionExecutor.tt with Executor0 support`
  - Files: `LayerBase/ECS/Projection/Templates/ProjectionExecutor.tt`, `LayerBase/ECS/Projection/Flow/ProjectionExecutor.g.cs`

---

- [ ] 7. Extend ProjectionMultiFlows.tt for c=0

  **What to do:**
  - Modify `ProjectionMultiFlows.tt` to generate c=0 multi-event flows (currently starts at c=1)
  - Change loop from `for (var c = 1; ...)` to `for (var c = 0; ...)`
  - For c=0: use non-generic `ProjectionPredicate?` instead of `ProjectionPredicate<T0>?`
  - Generate `ProjectionBringFlow0_2e<TEvent0, TEvent1>` etc.
  - Regenerate `ProjectionMultiFlows.g.cs`
  - Reference: Plan section 6.2 (QueryFlow0 with multi-event Bring)

  **Must NOT do:**
  - Do NOT break existing c=1..8 generated code

  **Recommended Agent Profile:**
  - **Category:** `quick`
    - Reason: Small template modification (change loop start index)
  - **Skills:** []
  - **Skills Evaluated but Omitted:**
    - None relevant

  **Parallelization:**
  - **Can Run In Parallel:** YES
  - **Parallel Group:** Wave 2 (with Tasks 4, 5, 6, 8)
  - **Blocks:** Tasks 9, 10
  - **Blocked By:** Task 1

  **References:**

  **Pattern References:**
  - `LayerBase/ECS/Projection/Templates/ProjectionMultiFlows.tt` — the file to modify (line 9: `for (var c = 1; ...)`)

  **API/Type References:**
  - `LayerBase/ECS/Projection/Flow/ProjectionMultiFlows.g.cs` — existing generated output to verify against

  **WHY Each Reference Matters:**
  - `ProjectionMultiFlows.tt`: The exact template to modify

  **Acceptance Criteria:**

  **QA Scenarios (MANDATORY):**

  ```
  Scenario: c=0 multi-event flows generated
    Tool: Bash (grep)
    Preconditions: Template modified and .g.cs regenerated
    Steps:
      1. grep "ProjectionBringFlow0_2e" ProjectionMultiFlows.g.cs → expect match
      2. grep "ProjectionPostFlow0_2e" ProjectionMultiFlows.g.cs → expect match
    Expected Result: c=0 multi-event types exist
    Failure Indicators: No match
    Evidence: .sisyphus/evidence/task-7-multi-c0.txt

  Scenario: Existing c=1..8 code unchanged
    Tool: Bash (grep)
    Preconditions: .g.cs regenerated
    Steps:
      1. grep "ProjectionBringFlow1_2e" ProjectionMultiFlows.g.cs → expect match
      2. grep "ProjectionBringFlow8_2e" ProjectionMultiFlows.g.cs → expect match
    Expected Result: Existing types still present
    Failure Indicators: Missing existing types
    Evidence: .sisyphus/evidence/task-7-existing-intact.txt
  ```

  **Commit:** YES (groups with Wave 2)
  - Message: `feat(projection): extend ProjectionMultiFlows.tt for c=0`
  - Files: `LayerBase/ECS/Projection/Templates/ProjectionMultiFlows.tt`, `LayerBase/ECS/Projection/Flow/ProjectionMultiFlows.g.cs`

---

- [ ] 8. Extend ProjectionMultiExecutors.tt for c=0

  **What to do:**
  - Modify `ProjectionMultiExecutors.tt` to generate c=0 multi-event executors (currently starts at c=1)
  - Change loop from `for (var c = 1; ...)` to `for (var c = 0; ...)`
  - For c=0: no component column reads (no `chunk.GetFirst<T0>()`), only ProjectionMeta + Entity
  - Generate `ProjectionExecutor0_2E<TEvent0, TEvent1>` etc.
  - Regenerate `ProjectionMultiExecutors.g.cs`
  - Reference: Plan section 9.2

  **Must NOT do:**
  - Do NOT add component reads for c=0 case
  - Do NOT break existing c=1..8 generated code

  **Recommended Agent Profile:**
  - **Category:** `quick`
    - Reason: Small template modification (change loop start + add c=0 conditional)
  - **Skills:** []
  - **Skills Evaluated but Omitted:**
    - None relevant

  **Parallelization:**
  - **Can Run In Parallel:** YES
  - **Parallel Group:** Wave 2 (with Tasks 4, 5, 6, 7)
  - **Blocks:** Tasks 9, 10
  - **Blocked By:** Task 1

  **References:**

  **Pattern References:**
  - `LayerBase/ECS/Projection/Templates/ProjectionMultiExecutors.tt` — the file to modify

  **API/Type References:**
  - `LayerBase/ECS/Projection/Flow/ProjectionMultiExecutors.g.cs` — existing generated output

  **WHY Each Reference Matters:**
  - `ProjectionMultiExecutors.tt`: The exact template to modify

  **Acceptance Criteria:**

  **QA Scenarios (MANDATORY):**

  ```
  Scenario: c=0 multi-event executors generated
    Tool: Bash (grep)
    Preconditions: Template modified and .g.cs regenerated
    Steps:
      1. grep "ProjectionExecutor0_2E" ProjectionMultiExecutors.g.cs → expect match
      2. Verify Executor0_2E section does NOT contain "chunk.GetFirst<"
    Expected Result: c=0 executor exists without component reads
    Failure Indicators: Missing executor or component reads present
    Evidence: .sisyphus/evidence/task-8-exec-multi-c0.txt

  Scenario: Existing c=1..8 code unchanged
    Tool: Bash (grep)
    Preconditions: .g.cs regenerated
    Steps:
      1. grep "ProjectionExecutor1_2E" ProjectionMultiExecutors.g.cs → expect match
    Expected Result: Existing types still present
    Failure Indicators: Missing existing types
    Evidence: .sisyphus/evidence/task-8-existing-intact.txt
  ```

  **Commit:** YES (groups with Wave 2)
  - Message: `feat(projection): extend ProjectionMultiExecutors.tt for c=0`
  - Files: `LayerBase/ECS/Projection/Templates/ProjectionMultiExecutors.tt`, `LayerBase/ECS/Projection/Flow/ProjectionMultiExecutors.g.cs`

---

- [ ] 9. Regenerate all .g.cs files

  **What to do:**
  - Delete all .g.cs files in `LayerBase/ECS/Projection/Flow/`
  - Run T4 templates to regenerate all .g.cs files
  - Verify all files regenerated correctly
  - This is the integration point — all templates must work together

  **Must NOT do:**
  - Do NOT modify any .tt template files in this task

  **Recommended Agent Profile:**
  - **Category:** `quick`
    - Reason: Mechanical regeneration step
  - **Skills:** []
  - **Skills Evaluated but Omitted:**
    - None relevant

  **Parallelization:**
  - **Can Run In Parallel:** NO
  - **Parallel Group:** Sequential (Wave 3)
  - **Blocks:** Task 10
  - **Blocked By:** Tasks 2, 3, 4, 5, 6, 7, 8

  **References:**

  **Pattern References:**
  - `LayerBase/ECS/Projection/Templates/` — all .tt files to run

  **WHY Each Reference Matters:**
  - All templates must be run to regenerate the .g.cs files

  **Acceptance Criteria:**

  **QA Scenarios (MANDATORY):**

  ```
  Scenario: All .g.cs files regenerated
    Tool: Bash
    Preconditions: All templates completed
    Steps:
      1. Delete all .g.cs files in Flow/ and Create/
      2. Run T4 for each .tt file
      3. Verify all .g.cs files exist
      4. dotnet build LayerBase.sln -c Debug → expect success
    Expected Result: All files regenerated and build passes
    Failure Indicators: Missing files or build failure
    Evidence: .sisyphus/evidence/task-9-regen.txt
  ```

  **Commit:** YES
  - Message: `feat(projection): regenerate all .g.cs from T4 templates`
  - Files: All .g.cs files

---

- [ ] 10. Build verification + API smoke test

  **What to do:**
  - Run full build: `dotnet build LayerBase.sln -c Debug`
  - Run tests: `dotnet test LayerBase.Test/LayerBase.Test.csproj -c Debug`
  - Verify key APIs compile (write minimal test code if needed):
    - `world.CreateEntity(c0, c1).WithProjectedActor<MyActor>().Entity`
    - `world.Query().Bring<E>().ForEach(...).Batch().Post()`
    - `world.Query<C0, C1>().Bring<E0, E1>().ForEach(...).Batch().Post()` (via Multi*)

  **Must NOT do:**
  - Do NOT modify production code in this task (only verification)

  **Recommended Agent Profile:**
  - **Category:** `unspecified-high`
    - Reason: Comprehensive verification with potential test writing
  - **Skills:** []
  - **Skills Evaluated but Omitted:**
    - None relevant

  **Parallelization:**
  - **Can Run In Parallel:** NO
  - **Parallel Group:** Sequential (Wave 3, after Task 9)
  - **Blocks:** F1-F3
  - **Blocked By:** Task 9

  **References:**

  **Pattern References:**
  - `LayerBase.Test/` — existing test patterns
  - `LayerBase.Usages/` — usage examples

  **WHY Each Reference Matters:**
  - Existing tests show the verification pattern to follow

  **Acceptance Criteria:**

  **QA Scenarios (MANDATORY):**

  ```
  Scenario: Full build passes
    Tool: Bash
    Preconditions: All .g.cs files regenerated
    Steps:
      1. dotnet build LayerBase.sln -c Debug → expect "Build succeeded"
    Expected Result: Clean build
    Failure Indicators: Any error
    Evidence: .sisyphus/evidence/task-10-build.txt

  Scenario: Existing tests pass
    Tool: Bash
    Preconditions: Build succeeded
    Steps:
      1. dotnet test LayerBase.Test/LayerBase.Test.csproj -c Debug → expect all pass
    Expected Result: All tests pass
    Failure Indicators: Any test failure
    Evidence: .sisyphus/evidence/task-10-tests.txt
  ```

  **Commit:** NO (verification only)

---

## Final Verification Wave

- [ ] F1. **Plan Compliance Audit** — `oracle`
  Read the plan end-to-end. For each "Must Have": verify implementation exists (read file, run build). For each "Must NOT Have": search codebase for forbidden patterns — reject with file:line if found. Check evidence files exist in .sisyphus/evidence/. Compare deliverables against plan.
  Output: `Must Have [N/N] | Must NOT Have [N/N] | Tasks [N/N] | VERDICT: APPROVE/REJECT`

- [ ] F2. **Code Quality Review** — `unspecified-high`
  Run T4 templates, verify generated code compiles. Check for: empty catches, unused imports, commented-out code. Verify naming conventions match existing codebase (PascalCase types, camelCase locals). Check AI slop: excessive comments, over-abstraction, generic names.
  Output: `Build [PASS/FAIL] | Generated Code [N clean/N issues] | VERDICT`

- [ ] F3. **Build + Regeneration Verification** — `unspecified-high`
  Delete all .g.cs files. Run T4 to regenerate. Run `dotnet build LayerBase.sln -c Debug`. Verify all generated files are byte-identical or semantically equivalent. Save build output to `.sisyphus/evidence/final-build.txt`.
  Output: `Regeneration [PASS/FAIL] | Build [PASS/FAIL] | VERDICT`

---

## Commit Strategy

- **After Wave 1:** `feat(projection): add EntityCreateFlow chain API and Helpers.ttinclude batch functions`
- **After Wave 2:** `feat(projection): rewrite T4 templates for Query0 and multi-event support`
- **After Wave 3:** `feat(projection): regenerate all .g.cs from templates`

---

## Success Criteria

### Verification Commands
```bash
dotnet build LayerBase.sln -c Debug  # Expected: Build succeeded
dotnet test LayerBase.Test/LayerBase.Test.csproj -c Debug  # Expected: All tests pass
```

### Final Checklist
- [ ] All "Must Have" present
- [ ] All "Must NOT Have" absent
- [ ] All .g.cs files regenerable from .tt templates
- [ ] Query0 API works: `world.Query().Bring<E>()...`
- [ ] Multi-event API works: `world.Query<C0,C1>().Bring<E0,E1>()...` (via Multi*)
- [ ] EntityCreateFlow API works: `world.CreateEntity(c0,c1).WithProjectedActor<A>()`
