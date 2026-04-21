# Plan: README Technical Manual Refresh

## Scope

Expected file touches:
- `README.md`
- planning artifact only: `codex/work/specs/readme-technical-manual-refresh.md`
- planning artifact only: `codex/work/plans/readme-technical-manual-refresh.md`

## Steps

1. Read the current README and the total-outline guidance to identify missing homepage information, misplaced advanced content, and unsupported claims.
2. Inspect benchmark artifact markdown/csv files and extract the most useful reader-facing evidence:
   - single-subscriber notify
   - fanout scaling
   - request/response call comparison
   - internal routing/load scenarios
3. Redesign the README information architecture so readers first see positioning, quick start, and trustworthy evidence before deeper internals.
4. Rewrite the README into a technical manual with runnable examples, explained terminology, workflow-oriented sections, and explicit limits.
5. Validate all benchmark references and do a final readability pass for flow, clarity, and maintenance usefulness.

## Risks

- Benchmark artifacts may contain many overlapping reports; selecting too many numbers could reduce clarity.
- Existing README terminology may overstate guarantees unless every strong claim is grounded by code or benchmark evidence.
- The manual must stay concise enough for a homepage while still serving as a long-lived technical entry point.

## Verification Notes

- Prefer artifact markdown/csv sources over current README claims when conflicts exist.
- Ensure the final structure moves from common/simple/stable paths to advanced/rare/high-risk topics.
