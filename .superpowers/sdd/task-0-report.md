# Task 0 Report - Scope Ownership/Shutdown Regression Tests

## Files changed

- `LayerBase.Test/ScopeLifecycleConcurrencyTests.cs`
- `LayerBase.Test/ScopePromiseShutdownTests.cs`
- `LayerBase.Test/ProjectedActorOwnershipTests.cs`
- `LayerBase.Test/ScopeResourceGenerationTests.cs`
- `LayerBase.Test/ModuleRuntimeIsolationTests.cs`
- `LayerBase.Test/ScopeDiGenerationTests.cs`
- `.superpowers/sdd/task-0-report.md`

No production code or generator code was modified.

## Tests added

- `ScopeLifecycleConcurrencyTests.Start_and_stop_must_not_dispose_service_before_start_returns`
- `ScopeLifecycleConcurrencyTests.Dispose_must_not_return_before_concurrent_stop_cleanup_finishes`
- `ScopePromiseShutdownTests.Continuation_close_and_drain_must_not_leave_successful_enqueue_unexecuted`
- `ScopePromiseShutdownTests.Scope_stop_must_cancel_pending_promise_and_run_registered_continuation`
- `ProjectedActorOwnershipTests.Shared_projected_actor_release_must_run_on_owner_thread_not_scope_worker`
- `ScopeResourceGenerationTests.Generated_resource_imports_require_provider_in_same_scope`
- `ScopeResourceGenerationTests.Generated_resource_bindings_are_scope_local_and_clear_on_stop`
- `ModuleRuntimeIsolationTests.Module_catalog_rejects_service_when_scope_definition_module_is_not_installed`
- `ModuleRuntimeIsolationTests.Module_catalog_rejects_handler_when_message_targets_different_scope`
- `ScopeDiGenerationTests.Scope_planner_keeps_main_and_scoped_services_in_separate_runtime_boundaries`

## Public/internal API gaps

- Step 3 asked for a Barrier-controlled producer that pauses between `IsClosed` and `Enqueue`. `ReliableContinuationInbox` exposes `IsClosed`, `TryEnqueue`, `Close`, and `Drain`, but has no public/internal hook to pause inside the checked region of `TryEnqueue`. The added regression uses a tight multi-producer close/drain race against the internal inbox and consistently exposes successful enqueue with an unexecuted continuation.
- Step 4 asked to record LayerRuntime owner thread and scope worker thread. The compile-safe test uses a shared `ActorWorld` created on the test thread as the owner-thread stand-in and records the scope worker thread through a worker-scope service. The failure shows projected actor release currently occurs on the worker thread.

## Commands run

1. `dotnet test LayerBase.Test\LayerBase.Test.csproj -c Release --no-restore --filter "FullyQualifiedName~ScopeRuntimeFoundationTests|FullyQualifiedName~ScopeResourceBindingTests" --logger "console;verbosity=minimal"`
   - Result: passed, 72 passed, 0 failed.

2. `dotnet test LayerBase.Test\LayerBase.Test.csproj -c Release --no-restore --filter "FullyQualifiedName~ScopeLifecycleConcurrencyTests|FullyQualifiedName~ScopePromiseShutdownTests|FullyQualifiedName~ProjectedActorOwnershipTests|FullyQualifiedName~ScopeResourceGenerationTests|FullyQualifiedName~ModuleRuntimeIsolationTests|FullyQualifiedName~ScopeDiGenerationTests" --logger "console;verbosity=minimal"`
   - Initial result: compile errors in the new projected actor and promise test files. Fixed test imports/usings only.
   - Final focused result: failed, 7 passed, 3 failed.
   - Failing tests:
     - `ProjectedActorOwnershipTests.Shared_projected_actor_release_must_run_on_owner_thread_not_scope_worker`
     - `ScopeLifecycleConcurrencyTests.Dispose_must_not_return_before_concurrent_stop_cleanup_finishes`
     - `ScopePromiseShutdownTests.Continuation_close_and_drain_must_not_leave_successful_enqueue_unexecuted`

3. `dotnet test LayerBase.Test\LayerBase.Test.csproj -c Release --logger "console;verbosity=minimal"`
   - Result: failed, 494 passed, 3 failed, 0 skipped, 497 total.
   - Same three failures as the focused run.
