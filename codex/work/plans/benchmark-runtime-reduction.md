# Plan: Benchmark Runtime Reduction

## Scope

Expected file touches:
- `LayerBase.BenchMark/Program.cs`
- `LayerBase.BenchMark.Compare/Program.cs`

## Steps

1. Inspect the BenchmarkDotNet config in both benchmark entrypoints.
2. Identify heavy parameter/data combinations that can be reduced without gutting coverage.
3. Apply minimal config changes to shorten warmup/iteration volume and trim oversized params where useful.
4. Build both benchmark projects to verify the edits.

## Risks

- Over-reducing iteration count can make results noisier.
- Removing too many params can weaken comparison coverage.
- The best compromise depends on whether the user wants developer-speed runs or publication-quality runs.

## Verification Notes

- Favor faster debug-build verification rather than full benchmark execution during this change.
