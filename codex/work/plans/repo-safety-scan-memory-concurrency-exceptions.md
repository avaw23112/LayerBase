# Plan: Repository Safety Scan for Memory, Concurrency, and Exception Risks

## Scope

Primary inspection targets:
- `LayerBase/Event/**`
- `LayerBase/Layer/**`
- `LayerBase/Tools/Timer/**`
- `LayerBase/DI/**`
- generator/runtime integration points where long-lived state is registered

## Steps

1. Inspect long-lived/static objects, pooling, reset/dispose, and subscription cleanup for memory-retention risk.
2. Inspect concurrent collections, lock/volatile/Interlocked usage, and mutation/publication patterns for race hazards.
3. Inspect exception handling in dispatch, async/parallel execution, callbacks, and generated registration paths for failure containment gaps.
4. Summarize findings by severity with confidence and fix direction.

## Risks

- Some issues may be “conditionally risky” rather than universally reproducible.
- Generated code and runtime registration can spread behavior across multiple files, so findings need careful scope attribution.
- Highly optimized hot-path code may intentionally trade readability for speed, increasing false-positive risk if inspected casually.

## Verification Notes

- Prefer concrete source references over generalized judgments.
- Only escalate a finding to “high risk” when failure mode and impact are both clear from code.
