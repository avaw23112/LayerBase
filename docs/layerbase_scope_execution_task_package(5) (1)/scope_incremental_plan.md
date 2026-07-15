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
- Cross-assembly `[OwnerLayer]` services emit immutable `ServiceContribution` metadata with owner layer, owner scope, service contract, implementation type, and lifetime.
- Generated modules implement `IAssemblyModule` and expose a static `AssemblyModuleManifest`.
- Generated code must not Push layers, assign `LayerIndex`, assign `ScopeId`, create scope runtimes, or instantiate service implementations.
- Later 05/06 slices can add generated Context, LocalCall, and Tool contribution attributes without changing the runtime composition contract.

## 05/06 Cross-assembly OwnerLayer fallback

`AssemblyModule` is the fallback registration owner when feature assemblies cannot emit partial code into AOT assemblies that contain Layer and Scope definitions.

Acceptance points:

- If an `[OwnerLayer]` service targets a Layer declared in the current assembly, the existing Layer partial path remains active.
- If an `[OwnerLayer]` service targets a Layer from another assembly, `LayerServiceGenerator` must not require the external Layer to be partial.
- Cross-assembly `[OwnerLayer]` services are emitted into an `[AssemblyModule]` manifest as `ServiceContribution` entries.
- A single module root is selected automatically; multiple module roots are compile-time ambiguous and must be split before fallback can proceed.
- `[Scope<TScope>]` assigns a custom AOT Scope; otherwise the fallback contribution uses `MainScope`.
- Missing module roots and ambiguous module roots are compile-time diagnostics, never silent drops.
- Cross-assembly `[OwnerService]` contexts emit `ContextContribution` metadata into the same `[AssemblyModule]` manifest and inherit owner layer/scope from their owner service.
- This 05/06 context slice only transfers `[OwnerService]` targets that implement `ILayerContext`; cross-assembly event handler fallback is reserved for the later EventHandlerContribution slice and is diagnosed instead of being emitted as a context.
- Cross-assembly CallHandler fallback emits `LocalCallContribution` metadata into the same `[AssemblyModule]` manifest; runtime invoker activation remains a later LocalCall runtime slice.
