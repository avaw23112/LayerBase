# Devlog: EventBucket SOA Isolation

## Start

- Scope limited to `EventBucket<T>` layout and dispatch dereference patterns.
- Hypothesis refined: the likely issue is hot-object footprint, not full eager traversal of every SOA array on every dispatch.
