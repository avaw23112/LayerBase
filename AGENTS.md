# Repository Guidelines

## Project Structure & Module Organization
- `LayerBase/`: core library code; key areas include `Application/` (bootstrapping), `DataStruct/` (utilities like `FreeList`), `DI/` (service wiring), `Event/` (event types, dispatcher, metadata), `Layer/` (layer lifecycle), and `Tools/Timer/` (tick-driven timers such as `TimerScheduler`).
- `LayerBase.Test/`: NUnit tests validating DI, layers, and services.
- `LayerBase.Usages/`: runnable samples and playground code (e.g., `LayerChainTest.cs`).
- Solutions: `LayerBase.sln` (primary), plus generator/task projects under `LayerBase.Generator/` and `LayerBase.Task/` when needed.

## Build, Test, and Development Commands
- `dotnet restore LayerBase.sln` — restore dependencies.
- `dotnet build LayerBase.sln -c Debug` (or `Release`) — build all projects.
- `dotnet test LayerBase.Test/LayerBase.Test.csproj -c Debug` — run NUnit tests.
- `dotnet run --project LayerBase.Usages/LayerBase.Usages.csproj` — execute sample usage code.
- For iterative work, prefer `dotnet watch test` or `dotnet watch run` on the relevant project when available.

## Coding Style & Naming Conventions
- Language: C# with 4-space indentation; braces on new lines for types/methods; use guard clauses for argument validation (see timer APIs).
- Naming: PascalCase for types/methods/properties; camelCase for locals/fields (prefix `_` for private fields); async methods suffix with `Async`.
- Event patterns: use `Event<T>` for payloads, `EventHandlerDelegate<T>`/`IEventHandler<T>` for handlers, and `Action<Event<T>>` when scheduling via `TimerScheduler`.
- Avoid introducing non-ASCII unless required; keep comments minimal and purposeful.

## Testing Guidelines
- Framework: NUnit (`[Test]`, `Assert`, `StringAssert`, `Is` helpers).
- Naming: describe behavior and expectation (e.g., `GeneratedServices_are_resolvable_with_expected_lifetimes`).
- Scope: prefer deterministic, isolated tests; dispose layers/services in `try/finally` as in existing tests.
- Run `dotnet test` before submitting; add coverage when modifying dispatching, DI wiring, or timers.

## Commit & Pull Request Guidelines
- Commits: concise, imperative subjects; reference affected area (`timer: add event action overloads`). Include brief body when rationale or context is non-obvious.
- PRs: describe intent, key changes, and risks; list tests executed (`dotnet test ...`). Link issues/tasks when applicable; add screenshots/log snippets for behavioral changes.
- Keep diffs focused; avoid reformatting unrelated code; ensure new APIs have inline comments or XML docs when behavior is non-obvious.

## Security & Configuration Tips
- Timer and event code is tick-driven; avoid blocking handlers—use async (`LBTask`) or `Action<Event<T>>` where appropriate.
- Threading: `TimerScheduler` uses internal locks; interact via public APIs and avoid external synchronization around it.
- Configuration: frequency gates set via `SetFrequency`; zero disables frequency invocations. Normalize times to non-negative when scheduling.
