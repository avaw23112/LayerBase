# Plan: EventBucket SOA Isolation

1. Introduce a compact dispatch-state object plus per-region objects for sync, async, interface, and parallel SOA data.
2. Update rebuild to populate and publish the isolated state.
3. Update dispatch/post/local paths to use the isolated state and region references.
4. Run functional tests and benchmark-style sanity checks.
