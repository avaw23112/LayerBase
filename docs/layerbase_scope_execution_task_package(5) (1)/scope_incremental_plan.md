# Scope Migration Incremental Plan

## ActorUpdateAttribute follow-up

`ActorUpdateAttribute` is an actor lifecycle entry point and should be closed during the 21/22 ActorWorld scope migration work, not during the 05/06 composition model pass.

Acceptance points:

- `ActorUpdateAttribute` metadata remains owned by actor metadata (`ActorTypeMeta`) and does not become a Layer/Service lifecycle hook.
- `ActorLifecycleScheduler` is driven only from the owning `ScopeRuntime` actor pump.
- MainScope ActorWorld execution stays on the MainScope owner thread until custom Scope ActorWorld support is explicitly introduced.
- Worker/custom scope migration must verify that no actor lifecycle path reaches `LayerRuntime` global pump resources directly.

## 05/06 AssemblyModule source generator slice

The first source-generator slice is limited to static composition metadata. It makes generated modules participate in the existing `AddAssemblyModule -> AssemblyModuleComposer -> RuntimeCompositionPlan` chain without creating runtime ownership.

Acceptance points:

- `[AssemblyModule]` marks an explicit partial module type; the generator does not scan referenced assemblies.
- `[ModuleService]` emits immutable `ServiceContribution` metadata with owner layer, owner scope, service contract, implementation type, and lifetime.
- Generated modules implement `IAssemblyModule` and expose a static `AssemblyModuleManifest`.
- Generated code must not Push layers, assign `LayerIndex`, assign `ScopeId`, create scope runtimes, or instantiate service implementations.
- Later 05/06 slices can add generated Context, LocalCall, and Tool contribution attributes without changing the runtime composition contract.
