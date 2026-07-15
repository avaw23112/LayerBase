# Scope Migration Incremental Plan

## ActorUpdateAttribute follow-up

`ActorUpdateAttribute` is an actor lifecycle entry point and should be closed during the 21/22 ActorWorld scope migration work, not during the 05/06 composition model pass.

Acceptance points:

- `ActorUpdateAttribute` metadata remains owned by actor metadata (`ActorTypeMeta`) and does not become a Layer/Service lifecycle hook.
- `ActorLifecycleScheduler` is driven only from the owning `ScopeRuntime` actor pump.
- MainScope ActorWorld execution stays on the MainScope owner thread until custom Scope ActorWorld support is explicitly introduced.
- Worker/custom scope migration must verify that no actor lifecycle path reaches `LayerRuntime` global pump resources directly.
