# Task Spec: Benchmark Runtime Reduction

## Goal

Reduce the runtime cost of the two benchmark entrypoints:
- `LayerBase.BenchMark/Program.cs`
- `LayerBase.BenchMark.Compare/Program.cs`

The intent is to keep the benchmark suites useful for iteration while requiring less total execution time and less oversized sample/data support.

## Context

- The current benchmark projects use BenchmarkDotNet and include broad benchmark sets plus large parameter/data combinations.
- The user wants them to run faster for day-to-day use, not necessarily maximize statistical depth.
- This is a benchmark ergonomics task, not a runtime behavior change.

## Constraints

- Keep the benchmark projects functional and still representative.
- Prefer config/parameter reduction over broad benchmark deletion.
- Keep diffs small and easy to reason about.
- No new dependencies.

## Non-goals

- Reworking benchmarked production logic.
- Deleting whole benchmark categories unless clearly unnecessary.
- Optimizing release accuracy for publication-grade benchmark reports.

## Done Criteria

- Both benchmark entrypoints use lighter BenchmarkDotNet settings and/or fewer heavy parameter sets.
- The projects still build successfully.
- The resulting changes are documented in the final summary.

## Verification

- `dotnet build LayerBase.BenchMark/LayerBase.BenchMark.csproj -c Debug`
- `dotnet build LayerBase.BenchMark.Compare/LayerBase.BenchMark.Compare.csproj -c Debug`
