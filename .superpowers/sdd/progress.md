# SDD Progress

## Task 0 + first ownership fixes

- Added Task 0 regression tests under LayerBase.Test for lifecycle concurrency, promise shutdown, projected actor ownership, resource generation, module isolation, and DI boundaries.
- Fixed `ReliableContinuationInbox` so enqueue/close/drain share one lock; successful enqueue cannot be lost after drain.
- Fixed `ScopeRuntime.Dispose`/`Stop` interaction so concurrent dispose waits for in-flight stop cleanup.
- Routed projected actor Disable/Release from shared ScopeRuntime worlds through `LayerRuntime.ActorLifecycleInbox` via an internal lifecycle sink.
- Adjusted projected actor ownership regression to use a real `LayerRuntime` owner-thread drain instead of a bare shared ActorWorld approximation.
- Verification:
  - Focused Task 0 tests: 10 passed, 0 failed (`task-0-focused-after-fix-2.log`).
  - Full Release suite: 497 passed, 0 failed (`full-after-task0-fixes.log`).
## Scope resource generated registration

- Added `ScopeResourceContributionRegistry` as the runtime registry for generated scope resource export/import contributions.
- Updated `ScopeResourceGenerator` so generated publisher/consumer partials trigger a generated `Register()` method, which registers contributions without Assembly scanning.
- Replaced `ScopeRuntime` reflection discovery (`Assembly.GetType`, `GetMethod`, `Invoke`) with `RuntimeHelpers.RunClassConstructor` plus `ScopeResourceContributionRegistry.CollectFor`.
- Added regression coverage that `ScopeRuntime` no longer contains the old reflective generated-resource discovery path.
- Verification:
  - Resource-focused tests: 11 passed, 0 failed (`scope-resource-registry-tests-2.log`).
## Scope host factory de-globalized

- Replaced generated `ScopeHostFactory.Register(...)` with per-runtime `IScopeHostFactoryRegistrar.CreateScopeHostFactory()` delegate retrieval.
- Updated `LayerRuntime` to use the generated delegate directly from layer instances and fall back to `ScopeRuntimeHost.Create(...)`.
- Removed `ScopeHostFactory.Reset()` from `LayerHub` and deleted the static `ScopeHostFactory` registry file.
- Added module isolation regression coverage to prevent the global scope host factory from returning.
- Verification:
  - Scope host/generator focused tests: 112 passed, 0 failed (`scope-host-factory-tests.log`).
- Full Release suite after scope host de-globalization: 499 passed, 0 failed (`full-after-scopehost-deglobalize.log`).
## Module dispatcher global registry removed

- Removed `LayerRuntime` reads from `ModuleDispatchRegistry`.
- Deleted the static `ModuleDispatchRegistry` file.
- Added module isolation regression coverage preventing `ModuleDispatchRegistry`/`TryGetCallDispatchers`/`TryGetEventDispatchers` from returning.
- Full Release suite after module dispatcher de-globalization: 500 passed, 0 failed (`full-after-module-dispatch-deglobalize.log`).
## Module catalog and module build fallback removed

- Removed generated `ModuleCatalogRegistry` registration and deleted the static registry file.
- Changed module install to require explicit module instances instead of implicit global catalog discovery.
- Updated the AssemblyModule sample to call `GeneratedModuleCatalog.Create()` and pass the result to `.Install(modules)`.
- Added module isolation coverage preventing `ModuleCatalogRegistry` from returning.
- Removed the `ModuleBuildException` fallback path so invalid installed modules fail the build instead of silently dropping into legacy construction.
- Verification:
  - Module catalog focused tests: 5 passed, 0 failed (`module-catalog-deglobalize-tests.log`).
  - Module isolation after no-fallback: 6 passed, 0 failed (`module-isolation-after-no-fallback.log`).
  - Full Release suite: 502 passed, 0 failed (`full-after-module-no-fallback.log`).
## Publish/From reflection binder removed

- Deleted `ScopeResourceBinder` and removed the `ScopeRuntime` fallback call into runtime field/property reflection.
- Changed `ScopeResourceGenerator` so generated publisher/consumer partial types register their own contributions from inside the partial type, which supports private nested resource owners.
- Kept assembly-level resource manifest attributes only for resource providers visible from assembly attributes.
- Marked nested resource binding tests as a partial outer type so they exercise the generated path rather than reflection fallback.
- Added structural coverage that `ScopeRuntime` does not call `ScopeResourceBinder` and the binder file is absent.
- Verification:
  - Resource/generator focused tests: 13 passed, 0 failed (`scope-resource-no-binder-tests-4.log`).
  - Full Release suite: 503 passed, 0 failed (`full-after-scope-resource-no-binder.log`).
## Legacy LayerRuntime business API public surface reduced

- Changed `LayerRuntime.EventCenter`, `ServiceProvider`, `GetService`, `Scheduler`, `Timer`, `EcsWorld`, `EcsQueryRegistry`, and `EcsScheduler` from public API to internal runtime plumbing.
- Changed legacy generic event business methods (`Send`, `Post`, `TryPost`, `MarkDirty`, `PostLatest`, `PostFromAnyThread`, `TryPostFromAnyThread`, `PostCoalesced`, `SchedulePost`) from public API to internal.
- Added reflection coverage that these LayerRuntime business resources and event APIs are no longer publicly exposed.
- Verification:
  - Public API structure test: 1 passed, 0 failed (`layer-runtime-public-events-green.log`).
  - Full Release suite: 504 passed, 0 failed (`full-after-layer-runtime-public-events-internal.log`).
  - Solution build: 0 errors (`solution-build-after-layer-runtime-public-events-internal.log`).
## Layer public business API and reflection fallbacks reduced

- Changed `Layer.RegisterService`, `GetService`, `Send`, `Post`, `TryPost`, `RecordSubscribedEvent`, and `RecordProducedEvent` from public API to internal.
- Removed `Layer` interface-event reflection binding (`GetInterfaces`/`GetGenericTypeDefinition`) and converted the owner-service event regression to generated `[Subscribe]`.
- Added reflection coverage that the Layer business methods are no longer publicly exposed and that Layer no longer binds interface handlers reflectively.
- Verification:
  - Layer public API focused tests: 8 passed, 0 failed (`layer-public-business-green.log`).
  - Layer generated subscription focused tests: 2 passed, 0 failed (`layer-no-interface-reflection.log`).
  - Full Release suite: 509 passed, 0 failed (`full-after-layer-no-interface-reflection.log`).
## Scope DI generated mount and interface fallback removal

- Replaced `ScopeServiceProvider.InjectMembers` reflection scanning with generated `IGeneratedScopeMount` implementations and `ScopeMountContext`.
- Extended `LayerServiceGenerator` so partial services and contexts with `[Mount]` assign fields/properties through generated code.
- Removed `ScopeRuntime` interface-event reflection fallback and kept scope event binding on generated `IAutoScopeSubscribe`.
- Added coverage for generated scope mounts, no Scope DI reflection member injection, and no Scope interface-handler reflection binding.
- Verification:
  - Scope generated mount focused tests: 4 passed, 0 failed (`scope-di-generated-mount-4.log`).
  - Scope generated subscription focused tests: 2 passed, 0 failed (`scope-generated-subscribe-events.log`).
  - Full Release suite: 508 passed, 0 failed (`full-after-scope-no-interface-reflection.log`).
  - Solution build: 0 errors (`solution-build-after-scope-di-and-interface-reflection.log`).
## Scope concurrency and runtime shutdown P0 fixes

- Added `ScopeRuntimeState.Faulted` and replaced `ScopeRuntime` enum-order lifecycle comparisons with explicit state predicates.
- Split `RequestStop()` from blocking `Stop()`, added owner execution depth, deferred cleanup claiming, and inline owner-thread checks so cleanup cannot run inside active Initialize/handler/pump call stacks or from a non-owner inline thread.
- Added `ScopeStoppedException` and rejected `SchedulePost` after stop request through a shared business-ingress guard.
- Changed `ScopePromise` so completed results remain registered until a continuation is queued or `GetResult()` consumes the value.
- Made `ActorLifecycleInbox` reliable for lifecycle commands with a fast lane plus overflow queue and explicit `ControlEnqueueResult`; lifecycle enqueue now only fails after close.
- Updated projected actor retirement so ECS actor refs are retained when lifecycle release/disable cannot enter the owner queue because it is closed.
- Changed `LayerBaseSynchronizationContext` and `MainThreadCompletionQueue` to reject post/send/enqueue after disposal instead of silently dropping work.
- Added regression coverage for deferred inline cleanup, non-owner inline stop rejection, handler-triggered `RequestStop`, post-stop schedule rejection, promise result-ready registration, actor lifecycle overflow, and synchronization context shutdown.
- Verification:
  - Lifecycle focused tests: 6 passed, 0 failed (`lifecycle-business-ingress-green.log`).
  - P0 focused tests: 15 passed, 0 failed (`p0-focused-after-actor-control-result.log`).
  - Full Release suite: 519 passed, 0 failed (`full-after-actor-control-result.log`).
  - Solution build: 0 errors (`solution-build-after-actor-control-result.log`).
## Generator generic owner restriction

- Added `LBG413` for `[Provide]`, `[From]`, and `[Mount]` declarations owned by generic types or types nested inside generic types.
- Shared the generic-owner diagnostic helper across resource, service, and shared-field generator paths.
- Verification:
  - Generic-owner focused tests: 6 passed, 0 failed (`generic-owner-lbg413-green.log`).
  - Full Release suite: 522 passed, 0 failed (`full-after-lbg413.log`).
  - Solution build: 0 errors (`solution-build-after-lbg413.log`).
## Resource plan, mount slots, and runtime kernel

- Removed the process-wide `ScopeResourceContributionRegistry` and the resource `RunClassConstructor` path.
- Added resource exports/imports to `ModuleManifest` and `ModuleRuntimeCatalog`; `ScopeCompositionBuilder` now builds per-scope `ScopeResourcePlan` arrays.
- Changed `ScopeResourceRegistry` to bind by precomputed object/export/import slots instead of provider/type dictionaries on the binding path.
- Changed `ScopeResourceGenerator` to emit instance metadata interfaces for manual scope construction without static registration side effects.
- Changed mount generation from `context.Get<T>()` to `context.GetAt<T>(localDependencyId)` plus generated dependency metadata; removed `ScopeRuntime.GetMountedObject<T>`.
- Added `RuntimeKernel` ownership for runtime-global ActorWorld, exception hub, tools, and ScopeHost.
- Verification:
  - Resource focused tests: 63 passed, 0 failed (`resource-plan-first-green.log`).
  - Module resource manifest tests: 13 passed, 0 failed (`module-resource-manifest-green.log`).
  - Mount focused tests: 11 passed, 0 failed (`mount-slot-green.log`).
  - Full Release suite: 523 passed, 0 failed (`full-after-resource-mount-kernel.log`).
  - Solution build: 0 errors (`solution-build-after-resource-mount-kernel.log`).
