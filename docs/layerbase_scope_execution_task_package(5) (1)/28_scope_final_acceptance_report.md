# 28 Scope Final Acceptance Report

Date: 2026-07-16

This report records the commands that were actually executed for task package 28.
It does not mark Repository Scope Migration Accepted because several acceptance gates
remain outside the verified set in this repository turn.

## Revision Under Test

- Previous pushed implementation commit: `32121af scope: add diagnostics snapshots`
- This report and the final acceptance tests are included by the containing commit.
- Reference baseline branch points recorded during the migration:
  - `master`: `7dee16c46d72a68f502554f693aed0c314b22be3`
  - `faster`: `8898a90bcb3e00a370e47f8b39f6eff32fa98980`

## Environment

- OS: Windows 11 `10.0.26200`
- .NET SDK: `10.0.301`
- .NET runtimes used by tests and benchmarks include `Microsoft.NETCore.App 8.0.28`
- Unity project layout was not present in this repository root; no `Assets`,
  `ProjectSettings`, or `Packages` directory was found for a real Unity IL2CPP build.

## Executed Gates

| Gate | Command | Result |
| --- | --- | --- |
| Scope final architecture tests | `dotnet test LayerBase.Test\LayerBase.Test.csproj -c Debug --filter "FullyQualifiedName~ScopeFinalAcceptanceTests"` | Passed `9/9` |
| All Debug tests | `dotnet test LayerBase.Test\LayerBase.Test.csproj -c Debug` | Passed `596/596` |
| All Release tests | `dotnet test LayerBase.Test\LayerBase.Test.csproj -c Release` | Passed `596/596` |
| Core net8 Release build | `dotnet build LayerBase\LayerBase.csproj -c Release -f net8.0` | Passed, `0` warnings, `0` errors |
| Core netstandard2.1 Release build | `dotnet build LayerBase\LayerBase.csproj -c Release -f netstandard2.1` | Passed, `0` errors, existing warnings only |
| Solution Debug build | `dotnet build LayerBase.sln -c Debug` | Passed with existing warnings |
| Solution Release build | `dotnet build LayerBase.sln -c Release` | Passed with existing warnings |
| Benchmark entry list | `dotnet run --project LayerBase.BenchMark\LayerBase.BenchMark.csproj -c Release -- --list flat` | Listed existing benchmark entries |
| Compare benchmark entry list | `dotnet run --project LayerBase.BenchMark.Compare\LayerBase.BenchMark.Compare.csproj -c Release -- --list flat` | Listed existing compare benchmark entries |
| Short Request/Response compare benchmark | `dotnet run --project LayerBase.BenchMark.Compare\LayerBase.BenchMark.Compare.csproj -c Release -- --filter "*RequestResponseCompareBench*" --job short` | Passed, generated BenchmarkDotNet reports |

## Short Benchmark Result

BenchmarkDotNet report:
`BenchmarkDotNet.Artifacts\results\LayerBaseCompareBenchmarks.RequestResponseCompareBench-report-github.md`

| Method | Mean | Allocated |
| --- | ---: | ---: |
| Direct LBTask request/response, 100k calls | `22.37 us` | `0 B` |
| LayerBase CallAsync, 100k calls | `565.75 us` | `0 B` |
| MessagePipe IRequestHandler, 100k calls | `36.49 us` | `0 B` |

This is a short compare run, not a replacement for a full benchmark baseline.

## Architecture Gates Added In Code

`LayerBase.Test/ScopeFinalAcceptanceTests.cs` adds static and reflection-based gates for:

- `ScopeRuntime` must not own `ActorWorld`, `WorkerJobScheduler`, or `Thread`.
- `ActorWorld` must not escape into Scope, Tool, ECS runtime owner, or application runtime owners.
- No third cross-Scope business channel names are allowed.
- `ScopeRef<TScope>` public surface remains address, post, and call oriented.
- Local call route entries must not route by `TargetScope`.
- LayerTool descriptors keep owner/cache metadata without public runtime object exposure.
- Production sources must not use runtime emit or dynamic compilation.
- Running Scope/runtime sources must not scan all assemblies or use generic runtime construction.
- Query generator entry points remain `void` and keep input parameters out of execute parameter lists.

## Failed Or Not Executed Gates

| Gate | Status | Evidence |
| --- | --- | --- |
| Unity IL2CPP Release gate | Not executed | This repository is not a Unity consumer project and has no Unity project layout. Per doc28, this must stay `Not Executed`; a normal `dotnet build` is not IL2CPP evidence. |
| Full benchmark baseline | Not executed | Only the short Request/Response compare benchmark was executed. A complete benchmark baseline still requires running the relevant existing BenchmarkDotNet suites and comparing against stored thresholds or historical reports. |
| LayerTool per-Scope instance isolation | Not fully accepted | Current public evidence verifies descriptor shape and absence of public runtime objects, but a full per-Scope Tool instance isolation benchmark/leak gate is not yet established. |

## Acceptance State

Repository Scope Migration Accepted: **No**

Reason: doc28 forbids final acceptance when any required gate is missing. The repository has passing
solution/core builds and tests, added architecture gates, and one executed short benchmark, but still has
no real Unity IL2CPP execution and no complete benchmark baseline.
