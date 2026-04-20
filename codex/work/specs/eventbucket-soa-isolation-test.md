# Test Spec: EventBucket SOA Isolation

## Functional Verification

1. Existing `EventPipelineTests` pass unchanged in behavior.
2. Full `LayerBase.Test` project passes after the refactor.

## Performance Sanity

1. Run benchmark-style tests that exercise dispatch and mostly-idle pumping.
2. Treat these as sanity checks, not hard benchmark claims.

## Manual Review

1. Confirm hot-path dispatch code now dereferences isolated region objects instead of reading every SOA field from `EventBucket<T>`.
2. Confirm null/empty regions are skipped without touching unrelated arrays.
