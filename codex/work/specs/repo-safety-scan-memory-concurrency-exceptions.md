# Task Spec: Repository Safety Scan for Memory, Concurrency, and Exception Risks

## Goal

Perform a focused repository-wide engineering analysis to identify likely:
- memory leaks / retention risks
- concurrency-safety hazards
- high-risk exception paths

The output should prioritize concrete, evidence-backed findings over broad stylistic commentary.

## Context

- LayerBase contains shared infrastructure for event dispatch, layers, timers, DI/service registration, and generated registrations.
- The highest-risk areas are likely long-lived/static state, pooled resources, async continuations, subscriptions, cross-thread queues, and reset/dispose behavior.
- The user asked for a scan, not a refactor, so this task is analysis-first.

## Constraints

- No speculative claims without pointing to concrete code evidence.
- Prefer shared infrastructure and hot-path primitives over low-impact application samples.
- Distinguish between confirmed bug candidates, plausible risks, and lower-confidence observations.
- Keep `Call` semantics framed correctly: `Call` is only a single-target functional slice mechanism, not a workflow orchestration boundary.

## Non-goals

- Fixing every issue in this pass.
- Exhaustive audit of benchmark/sample code unless it reveals shared runtime risk.
- General style cleanup unrelated to safety/reliability.

## Done Criteria

- Review the main infrastructure areas relevant to memory lifecycle, synchronization, and exception containment.
- Produce a prioritized list of findings with severity, confidence, and source locations.
- For each finding, explain why it is risky and suggest a practical mitigation direction.
- Clearly separate confirmed issues from “needs validation” candidates.

## Verification

- Use direct code inspection with file/line evidence.
- If needed, use targeted builds/tests only to validate especially suspicious claims.
