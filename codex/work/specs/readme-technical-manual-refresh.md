# Task Spec: README Technical Manual Refresh

## Goal

Reorganize and rewrite the repository root `README.md` into a clearer technical manual that follows `doc/ReadMe编写总纲`, gives new readers a shortest-path onboarding flow, and uses the latest benchmark artifacts as credible evidence instead of scattered performance claims.

## Context

- The repository already contains a long README, but its structure mixes positioning, architecture, benchmark details, and advanced features too early.
- `doc/ReadMe编写总纲` requires a homepage-first information architecture: explain what the project is, who it is for, provide a shortest-path quick start, then expand into concepts, workflows, boundaries, diagnostics, comparison, and maintenance guidance.
- Benchmark evidence is available under:
  - `LayerBase.BenchMark/bin/Release/net8.0/BenchmarkDotNet.Artifacts/results`
  - `LayerBase.BenchMark.Compare/bin/Release/net8.0/BenchmarkDotNet.Artifacts/results`
- The rewritten README must remain technically accurate to the current project and avoid unsupported marketing claims.

## Constraints

- Edit the root `README.md` only unless a supporting doc artifact is genuinely needed.
- Reuse verified benchmark numbers from the artifact outputs; do not invent unpublished results.
- Keep terminology explanations beginner-friendly on first use.
- Preserve important project boundaries, especially event-vs-call semantics and lifecycle requirements.
- No code or API behavior changes.

## Non-goals

- Re-running benchmarks.
- Changing library implementation, tests, samples, or package metadata.
- Producing a multi-file documentation site; this task is limited to the current README rewrite.

## Done Criteria

- `README.md` is reorganized into a clear technical manual flow matching the total-outline guidance.
- The homepage section answers: what it is, what problem it solves, who should use it, who should not, and why it differs from alternatives.
- The document includes a shortest-path install + minimal runnable example + expected result.
- The document includes benchmark-backed performance sections derived from the provided artifact data.
- The document explicitly documents lifecycle, threading, resource, and misuse boundaries.
- The document includes comparison, troubleshooting, best practices, compatibility, and FAQ sections.

## Verification

- Review `README.md` structure against `doc/ReadMe编写总纲`.
- Cross-check all cited benchmark figures against the artifact files.
- Inspect the final markdown for readability, section order, and internal consistency.
