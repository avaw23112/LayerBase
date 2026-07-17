# Generated Scope Definition and Stable ScopeId Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Use `superpowers:test-driven-development` inside every task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace manually authored and reflectively resolved `ScopeId` values with source-generated, cross-process-stable identifiers, while making every Scope a class object that owns its own public `ScopeOptions`.

**Architecture:** Scope identity is computed at compile time from either an optional `[ScopeIdentity("stable-key")]` or the default `scope:{assembly-simple-name}:{fully-qualified-metadata-type-name}` string. Roslyn generators emit `GeneratedScopeDefinition` records containing the stable ID, identity, CLR type, and a non-reflective factory. Runtime composition merges generated definitions from pushed Layers and assembly Modules, validates type/identity/ID conflicts, creates one `IScopeDefinition` instance per Scope per `LayerRuntime`, and builds `ScopeExecutionPlan.Options` from that object.

**Tech Stack:** C# 12, .NET 8/9, Roslyn `IIncrementalGenerator`, SHA-256 at compile time, NUnit, `LayerBase.sln`.

## Baseline and execution preconditions

- Target repository: `avaw23112/LayerBase`.
- Plan baseline: `master` at commit `cc31fa9137c9e306c2434736b761f75fc9610128`.
- Do not apply this work on top of an uncommitted tree.
- Keep the existing `MainScope.ScopeId == 0` compatibility constant. Only framework-owned `MainScope` may use ID `0`.
- Custom Scope definitions must no longer declare `ScopeId`.
- Do not use `Type.GetField`, `FieldInfo.GetValue`, `Activator.CreateInstance`, assembly scanning, or runtime hashing to discover/create Scope definitions.
- Hashing occurs only inside the generator. Generated runtime code contains literal integer IDs.
- Adding, deleting, or reordering unrelated Scope types must not change an existing Scope ID.
- Default identity changes when the Scope type name, namespace, or defining assembly name changes.
- `[ScopeIdentity]` overrides default identity and survives type/namespace/assembly renames.
- Stable keys and identity comparisons use `StringComparison.Ordinal`; do not lowercase them.
- Scope classes may not be abstract, generic, or value types.
- A Scope used by generated code must expose a parameterless constructor accessible from the generated caller.
- Each `LayerRuntime` creates exactly one definition object for each unique Scope type.
- Duplicate generated registrations for the same `(Type, Identity, ScopeId)` are allowed and deduplicated.
- Same Type with different identity/ID, same identity with different Type, or same ID with different identity must fail composition.
- Follow repository style: four spaces, braces on new lines, guard clauses, focused diffs, no unrelated formatting.
- Run all commands from the repository root.

## Worktree preparation

- [ ] Read `AGENTS.md`.
- [ ] Confirm the expected baseline:

```bash
git status --short
git rev-parse HEAD
git merge-base --is-ancestor cc31fa9137c9e306c2434736b761f75fc9610128 HEAD
```

Expected:

```text
git status --short: no output
git merge-base: exit code 0
```

- [ ] Create an isolated worktree from the current branch:

```bash
BASE_BRANCH="$(git branch --show-current)"
git worktree add ../LayerBase-generated-scope-id \
    -b refactor/generated-scope-id \
    "$BASE_BRANCH"
cd ../LayerBase-generated-scope-id
```

- [ ] Establish the baseline:

```bash
dotnet restore LayerBase.sln
dotnet build LayerBase.sln -c Release --no-restore
dotnet test LayerBase.Test/LayerBase.Test.csproj -c Release --no-build
```

Expected: restore, build, and all NUnit tests pass before edits.

---

## File responsibility map

### New runtime files

- `LayerBase/Scope/ScopeIdentityAttribute.cs`
  - Declares the optional persistent identity override.
- `LayerBase/Scope/GeneratedScopeDefinition.cs`
  - Runtime-neutral generated descriptor and factory delegate.
- `LayerBase/Scope/ScopeDefinitionRegistry.cs`
  - Merges Layer and Module descriptors, validates conflicts, and provides type/ID lookup.

### New generator files

- `LayerBase.Generator/LayerBase.Generator/ScopeDefinitionCodeGen.cs`
  - Shared symbol validation, identity construction, metadata-name construction, SHA-256 ID generation, descriptor emission helpers, and diagnostic descriptors.
- `LayerBase.Generator/LayerBase.Generator/ScopeDefinitionGenerator.cs`
  - Validates every locally declared `IScopeDefinition` and detects local identity/ID collisions.

### New tests

- `LayerBase.Test/ScopeDefinitionApiTests.cs`
  - Public API and immutable options behavior.
- `LayerBase.Test/ScopeDefinitionGeneratorTests.cs`
  - Generator validation, stable hash behavior, generated factory, and rename behavior.
- `LayerBase.Test/ScopeDefinitionRegistryTests.cs`
  - Runtime deduplication and conflict rejection.
- `LayerBase.Test/ScopeDefinitionRuntimeIntegrationTests.cs`
  - End-to-end options ownership, one factory call per runtime, Module path, and Layer path.

### Existing files to modify

- `LayerBase/Scope/ScopeDefinitions.cs`
- `LayerBase/Scope/ScopeRuntimeModel.cs`
- `LayerBase/Scope/ScopeAttribute.cs`
- `LayerBase/Scope/IGeneratedScopeDefinitionProvider.cs`
- `LayerBase/Scope/RuntimeCompositionPlan.cs`
- `LayerBase/Modules/AssemblyModule.cs`
- `LayerBase.Generator/LayerBase.Generator/LayerServiceGenerator.cs`
- `LayerBase.Generator/LayerBase.Generator/AssemblyModuleGenerator.cs`
- `LayerBase.Usage/BusinessRetailContracts.cs`
- All test/source files reported by:

```bash
rg -l ': IScopeDefinition' LayerBase LayerBase.Test LayerBase.Usage
```

---

# Task 1: Make Scope definitions class objects that own public options

**Files:**
- Create: `LayerBase/Scope/ScopeIdentityAttribute.cs`
- Create: `LayerBase/Scope/GeneratedScopeDefinition.cs`
- Create: `LayerBase.Test/ScopeDefinitionApiTests.cs`
- Modify: `LayerBase/Scope/ScopeDefinitions.cs`
- Modify: `LayerBase/Scope/ScopeRuntimeModel.cs`
- Modify: `LayerBase/Scope/ScopeAttribute.cs`

**Interfaces:**
- Produces:
  - `IScopeDefinition.Options : ScopeOptions`
  - `ScopeIdentityAttribute(string value)`
  - `ScopeDefinitionFactory`
  - `GeneratedScopeDefinition`
  - Public `ScopeThreadingMode`, `ScopeClockMode`, and `ScopeOptions`
- Consumes: Existing `ScopeFaultPolicy` and `EcsRuntimeOptions`.

- [ ] **Step 1: Add failing public API tests**

Create `LayerBase.Test/ScopeDefinitionApiTests.cs`:

```csharp
using LayerBase.ECS;
using LayerBase.Scope;

namespace LayerBase.Test;

[TestFixture]
public sealed class ScopeDefinitionApiTests
{
    [Test]
    public void Scope_definition_exposes_its_own_options()
    {
        var scope = new WorkerProbeScope();

        Assert.Multiple(() =>
        {
            Assert.That(scope.Options.Threading, Is.EqualTo(ScopeThreadingMode.Worker));
            Assert.That(scope.Options.Clock, Is.EqualTo(ScopeClockMode.FixedRate));
            Assert.That(scope.Options.TickRateHz, Is.EqualTo(37));
            Assert.That(scope.Options.FaultPolicy, Is.EqualTo(ScopeFaultPolicy.FailScope));
        });
    }

    [Test]
    public void Stable_identity_attribute_trims_surrounding_whitespace()
    {
        var attribute = new ScopeIdentityAttribute("  game.inventory  ");

        Assert.That(attribute.Value, Is.EqualTo("game.inventory"));
    }

    [Test]
    public void Stable_identity_attribute_rejects_empty_value()
    {
        Assert.That(
            () => new ScopeIdentityAttribute("   "),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Generated_definition_rejects_custom_scope_id_zero()
    {
        Assert.That(
            () => new GeneratedScopeDefinition(
                scopeId: 0,
                identity: "scope-key:game.inventory",
                scopeType: typeof(WorkerProbeScope),
                factory: static () => new WorkerProbeScope()),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Generated_definition_factory_must_return_declared_type()
    {
        var descriptor = new GeneratedScopeDefinition(
            scopeId: 17,
            identity: "scope-key:game.inventory",
            scopeType: typeof(WorkerProbeScope),
            factory: static () => new InlineProbeScope());

        Assert.That(
            descriptor.CreateDefinition,
            Throws.TypeOf<InvalidOperationException>());
    }

    private sealed class WorkerProbeScope : IScopeDefinition
    {
        public ScopeOptions Options { get; } = ScopeOptions.Worker(
            tickRateHz: 37,
            faultPolicy: ScopeFaultPolicy.FailScope,
            ecsRuntime: EcsRuntimeOptions.Default);
    }

    private sealed class InlineProbeScope : IScopeDefinition
    {
        public ScopeOptions Options => ScopeOptions.Inline;
    }
}
```

Every named argument above has one responsibility:

- `scopeId`: precomputed routing ID embedded by generated code.
- `identity`: canonical string used to explain and validate the ID.
- `scopeType`: exact CLR type that the factory is required to return.
- `factory`: source-generated constructor delegate; it replaces reflection.
- `tickRateHz`: fixed worker ticks per second.
- `faultPolicy`: behavior when the Scope reports an unhandled failure.
- `ecsRuntime`: ECS configuration owned by this Scope.

- [ ] **Step 2: Run the tests and verify the API is missing**

```bash
dotnet test LayerBase.Test/LayerBase.Test.csproj \
    -c Release \
    --filter FullyQualifiedName~ScopeDefinitionApiTests
```

Expected: compilation fails because `IScopeDefinition.Options`, public options types, `ScopeIdentityAttribute`, and `GeneratedScopeDefinition` do not exist.

- [ ] **Step 3: Implement `ScopeIdentityAttribute`**

Create `LayerBase/Scope/ScopeIdentityAttribute.cs`:

```csharp
namespace LayerBase.Scope;

/// <summary>
/// Overrides the default type-based Scope identity with a persistent key.
/// Use this only when a Scope must preserve its ID across type, namespace,
/// or assembly renames.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ScopeIdentityAttribute : Attribute
{
    public ScopeIdentityAttribute(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Scope identity is required.", nameof(value));

        // Trimming prevents invisible leading/trailing whitespace from becoming
        // part of a long-lived network identity.
        Value = value.Trim();
    }

    /// <summary>
    /// Project-wide unique key. Comparisons are ordinal and case-sensitive.
    /// </summary>
    public string Value { get; }
}
```

- [ ] **Step 4: Replace the Scope definition contract**

Replace `LayerBase/Scope/ScopeDefinitions.cs` with:

```csharp
namespace LayerBase.Scope;

/// <summary>
/// Defines one runtime execution domain and the immutable options used to build it.
/// A LayerRuntime creates one definition object per unique Scope type.
/// </summary>
public interface IScopeDefinition
{
    /// <summary>
    /// Threading, clock, update rate, fault, and ECS configuration for this Scope.
    /// Runtime composition reads this property once while building the execution plan.
    /// </summary>
    ScopeOptions Options { get; }
}

/// <summary>
/// Built-in default Scope. ID zero remains framework-reserved for compatibility.
/// </summary>
public sealed class MainScope : IScopeDefinition
{
    public const int ScopeId = 0;

    public ScopeOptions Options => ScopeOptions.Main;
}

internal static class ScopeDefinitionIds
{
    public const int Main = MainScope.ScopeId;

    public const string MainIdentity =
        "scope:LayerBase:LayerBase.Scope.MainScope";
}
```

Do not keep `ScopeDefinitionIds.Resolve(Type)`. That method is the reflection path being removed.

- [ ] **Step 5: Make the options API public and immutable**

In `LayerBase/Scope/ScopeRuntimeModel.cs`:

1. Change `ScopeThreadingMode`, `ScopeClockMode`, and `ScopeOptions` from `internal` to `public`.
2. Keep `ScopeOptions` as `readonly struct`.
3. Replace its constructor/factory region with:

```csharp
public readonly struct ScopeOptions
{
    public ScopeOptions(
        ScopeThreadingMode threading,
        ScopeClockMode clock,
        int tickRateHz,
        ScopeFaultPolicy faultPolicy = ScopeFaultPolicy.ReportAndContinue,
        EcsRuntimeOptions? ecsRuntime = null)
    {
        if (tickRateHz < 0)
            throw new ArgumentOutOfRangeException(nameof(tickRateHz));

        if (threading == ScopeThreadingMode.Worker &&
            clock != ScopeClockMode.FixedRate)
        {
            throw new ArgumentException(
                "Worker Scope must use FixedRate clock mode.",
                nameof(clock));
        }

        if (clock == ScopeClockMode.FixedRate && tickRateHz <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tickRateHz),
                "FixedRate Scope requires a positive tick rate.");
        }

        if (clock != ScopeClockMode.FixedRate && tickRateHz != 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tickRateHz),
                "Only FixedRate Scope may declare a non-zero tick rate.");
        }

        Threading = threading;
        Clock = clock;
        TickRateHz = tickRateHz;
        FaultPolicy = faultPolicy;
        EcsRuntime = ecsRuntime ?? EcsRuntimeOptions.Default;
    }

    public ScopeThreadingMode Threading { get; }

    public ScopeClockMode Clock { get; }

    public int TickRateHz { get; }

    public ScopeFaultPolicy FaultPolicy { get; }

    public EcsRuntimeOptions EcsRuntime { get; }

    public static ScopeOptions Main { get; } = new(
        threading: ScopeThreadingMode.Main,
        clock: ScopeClockMode.RuntimePump,
        tickRateHz: 0);

    public static ScopeOptions Inline { get; } = new(
        threading: ScopeThreadingMode.Inline,
        clock: ScopeClockMode.RuntimePump,
        tickRateHz: 0);

    public static ScopeOptions Manual(
        ScopeFaultPolicy faultPolicy = ScopeFaultPolicy.ReportAndContinue,
        EcsRuntimeOptions? ecsRuntime = null)
    {
        return new ScopeOptions(
            threading: ScopeThreadingMode.Inline,
            clock: ScopeClockMode.Manual,
            tickRateHz: 0,
            faultPolicy: faultPolicy,
            ecsRuntime: ecsRuntime);
    }

    public static ScopeOptions Worker(
        int tickRateHz = 60,
        ScopeFaultPolicy faultPolicy = ScopeFaultPolicy.ReportAndContinue,
        EcsRuntimeOptions? ecsRuntime = null)
    {
        return new ScopeOptions(
            threading: ScopeThreadingMode.Worker,
            clock: ScopeClockMode.FixedRate,
            tickRateHz: tickRateHz,
            faultPolicy: faultPolicy,
            ecsRuntime: ecsRuntime);
    }

    public ScopeOptions WithEcsRuntime(EcsRuntimeOptions ecsRuntime)
    {
        return new ScopeOptions(
            threading: Threading,
            clock: Clock,
            tickRateHz: TickRateHz,
            faultPolicy: FaultPolicy,
            ecsRuntime: ecsRuntime);
    }
}
```

Do not change enum numeric values; serialized diagnostics and existing switch statements depend on them.

- [ ] **Step 6: Add the generated descriptor**

Create `LayerBase/Scope/GeneratedScopeDefinition.cs`:

```csharp
namespace LayerBase.Scope;

/// <summary>
/// Constructs one Scope definition object without Activator or reflection.
/// </summary>
public delegate IScopeDefinition ScopeDefinitionFactory();

/// <summary>
/// Compile-time generated registration for one Scope type.
/// </summary>
public readonly struct GeneratedScopeDefinition
{
    public GeneratedScopeDefinition(
        int scopeId,
        string identity,
        Type scopeType,
        ScopeDefinitionFactory factory)
    {
        if (scopeType == null)
            throw new ArgumentNullException(nameof(scopeType));
        if (!typeof(IScopeDefinition).IsAssignableFrom(scopeType))
            throw new ArgumentException(
                $"Scope type '{scopeType.FullName}' must implement {nameof(IScopeDefinition)}.",
                nameof(scopeType));
        if (scopeId < 0)
            throw new ArgumentOutOfRangeException(nameof(scopeId));
        if (scopeId == ScopeDefinitionIds.Main && scopeType != typeof(MainScope))
            throw new ArgumentOutOfRangeException(
                nameof(scopeId),
                "Scope ID zero is reserved for MainScope.");
        if (string.IsNullOrWhiteSpace(identity))
            throw new ArgumentException("Scope identity is required.", nameof(identity));

        ScopeId = scopeId;
        Identity = identity;
        ScopeType = scopeType;
        Factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public int ScopeId { get; }

    public string Identity { get; }

    public Type ScopeType { get; }

    public ScopeDefinitionFactory Factory { get; }

    public IScopeDefinition CreateDefinition()
    {
        IScopeDefinition definition = Factory()
            ?? throw new InvalidOperationException(
                $"Scope factory for '{ScopeType.FullName}' returned null.");

        if (definition.GetType() != ScopeType)
        {
            throw new InvalidOperationException(
                $"Scope factory for '{ScopeType.FullName}' returned " +
                $"'{definition.GetType().FullName}'.");
        }

        return definition;
    }

    internal static GeneratedScopeDefinition Main { get; } = new(
        scopeId: ScopeDefinitionIds.Main,
        identity: ScopeDefinitionIds.MainIdentity,
        scopeType: typeof(MainScope),
        factory: static () => new MainScope());
}
```

- [ ] **Step 7: Tighten the generic Scope attribute**

In `LayerBase/Scope/ScopeAttribute.cs`, change only the generic constraint:

```csharp
public sealed class ScopeAttribute<TScope> : ScopeAttribute
    where TScope : class, IScopeDefinition
```

Do not add `new()` to this public constraint. Constructor accessibility is validated by the generator, which also supports same-assembly internal constructors.

- [ ] **Step 8: Run the focused tests**

```bash
dotnet test LayerBase.Test/LayerBase.Test.csproj \
    -c Release \
    --filter FullyQualifiedName~ScopeDefinitionApiTests
```

Expected: all tests pass.

- [ ] **Step 9: Commit**

```bash
git add \
    LayerBase/Scope/ScopeIdentityAttribute.cs \
    LayerBase/Scope/GeneratedScopeDefinition.cs \
    LayerBase/Scope/ScopeDefinitions.cs \
    LayerBase/Scope/ScopeRuntimeModel.cs \
    LayerBase/Scope/ScopeAttribute.cs \
    LayerBase.Test/ScopeDefinitionApiTests.cs

git commit -m "refactor(scope): make definitions own runtime options"
```

---

# Task 2: Add deterministic identity and generator validation

**Files:**
- Create: `LayerBase.Generator/LayerBase.Generator/ScopeDefinitionCodeGen.cs`
- Create: `LayerBase.Generator/LayerBase.Generator/ScopeDefinitionGenerator.cs`
- Create: `LayerBase.Test/ScopeDefinitionGeneratorTests.cs`

**Interfaces:**
- Produces:
  - `ScopeDefinitionCodeGen.TryCreateModel(...)`
  - `ScopeDefinitionCodeGen.ComputeScopeId(string identity)`
  - `ScopeDefinitionModel`
  - Diagnostics `LBSC001` through `LBSC009`
- Consumes:
  - `LayerBase.Scope.IScopeDefinition`
  - `LayerBase.Scope.ScopeIdentityAttribute`

- [ ] **Step 1: Add generator tests before implementation**

Create `LayerBase.Test/ScopeDefinitionGeneratorTests.cs`. Reuse the metadata references and `GeneratorDriver` pattern already present in `LayerGeneratorContractTests`; keep the helper local to this fixture so tests can choose compilation assembly names.

The fixture must include these tests:

```csharp
[Test]
public void Default_identity_is_stable_when_unrelated_scope_is_added()
{
    const string original = """
        using LayerBase.Scope;

        public sealed class InventoryScope : IScopeDefinition
        {
            public ScopeOptions Options => ScopeOptions.Inline;
        }
        """;

    const string withAnotherScope = """
        using LayerBase.Scope;

        public sealed class AlphaScope : IScopeDefinition
        {
            public ScopeOptions Options => ScopeOptions.Inline;
        }

        public sealed class InventoryScope : IScopeDefinition
        {
            public ScopeOptions Options => ScopeOptions.Inline;
        }
        """;

    int originalId = GetGeneratedScopeId(
        source: original,
        assemblyName: "Game.Contracts",
        scopeTypeName: "InventoryScope");

    int changedId = GetGeneratedScopeId(
        source: withAnotherScope,
        assemblyName: "Game.Contracts",
        scopeTypeName: "InventoryScope");

    Assert.That(changedId, Is.EqualTo(originalId));
}

[Test]
public void Default_identity_changes_when_type_name_changes()
{
    int first = GetGeneratedScopeId(
        source: CreateSingleScopeSource("InventoryScope", stableKey: null),
        assemblyName: "Game.Contracts",
        scopeTypeName: "InventoryScope");

    int second = GetGeneratedScopeId(
        source: CreateSingleScopeSource("RenamedInventoryScope", stableKey: null),
        assemblyName: "Game.Contracts",
        scopeTypeName: "RenamedInventoryScope");

    Assert.That(second, Is.Not.EqualTo(first));
}

[Test]
public void Stable_key_survives_type_and_assembly_rename()
{
    int first = GetGeneratedScopeId(
        source: CreateSingleScopeSource(
            typeName: "InventoryScope",
            stableKey: "game.inventory"),
        assemblyName: "Game.Contracts",
        scopeTypeName: "InventoryScope");

    int second = GetGeneratedScopeId(
        source: CreateSingleScopeSource(
            typeName: "RenamedInventoryScope",
            stableKey: "game.inventory"),
        assemblyName: "Game.Server.Contracts",
        scopeTypeName: "RenamedInventoryScope");

    Assert.That(second, Is.EqualTo(first));
}

[TestCase("LBSC003", "public readonly struct BadScope : IScopeDefinition")]
[TestCase("LBSC004", "public abstract class BadScope : IScopeDefinition")]
[TestCase("LBSC005", "public sealed class BadScope<T> : IScopeDefinition")]
public void Invalid_scope_shape_reports_expected_diagnostic(
    string diagnosticId,
    string declaration)
{
    string source = $$"""
        using LayerBase.Scope;

        {{declaration}}
        {
            public ScopeOptions Options => ScopeOptions.Inline;
        }
        """;

    GeneratorDriverRunResult result = RunScopeGenerators(
        source: source,
        assemblyName: "Game.Contracts");

    Assert.That(
        result.Diagnostics.Select(static diagnostic => diagnostic.Id),
        Does.Contain(diagnosticId));
}

[Test]
public void Empty_stable_key_reports_LBSC007()
{
    const string source = """
        using LayerBase.Scope;

        [ScopeIdentity("   ")]
        public sealed class BadScope : IScopeDefinition
        {
            public ScopeOptions Options => ScopeOptions.Inline;
        }
        """;

    GeneratorDriverRunResult result = RunScopeGenerators(
        source: source,
        assemblyName: "Game.Contracts");

    Assert.That(
        result.Diagnostics.Select(static diagnostic => diagnostic.Id),
        Does.Contain("LBSC007"));
}

[Test]
public void Manual_scope_id_reports_LBSC009()
{
    const string source = """
        using LayerBase.Scope;

        public sealed class BadScope : IScopeDefinition
        {
            public const int ScopeId = 10;
            public ScopeOptions Options => ScopeOptions.Inline;
        }
        """;

    GeneratorDriverRunResult result = RunScopeGenerators(
        source: source,
        assemblyName: "Game.Contracts");

    Assert.That(
        result.Diagnostics.Select(static diagnostic => diagnostic.Id),
        Does.Contain("LBSC009"));
}
```

The helper `GetGeneratedScopeId` must run both `ScopeDefinitionGenerator` and `LayerServiceGenerator` against a source that also contains a partial Layer and a scoped Service. It must inspect generated source text, locate the descriptor whose `scopeType` matches the requested type, and parse its literal `scopeId`.

Do not calculate the expected numeric ID in test code with a second implementation. The test verifies stability by comparing generated outputs, preventing the test and implementation from sharing the same defect.

- [ ] **Step 2: Run tests and verify they fail**

```bash
dotnet test LayerBase.Test/LayerBase.Test.csproj \
    -c Release \
    --filter FullyQualifiedName~ScopeDefinitionGeneratorTests
```

Expected: compilation fails because the new generator and helper do not exist.

- [ ] **Step 3: Implement shared identity construction and hashing**

Create `LayerBase.Generator/LayerBase.Generator/ScopeDefinitionCodeGen.cs`.

Required model:

```csharp
internal readonly struct ScopeDefinitionModel
{
    public ScopeDefinitionModel(
        INamedTypeSymbol scopeType,
        int scopeId,
        string identity,
        string fullyQualifiedTypeName,
        Location? location)
    {
        ScopeType = scopeType;
        ScopeId = scopeId;
        Identity = identity;
        FullyQualifiedTypeName = fullyQualifiedTypeName;
        Location = location;
    }

    public INamedTypeSymbol ScopeType { get; }

    public int ScopeId { get; }

    public string Identity { get; }

    public string FullyQualifiedTypeName { get; }

    public Location? Location { get; }
}
```

Required identity rules:

```csharp
private const string ScopeInterfaceMetadataName =
    "LayerBase.Scope.IScopeDefinition";

private const string ScopeIdentityAttributeMetadataName =
    "LayerBase.Scope.ScopeIdentityAttribute";

internal static string BuildIdentity(INamedTypeSymbol scopeType)
{
    AttributeData? identityAttribute = scopeType
        .GetAttributes()
        .FirstOrDefault(static attribute =>
            attribute.AttributeClass?.ToDisplayString() ==
            ScopeIdentityAttributeMetadataName);

    if (identityAttribute != null)
    {
        string? rawValue =
            identityAttribute.ConstructorArguments.Length == 1
                ? identityAttribute.ConstructorArguments[0].Value as string
                : null;

        if (!string.IsNullOrWhiteSpace(rawValue))
            return "scope-key:" + rawValue.Trim();
    }

    string assemblyName = scopeType.ContainingAssembly.Name;
    string metadataName = GetFullyQualifiedMetadataName(scopeType);

    return $"scope:{assemblyName}:{metadataName}";
}
```

Required metadata-name rules:

- Namespace separator: `.`
- Nested type separator: `+`
- Use each symbol's `MetadataName`, not display alias.
- Generic scopes are rejected before this is used.

Example:

```text
Namespace.Outer+InventoryScope
```

Required deterministic hash:

```csharp
internal static int ComputeScopeId(string identity)
{
    if (string.IsNullOrWhiteSpace(identity))
        throw new ArgumentException("Scope identity is required.", nameof(identity));

    for (int attempt = 0; attempt < 32; attempt++)
    {
        string candidate = attempt == 0
            ? identity
            : identity + "#" + attempt.ToString(CultureInfo.InvariantCulture);

        byte[] input = Encoding.UTF8.GetBytes(candidate);
        byte[] digest;

        using (SHA256 sha256 = SHA256.Create())
            digest = sha256.ComputeHash(input);

        // The highest bit is discarded so the value always fits a non-negative Int32.
        // Byte order is fixed explicitly; platform endianness must not influence the ID.
        int scopeId =
            ((digest[0] & 0x7F) << 24) |
            (digest[1] << 16) |
            (digest[2] << 8) |
            digest[3];

        if (scopeId != 0)
            return scopeId;
    }

    throw new InvalidOperationException(
        $"Unable to derive a non-zero Scope ID for identity '{identity}'.");
}
```

Required diagnostics:

| ID | Severity | Condition |
|---|---|---|
| `LBSC001` | Error | Different identities produce the same non-zero ID in one compilation |
| `LBSC002` | Error | Different Scope types use the same canonical identity |
| `LBSC003` | Error | Scope is not a class |
| `LBSC004` | Error | Scope is abstract |
| `LBSC005` | Error | Scope or a containing type is generic |
| `LBSC006` | Error | No accessible parameterless instance constructor |
| `LBSC007` | Error | `[ScopeIdentity]` value is null/empty/whitespace |
| `LBSC008` | Error | Candidate does not implement `IScopeDefinition` |
| `LBSC009` | Error | Scope declares a static `int ScopeId` field/property |

Constructor accessibility rules:

- Same assembly: allow `public`, `internal`, and `protected internal`.
- Referenced assembly: require `public`.
- Reject static constructors and constructors with parameters.
- Use Roslyn symbols only; do not use runtime reflection.

- [ ] **Step 4: Implement the validation generator**

Create `LayerBase.Generator/LayerBase.Generator/ScopeDefinitionGenerator.cs`:

```csharp
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LayerBase.Generator;

[Generator(LanguageNames.CSharp)]
public sealed class ScopeDefinitionGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<INamedTypeSymbol> candidates =
            context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) =>
                        node is TypeDeclarationSyntax { BaseList: not null },
                    transform: static (syntaxContext, _) =>
                        syntaxContext.SemanticModel.GetDeclaredSymbol(
                            (TypeDeclarationSyntax)syntaxContext.Node)
                        as INamedTypeSymbol)
                .Where(static symbol => symbol != null)
                .Select(static (symbol, _) => symbol!);

        IncrementalValueProvider<
            (Compilation Compilation, ImmutableArray<INamedTypeSymbol> Symbols)>
            input = context.CompilationProvider.Combine(candidates.Collect());

        context.RegisterSourceOutput(
            input,
            static (productionContext, value) =>
                Execute(
                    productionContext,
                    value.Compilation,
                    value.Symbols));
    }

    private static void Execute(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<INamedTypeSymbol> symbols)
    {
        // Deduplicate partial declarations by symbol identity.
        var unique = symbols
            .Distinct(SymbolEqualityComparer.Default)
            .Cast<INamedTypeSymbol>()
            .Where(ScopeDefinitionCodeGen.ImplementsScopeDefinition)
            .ToArray();

        var models = new List<ScopeDefinitionModel>(unique.Length);

        foreach (INamedTypeSymbol symbol in unique)
        {
            if (ScopeDefinitionCodeGen.TryCreateModel(
                    context,
                    compilation,
                    symbol,
                    reportDiagnostics: true,
                    out ScopeDefinitionModel model))
            {
                models.Add(model);
            }
        }

        ScopeDefinitionCodeGen.ReportLocalCollisions(context, models);
    }
}
```

`ScopeDefinitionGenerator` reports diagnostics only. It must not generate runtime catalogs. Layer and Module generators emit descriptors where they are actually consumed.

- [ ] **Step 5: Run generator tests**

```bash
dotnet test LayerBase.Test/LayerBase.Test.csproj \
    -c Release \
    --filter FullyQualifiedName~ScopeDefinitionGeneratorTests
```

Expected: shape/identity tests pass; generated-ID extraction tests may still fail because providers have not yet been upgraded.

- [ ] **Step 6: Commit**

```bash
git add \
    LayerBase.Generator/LayerBase.Generator/ScopeDefinitionCodeGen.cs \
    LayerBase.Generator/LayerBase.Generator/ScopeDefinitionGenerator.cs \
    LayerBase.Test/ScopeDefinitionGeneratorTests.cs

git commit -m "feat(generator): add stable scope identity validation"
```

---

# Task 3: Emit complete descriptors from generated Layers

**Files:**
- Modify: `LayerBase/Scope/IGeneratedScopeDefinitionProvider.cs`
- Modify: `LayerBase.Generator/LayerBase.Generator/LayerServiceGenerator.cs`
- Modify: `LayerBase.Test/LayerGeneratorContractTests.cs`
- Test: `LayerBase.Test/ScopeDefinitionGeneratorTests.cs`

**Interfaces:**
- Consumes: `ScopeDefinitionModel`, `GeneratedScopeDefinition`.
- Produces:
  - `GeneratedScopeDefinition[] IGeneratedScopeDefinitionProvider.__GetScopeDefinitions()`

- [ ] **Step 1: Change the provider contract**

Replace `LayerBase/Scope/IGeneratedScopeDefinitionProvider.cs` with:

```csharp
namespace LayerBase.Scope;

/// <summary>
/// Internal generated bridge from a Layer type to all Scope definitions
/// referenced by its generated registrations.
/// </summary>
public interface IGeneratedScopeDefinitionProvider
{
    GeneratedScopeDefinition[] __GetScopeDefinitions();
}
```

- [ ] **Step 2: Update existing generator contract sources**

In `LayerBase.Test/LayerGeneratorContractTests.cs`, replace every old Scope declaration shaped like:

```csharp
public readonly struct InventoryScope : IScopeDefinition
{
    public const int ScopeId = 71;
}
```

with:

```csharp
public sealed class InventoryScope : IScopeDefinition
{
    public ScopeOptions Options => ScopeOptions.Inline;
}
```

This preserves the old runtime behavior because custom Scope plans were previously hardcoded to `ScopeOptions.Inline`.

- [ ] **Step 3: Upgrade Layer scope collection**

In `LayerServiceGenerator`:

1. Keep collecting distinct owner Scope symbols from scoped Services and LayerTools.
2. Before emitting a Layer partial, convert each symbol with:

```csharp
if (!ScopeDefinitionCodeGen.TryCreateModel(
        spc,
        compilation,
        scopeType,
        reportDiagnostics:
            !SymbolEqualityComparer.Default.Equals(
                scopeType.ContainingAssembly,
                compilation.Assembly),
        out ScopeDefinitionModel model))
{
    continue;
}
```

Why `reportDiagnostics` is conditional:

- Local declarations are already diagnosed once by `ScopeDefinitionGenerator`.
- External Scope declarations are not visible to that generator in the current compilation, so the consuming generator must report inaccessible constructor/shape errors.

3. Sort emitted models by:
   - `ScopeId`
   - then `Identity` with `StringComparer.Ordinal`
   - then fully qualified type name.

4. Replace `EmitScopeDefinitionProvider` with code that emits a cached descriptor array:

```csharp
private static void EmitScopeDefinitionProvider(
    StringBuilder builder,
    IReadOnlyList<ScopeDefinitionModel> definitions)
{
    builder.AppendLine();
    builder.AppendLine(
        "    private static readonly global::LayerBase.Scope.GeneratedScopeDefinition[] __LayerBaseScopeDefinitions =");
    builder.AppendLine("    {");

    foreach (ScopeDefinitionModel definition in definitions)
    {
        builder.AppendLine(
            "        new global::LayerBase.Scope.GeneratedScopeDefinition(");
        builder.Append("            scopeId: ")
            .Append(definition.ScopeId)
            .AppendLine(",");
        builder.Append("            identity: \"")
            .Append(Escape(definition.Identity))
            .AppendLine("\",");
        builder.Append("            scopeType: typeof(")
            .Append(definition.FullyQualifiedTypeName)
            .AppendLine("),");
        builder.Append("            factory: static () => new ")
            .Append(definition.FullyQualifiedTypeName)
            .AppendLine("()),");
    }

    builder.AppendLine("    };");
    builder.AppendLine();
    builder.AppendLine(
        "    global::LayerBase.Scope.GeneratedScopeDefinition[] " +
        "global::LayerBase.Scope.IGeneratedScopeDefinitionProvider.__GetScopeDefinitions()");
    builder.AppendLine("    {");
    builder.AppendLine("        return __LayerBaseScopeDefinitions;");
    builder.AppendLine("    }");
}
```

The generated constructor arguments have fixed meanings:

- `scopeId`: compile-time SHA-256-derived routing number.
- `identity`: collision/audit identity.
- `scopeType`: exact type used for generic routing.
- `factory`: direct `new` expression; no runtime reflection.

5. Do not require the Scope class itself to be `partial`.

- [ ] **Step 4: Add generated-source assertions**

In `ScopeDefinitionGeneratorTests`, assert generated Layer source:

```csharp
StringAssert.Contains(
    "global::LayerBase.Scope.GeneratedScopeDefinition[]",
    generatedSource);

StringAssert.Contains(
    "factory: static () => new global::InventoryScope()",
    generatedSource);

StringAssert.DoesNotContain(
    "__GetScopeDefinitionTypes",
    generatedSource);
```

Also verify generated compilation has no errors.

- [ ] **Step 5: Run focused tests**

```bash
dotnet test LayerBase.Test/LayerBase.Test.csproj \
    -c Release \
    --filter "FullyQualifiedName~ScopeDefinitionGeneratorTests|FullyQualifiedName~LayerGeneratorContractTests"
```

Expected: all pass.

- [ ] **Step 6: Commit**

```bash
git add \
    LayerBase/Scope/IGeneratedScopeDefinitionProvider.cs \
    LayerBase.Generator/LayerBase.Generator/LayerServiceGenerator.cs \
    LayerBase.Test/LayerGeneratorContractTests.cs \
    LayerBase.Test/ScopeDefinitionGeneratorTests.cs

git commit -m "feat(generator): emit layer scope descriptors"
```

---

# Task 4: Carry generated Scope definitions through assembly Modules

**Files:**
- Modify: `LayerBase/Modules/AssemblyModule.cs`
- Modify: `LayerBase.Generator/LayerBase.Generator/AssemblyModuleGenerator.cs`
- Modify: `LayerBase.Test/AssemblyModuleGeneratorTests.cs`

**Interfaces:**
- Produces:
  - `AssemblyModuleManifest.ScopeDefinitions`
  - `ScopeDefinitionContributionPlan`
  - `CompositionContributions.ScopeDefinitions`
- Consumes: `GeneratedScopeDefinition` emitted by `AssemblyModuleGenerator`.

- [ ] **Step 1: Add failing Module generator assertions**

Extend `AssemblyModuleGeneratorTests` with a module source containing:

```csharp
[ScopeIdentity("game.inventory")]
public sealed class InventoryScope : IScopeDefinition
{
    public ScopeOptions Options => ScopeOptions.Worker(tickRateHz: 20);
}
```

and a generated contribution targeting `[Scope<InventoryScope>]`.

Assert generated module source contains:

```text
new global::LayerBase.Scope.GeneratedScopeDefinition(
scopeType: typeof(global::InventoryScope)
factory: static () => new global::InventoryScope()
```

Compile the generated output and instantiate the generated module. Assert:

```csharp
Assert.That(module.Manifest.ScopeDefinitions, Has.Count.EqualTo(1));
Assert.That(
    module.Manifest.ScopeDefinitions[0].ScopeType,
    Is.EqualTo(typeof(InventoryScope)));
```

- [ ] **Step 2: Run the test and verify it fails**

```bash
dotnet test LayerBase.Test/LayerBase.Test.csproj \
    -c Release \
    --filter FullyQualifiedName~AssemblyModuleGeneratorTests
```

Expected: `ScopeDefinitions` is missing.

- [ ] **Step 3: Extend `AssemblyModuleManifest` without breaking existing callers**

In `LayerBase/Modules/AssemblyModule.cs`:

1. Add `GeneratedScopeDefinition[] scopeDefinitions` only to a new longest constructor.
2. Keep every existing constructor and forward it with `Array.Empty<GeneratedScopeDefinition>()`.
3. The longest constructor must assign a read-only copy:

```csharp
public AssemblyModuleManifest(
    AssemblyModuleId moduleId,
    ServiceContribution[] services,
    ContextContribution[] contexts,
    LocalCallContribution[] localCalls,
    EventHandlerContribution[] eventHandlers,
    LayerToolContribution[] tools,
    EventContribution[] events,
    GeneratedScopeDefinition[] scopeDefinitions)
{
    ModuleId = moduleId;
    Services = Array.AsReadOnly(
        (services ?? Array.Empty<ServiceContribution>()).ToArray());
    Contexts = Array.AsReadOnly(
        (contexts ?? Array.Empty<ContextContribution>()).ToArray());
    LocalCalls = Array.AsReadOnly(
        (localCalls ?? Array.Empty<LocalCallContribution>()).ToArray());
    EventHandlers = Array.AsReadOnly(
        (eventHandlers ?? Array.Empty<EventHandlerContribution>()).ToArray());
    Tools = Array.AsReadOnly(
        (tools ?? Array.Empty<LayerToolContribution>()).ToArray());
    Events = Array.AsReadOnly(
        (events ?? Array.Empty<EventContribution>()).ToArray());
    ScopeDefinitions = Array.AsReadOnly(
        (scopeDefinitions ?? Array.Empty<GeneratedScopeDefinition>()).ToArray());
}

public IReadOnlyList<GeneratedScopeDefinition> ScopeDefinitions { get; }
```

4. Add an overload matching the current generated six-contribution form plus Scope definitions:

```csharp
public AssemblyModuleManifest(
    AssemblyModuleId moduleId,
    ServiceContribution[] services,
    ContextContribution[] contexts,
    LocalCallContribution[] localCalls,
    EventHandlerContribution[] eventHandlers,
    LayerToolContribution[] tools,
    GeneratedScopeDefinition[] scopeDefinitions)
    : this(
        moduleId,
        services,
        contexts,
        localCalls,
        eventHandlers,
        tools,
        Array.Empty<EventContribution>(),
        scopeDefinitions)
{
}
```

- [ ] **Step 4: Add the composition plan type**

In `AssemblyModule.cs` add:

```csharp
internal readonly struct ScopeDefinitionContributionPlan
{
    public ScopeDefinitionContributionPlan(
        AssemblyModuleId moduleId,
        GeneratedScopeDefinition definition)
    {
        ModuleId = moduleId;
        Definition = definition;
    }

    public AssemblyModuleId ModuleId { get; }

    public GeneratedScopeDefinition Definition { get; }
}
```

Add `ScopeDefinitionContributionPlan[] scopeDefinitions` to `CompositionContributions` and expose it as `ScopeDefinitions`. Update `CompositionContributions.Empty`.

In `AssemblyModuleComposer.Compose`:

```csharp
var scopeDefinitionPlans =
    new List<ScopeDefinitionContributionPlan>();

foreach (IAssemblyModule module in modules.OrderBy(static module => module.Id))
{
    AssemblyModuleManifest manifest = module.Manifest
        ?? throw new InvalidOperationException(
            $"Assembly module '{module.Id}' returned a null manifest.");

    foreach (GeneratedScopeDefinition definition in
             manifest.ScopeDefinitions
                 .OrderBy(static definition => definition.ScopeId)
                 .ThenBy(
                     static definition => definition.Identity,
                     StringComparer.Ordinal))
    {
        scopeDefinitionPlans.Add(
            new ScopeDefinitionContributionPlan(
                moduleId: module.Id,
                definition: definition));
    }

    // Existing contribution loops remain unchanged.
}
```

Pass `scopeDefinitionPlans.ToArray()` into `CompositionContributions`.

- [ ] **Step 5: Emit Scope descriptors into generated manifests**

In `AssemblyModuleGenerator`:

1. Retain the `INamedTypeSymbol` owner Scope in contribution models. Do not reduce it to only a string before descriptor generation.
2. Collect every distinct custom Scope referenced by generated Services, Contexts, LocalCalls, EventHandlers, Events, and LayerTools.
3. Exclude `MainScope`; runtime seeds it as a built-in definition.
4. Convert symbols through `ScopeDefinitionCodeGen.TryCreateModel`.
5. Sort definitions by ID/identity/type.
6. Add `AppendScopeDefinitionArray`.
7. Refactor `AppendLayerToolArray` so it writes a trailing comma instead of closing `);`.
8. Emit the Scope array after tools and explicitly close the manifest constructor.

Required emitted shape:

```csharp
new global::LayerBase.Modules.AssemblyModuleManifest(
    moduleId,
    services,
    contexts,
    localCalls,
    eventHandlers,
    tools,
    new global::LayerBase.Scope.GeneratedScopeDefinition[]
    {
        new global::LayerBase.Scope.GeneratedScopeDefinition(
            scopeId: 123456,
            identity: "scope-key:game.inventory",
            scopeType: typeof(global::InventoryScope),
            factory: static () => new global::InventoryScope()),
    });
```

- [ ] **Step 6: Run Module tests**

```bash
dotnet test LayerBase.Test/LayerBase.Test.csproj \
    -c Release \
    --filter FullyQualifiedName~AssemblyModuleGeneratorTests
```

Expected: all pass.

- [ ] **Step 7: Commit**

```bash
git add \
    LayerBase/Modules/AssemblyModule.cs \
    LayerBase.Generator/LayerBase.Generator/AssemblyModuleGenerator.cs \
    LayerBase.Test/AssemblyModuleGeneratorTests.cs

git commit -m "feat(modules): carry generated scope definitions"
```

---

# Task 5: Replace reflective runtime resolution with a conflict-validating registry

**Files:**
- Create: `LayerBase/Scope/ScopeDefinitionRegistry.cs`
- Create: `LayerBase.Test/ScopeDefinitionRegistryTests.cs`
- Modify: `LayerBase/Scope/RuntimeCompositionPlan.cs`
- Modify: `LayerBase/Scope/ScopeRuntimeModel.cs`

**Interfaces:**
- Produces:
  - `ScopeDefinitionRegistry.Add(...)`
  - `ScopeDefinitionRegistry.Require(Type)`
  - `ScopeDefinitionRegistry.OrderedDefinitions`
  - `ScopeDefinitionConflictException`
- Consumes:
  - Layer provider descriptors
  - Module `ScopeDefinitionContributionPlan`
  - `IScopeDefinition.Options`

- [ ] **Step 1: Add registry tests**

Create `LayerBase.Test/ScopeDefinitionRegistryTests.cs`:

```csharp
using LayerBase.Scope;

namespace LayerBase.Test;

[TestFixture]
public sealed class ScopeDefinitionRegistryTests
{
    [Test]
    public void Identical_duplicate_is_deduplicated()
    {
        var registry = new ScopeDefinitionRegistry();
        GeneratedScopeDefinition definition = Create(
            scopeId: 11,
            identity: "scope-key:game.inventory",
            scopeType: typeof(InventoryScope),
            factory: static () => new InventoryScope());

        registry.Add(definition, source: "layer:A");
        registry.Add(definition, source: "module:B");

        Assert.That(
            registry.OrderedDefinitions.Count(
                item => item.ScopeType == typeof(InventoryScope)),
            Is.EqualTo(1));
    }

    [Test]
    public void Same_id_with_different_identity_is_rejected()
    {
        var registry = new ScopeDefinitionRegistry();

        registry.Add(
            Create(
                11,
                "scope-key:game.inventory",
                typeof(InventoryScope),
                static () => new InventoryScope()),
            source: "layer:A");

        Assert.That(
            () => registry.Add(
                Create(
                    11,
                    "scope-key:game.payment",
                    typeof(PaymentScope),
                    static () => new PaymentScope()),
                source: "module:B"),
            Throws.TypeOf<ScopeDefinitionConflictException>());
    }

    [Test]
    public void Same_identity_with_different_type_is_rejected()
    {
        var registry = new ScopeDefinitionRegistry();

        registry.Add(
            Create(
                11,
                "scope-key:game.inventory",
                typeof(InventoryScope),
                static () => new InventoryScope()),
            source: "layer:A");

        Assert.That(
            () => registry.Add(
                Create(
                    12,
                    "scope-key:game.inventory",
                    typeof(PaymentScope),
                    static () => new PaymentScope()),
                source: "module:B"),
            Throws.TypeOf<ScopeDefinitionConflictException>());
    }

    [Test]
    public void Same_type_with_different_id_is_rejected()
    {
        var registry = new ScopeDefinitionRegistry();

        registry.Add(
            Create(
                11,
                "scope-key:game.inventory",
                typeof(InventoryScope),
                static () => new InventoryScope()),
            source: "layer:A");

        Assert.That(
            () => registry.Add(
                Create(
                    12,
                    "scope-key:game.inventory",
                    typeof(InventoryScope),
                    static () => new InventoryScope()),
                source: "module:B"),
            Throws.TypeOf<ScopeDefinitionConflictException>());
    }

    private static GeneratedScopeDefinition Create(
        int scopeId,
        string identity,
        Type scopeType,
        ScopeDefinitionFactory factory)
    {
        return new GeneratedScopeDefinition(
            scopeId: scopeId,
            identity: identity,
            scopeType: scopeType,
            factory: factory);
    }

    private sealed class InventoryScope : IScopeDefinition
    {
        public ScopeOptions Options => ScopeOptions.Inline;
    }

    private sealed class PaymentScope : IScopeDefinition
    {
        public ScopeOptions Options => ScopeOptions.Inline;
    }
}
```

The `source` parameter is diagnostic provenance only. It must identify the Layer or Module that supplied a conflicting descriptor.

- [ ] **Step 2: Run tests and verify registry is missing**

```bash
dotnet test LayerBase.Test/LayerBase.Test.csproj \
    -c Release \
    --filter FullyQualifiedName~ScopeDefinitionRegistryTests
```

Expected: compilation fails.

- [ ] **Step 3: Implement `ScopeDefinitionRegistry`**

Create `LayerBase/Scope/ScopeDefinitionRegistry.cs`:

```csharp
namespace LayerBase.Scope;

internal sealed class ScopeDefinitionRegistry
{
    private readonly Dictionary<Type, Entry> _byType = new();
    private readonly Dictionary<int, Entry> _byId = new();
    private readonly Dictionary<string, Entry> _byIdentity =
        new(StringComparer.Ordinal);

    public ScopeDefinitionRegistry()
    {
        Add(GeneratedScopeDefinition.Main, source: "framework:MainScope");
    }

    public IEnumerable<GeneratedScopeDefinition> OrderedDefinitions =>
        _byId.Values
            .OrderBy(static entry => entry.Definition.ScopeId)
            .ThenBy(
                static entry => entry.Definition.Identity,
                StringComparer.Ordinal)
            .Select(static entry => entry.Definition);

    public void Add(
        GeneratedScopeDefinition definition,
        string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException(
                "Scope definition source is required.",
                nameof(source));

        if (_byType.TryGetValue(definition.ScopeType, out Entry byType))
        {
            if (IsSame(byType.Definition, definition))
                return;

            throw Conflict(
                "The same Scope type was registered with different identity or ID.",
                byType,
                definition,
                source);
        }

        if (_byIdentity.TryGetValue(definition.Identity, out Entry byIdentity))
        {
            throw Conflict(
                "Different Scope types use the same identity.",
                byIdentity,
                definition,
                source);
        }

        if (_byId.TryGetValue(definition.ScopeId, out Entry byId))
        {
            throw Conflict(
                "Different Scope identities use the same Scope ID.",
                byId,
                definition,
                source);
        }

        var entry = new Entry(definition, source);
        _byType.Add(definition.ScopeType, entry);
        _byIdentity.Add(definition.Identity, entry);
        _byId.Add(definition.ScopeId, entry);
    }

    public GeneratedScopeDefinition Require(Type scopeType)
    {
        if (scopeType == null)
            throw new ArgumentNullException(nameof(scopeType));

        if (_byType.TryGetValue(scopeType, out Entry entry))
            return entry.Definition;

        throw new InvalidOperationException(
            $"Scope type '{scopeType.FullName}' has no generated definition. " +
            "Ensure the owning Layer or AssemblyModule was generated and installed.");
    }

    private static bool IsSame(
        GeneratedScopeDefinition left,
        GeneratedScopeDefinition right)
    {
        return left.ScopeType == right.ScopeType &&
               left.ScopeId == right.ScopeId &&
               string.Equals(
                   left.Identity,
                   right.Identity,
                   StringComparison.Ordinal);
    }

    private static ScopeDefinitionConflictException Conflict(
        string reason,
        Entry existing,
        GeneratedScopeDefinition incoming,
        string incomingSource)
    {
        return new ScopeDefinitionConflictException(
            reason +
            $" Existing: type='{existing.Definition.ScopeType.FullName}', " +
            $"identity='{existing.Definition.Identity}', " +
            $"id={existing.Definition.ScopeId}, source='{existing.Source}'. " +
            $"Incoming: type='{incoming.ScopeType.FullName}', " +
            $"identity='{incoming.Identity}', id={incoming.ScopeId}, " +
            $"source='{incomingSource}'.");
    }

    private readonly struct Entry
    {
        public Entry(
            GeneratedScopeDefinition definition,
            string source)
        {
            Definition = definition;
            Source = source;
        }

        public GeneratedScopeDefinition Definition { get; }

        public string Source { get; }
    }
}

internal sealed class ScopeDefinitionConflictException :
    InvalidOperationException
{
    public ScopeDefinitionConflictException(string message)
        : base(message)
    {
    }
}
```

- [ ] **Step 4: Make execution plans own the definition object**

In `ScopeRuntimeModel.cs`, replace the `ScopeExecutionPlan` constructor with:

```csharp
internal sealed class ScopeExecutionPlan
{
    public ScopeExecutionPlan(
        ScopeDescriptor descriptor,
        IScopeDefinition definition,
        LayerProviderRuntime[]? layerProviders = null,
        ScopeLayerSlice[]? layerSlices = null,
        ScopeLifecyclePlan? lifecyclePlan = null)
    {
        Descriptor = descriptor;
        Definition = definition
            ?? throw new ArgumentNullException(nameof(definition));
        Options = definition.Options;
        LayerProviders = layerProviders ?? LayerProviderRuntime.Empty;
        LayerSlices = layerSlices ?? Array.Empty<ScopeLayerSlice>();
        LifecyclePlan = lifecyclePlan ?? ScopeLifecyclePlan.Empty;
    }

    public ScopeDescriptor Descriptor { get; }

    public IScopeDefinition Definition { get; }

    public ScopeOptions Options { get; }

    public LayerProviderRuntime[] LayerProviders { get; }

    public ScopeLayerSlice[] LayerSlices { get; }

    public ScopeLifecyclePlan LifecyclePlan { get; }

    public static ScopeExecutionPlan CreateMain()
    {
        var definition = new MainScope();

        return new ScopeExecutionPlan(
            descriptor: new ScopeDescriptor(
                scopeId: ScopeDefinitionIds.Main,
                name: nameof(MainScope),
                scopeType: typeof(MainScope),
                identity: ScopeDefinitionIds.MainIdentity),
            definition: definition);
    }
}
```

Extend `ScopeDescriptor` with required `identity`:

```csharp
public ScopeDescriptor(
    int scopeId,
    string name,
    Type scopeType,
    string identity)
{
    if (scopeId < 0)
        throw new ArgumentOutOfRangeException(nameof(scopeId));

    ScopeId = scopeId;
    Name = string.IsNullOrWhiteSpace(name)
        ? throw new ArgumentException(
            "Scope name is required.",
            nameof(name))
        : name;
    ScopeType = scopeType
        ?? throw new ArgumentNullException(nameof(scopeType));
    Identity = string.IsNullOrWhiteSpace(identity)
        ? throw new ArgumentException(
            "Scope identity is required.",
            nameof(identity))
        : identity;
}

public string Identity { get; }
```

Update every `new ScopeDescriptor(...)` compile error to provide the generated identity. Do not invent an identity from `Type.FullName` at runtime.

- [ ] **Step 5: Rewrite runtime composition**

In `RuntimeCompositionPlan.Build`:

```csharp
var scopeDefinitions = new ScopeDefinitionRegistry();

CollectLocalScopeDefinitions(
    pushedLayers: pushedLayers,
    registry: scopeDefinitions);

CollectModuleScopeDefinitions(
    contributions: contributions,
    registry: scopeDefinitions);
```

Replace `CollectLocalScopeTypes` with:

```csharp
private static void CollectLocalScopeDefinitions(
    IReadOnlyList<Layer> pushedLayers,
    ScopeDefinitionRegistry registry)
{
    foreach (Layer layer in pushedLayers)
    {
        if (layer is not IGeneratedScopeDefinitionProvider provider)
            continue;

        foreach (GeneratedScopeDefinition definition in
                 provider.__GetScopeDefinitions())
        {
            registry.Add(
                definition,
                source:
                    $"layer:{layer.GetType().AssemblyQualifiedName}");
        }
    }
}
```

Add:

```csharp
private static void CollectModuleScopeDefinitions(
    CompositionContributions contributions,
    ScopeDefinitionRegistry registry)
{
    foreach (ScopeDefinitionContributionPlan plan in
             contributions.ScopeDefinitions)
    {
        registry.Add(
            plan.Definition,
            source: $"module:{plan.ModuleId}");
    }
}
```

Replace every `Dictionary<Type, int> scopeIdsByType` parameter with `ScopeDefinitionRegistry scopeDefinitions`.

Replace `ResolveScopeId` with:

```csharp
private static int ResolveScopeId(
    Type scopeType,
    ScopeDefinitionRegistry scopeDefinitions)
{
    return scopeDefinitions.Require(scopeType).ScopeId;
}
```

Delete all code containing:

```csharp
scopeType.GetField("ScopeId")
ScopeDefinitionIds.Resolve(scopeType)
scopeIdsByType.ContainsValue(...)
```

Replace `BuildScopeExecutionPlans` with:

```csharp
private static ScopeExecutionPlan[] BuildScopeExecutionPlans(
    LayerBuildPlan[] layerPlans,
    ScopeDefinitionRegistry scopeDefinitions)
{
    int[] layerIndexes = layerPlans
        .OrderBy(static layer => layer.LayerIndex)
        .Select(static layer => layer.LayerIndex)
        .ToArray();

    return scopeDefinitions.OrderedDefinitions
        .Select(definition =>
        {
            IScopeDefinition instance =
                definition.CreateDefinition();

            return new ScopeExecutionPlan(
                descriptor: new ScopeDescriptor(
                    scopeId: definition.ScopeId,
                    name: definition.ScopeType.Name,
                    scopeType: definition.ScopeType,
                    identity: definition.Identity),
                definition: instance,
                layerSlices: layerIndexes
                    .Select(static layerIndex =>
                        new ScopeLayerSlice(layerIndex))
                    .ToArray(),
                lifecyclePlan:
                    ScopeLifecyclePlan.EmptyForLayerIndexes(
                        layerIndexes));
        })
        .ToArray();
}
```

This is the only place custom Scope definition factories are invoked during composition.

- [ ] **Step 6: Run registry and composition tests**

```bash
dotnet test LayerBase.Test/LayerBase.Test.csproj \
    -c Release \
    --filter "FullyQualifiedName~ScopeDefinitionRegistryTests|FullyQualifiedName~ScopeCompositionPlanTests"
```

Expected: all pass after updating any test fixture constructor calls with an identity and definition object.

- [ ] **Step 7: Commit**

```bash
git add \
    LayerBase/Scope/ScopeDefinitionRegistry.cs \
    LayerBase/Scope/RuntimeCompositionPlan.cs \
    LayerBase/Scope/ScopeRuntimeModel.cs \
    LayerBase.Test/ScopeDefinitionRegistryTests.cs \
    LayerBase.Test/ScopeCompositionPlanTests.cs

git commit -m "refactor(scope): compose generated scope definitions"
```

---

# Task 6: Prove options and factories flow end to end

**Files:**
- Create: `LayerBase.Test/ScopeDefinitionRuntimeIntegrationTests.cs`
- Modify as required by compile errors:
  - `LayerBase/Scope/ScopeRuntimeHost.cs`
  - `LayerBase/Scope/ScopeRuntime.cs`
  - `LayerBase/Scope/LayerScopeExtensions.cs`
  - `LayerBase/Application/LayerRuntime.cs`

**Interfaces:**
- Consumes: `ScopeExecutionPlan.Definition`, `ScopeExecutionPlan.Options`.
- Produces: No new public API unless an existing diagnostic accessor must expose descriptor identity.

- [ ] **Step 1: Add end-to-end tests**

Create `LayerBase.Test/ScopeDefinitionRuntimeIntegrationTests.cs` with four tests.

### A. Options come from the Scope object

```csharp
[Test]
public void Custom_scope_runtime_uses_definition_options()
{
    using LayerRuntime runtime = LayerHub.CreateLayers()
        .Push(new InventoryLayer())
        .Build();

    Assert.That(
        runtime.TryGetScope<InventoryScope>(out ScopeRef<InventoryScope> scope),
        Is.True);

    ScopeRuntime internalRuntime =
        runtime.ScopeHost!.RequireScope(scope.Address.ScopeId);

    Assert.Multiple(() =>
    {
        Assert.That(
            internalRuntime.Options.Threading,
            Is.EqualTo(ScopeThreadingMode.Worker));
        Assert.That(
            internalRuntime.Options.Clock,
            Is.EqualTo(ScopeClockMode.FixedRate));
        Assert.That(
            internalRuntime.Options.TickRateHz,
            Is.EqualTo(23));
        Assert.That(
            internalRuntime.Options.FaultPolicy,
            Is.EqualTo(ScopeFaultPolicy.FailScope));
    });
}
```

### B. Duplicate Layer registrations create one definition object

```csharp
[Test]
public void Scope_factory_runs_once_per_runtime()
{
    CountingScope.Reset();

    using LayerRuntime runtime = LayerHub.CreateLayers()
        .Push(new FirstCountingLayer())
        .Push(new SecondCountingLayer())
        .Build();

    Assert.That(CountingScope.ConstructorCount, Is.EqualTo(1));
}
```

### C. Separate runtimes create separate definition objects

```csharp
[Test]
public void Each_runtime_receives_its_own_scope_definition_object()
{
    CountingScope.Reset();

    using LayerRuntime first = LayerHub.CreateLayers()
        .Push(new FirstCountingLayer())
        .Build();

    using LayerRuntime second = LayerHub.CreateLayers()
        .Push(new FirstCountingLayer())
        .Build();

    Assert.That(CountingScope.ConstructorCount, Is.EqualTo(2));
}
```

### D. Assembly Module-only registration carries its descriptor

Build/install a generated test module whose contribution targets a custom Scope and assert the runtime contains that Scope without any pushed Layer provider supplying the definition.

Required fixture Scope:

```csharp
[ScopeIdentity("layerbase.tests.inventory")]
public sealed class InventoryScope : IScopeDefinition
{
    public ScopeOptions Options { get; } = ScopeOptions.Worker(
        tickRateHz: 23,
        faultPolicy: ScopeFaultPolicy.FailScope);
}
```

Required counting Scope:

```csharp
public sealed class CountingScope : IScopeDefinition
{
    private static int _constructorCount;

    public CountingScope()
    {
        Interlocked.Increment(ref _constructorCount);
    }

    public static int ConstructorCount =>
        Volatile.Read(ref _constructorCount);

    public static void Reset()
    {
        Volatile.Write(ref _constructorCount, 0);
    }

    public ScopeOptions Options => ScopeOptions.Inline;
}
```

- [ ] **Step 2: Run the integration fixture**

```bash
dotnet test LayerBase.Test/LayerBase.Test.csproj \
    -c Release \
    --filter FullyQualifiedName~ScopeDefinitionRuntimeIntegrationTests
```

Expected: failures identify any remaining code path that still supplies hardcoded options or lacks module descriptors.

- [ ] **Step 3: Route runtime construction only through plan options**

Inspect `ScopeRuntimeHost` and `ScopeRuntime`:

```bash
rg -n "ScopeOptions\.(Main|Inline|Worker)|new ScopeOptions" \
    LayerBase/Scope \
    LayerBase/Application
```

Rules:

- `ScopeExecutionPlan.CreateMain` may use `new MainScope().Options`.
- Tests may construct explicit options.
- `ScopeRuntimeHost` and `ScopeRuntime` must consume `plan.Options`.
- Runtime composition must not select `Inline` based on “non-main”.
- Do not overwrite `plan.Options.EcsRuntime` with a global default after plan creation.

- [ ] **Step 4: Run the fixture again**

```bash
dotnet test LayerBase.Test/LayerBase.Test.csproj \
    -c Release \
    --filter FullyQualifiedName~ScopeDefinitionRuntimeIntegrationTests
```

Expected: all four tests pass.

- [ ] **Step 5: Commit**

```bash
git add \
    LayerBase.Test/ScopeDefinitionRuntimeIntegrationTests.cs \
    LayerBase/Scope/ScopeRuntimeHost.cs \
    LayerBase/Scope/ScopeRuntime.cs \
    LayerBase/Scope/LayerScopeExtensions.cs \
    LayerBase/Application/LayerRuntime.cs

git commit -m "test(scope): verify generated definitions at runtime"
```

Only stage files that actually changed.

---

# Task 7: Migrate every old Scope declaration and ID-dependent test

**Files:**
- Modify: `LayerBase.Usage/BusinessRetailContracts.cs`
- Modify: every result from:
  - `rg -l ': IScopeDefinition' LayerBase LayerBase.Test LayerBase.Usage`
  - `rg -l 'ScopeId' LayerBase LayerBase.Test LayerBase.Usage`

**Interfaces:**
- Consumes: New class/object Scope API.
- Produces: Repository-wide removal of manually assigned custom IDs.

- [ ] **Step 1: Generate the migration inventory**

```bash
rg -n \
    'readonly struct .*Scope|struct .*: IScopeDefinition|const int ScopeId|static int ScopeId|__GetScopeDefinitionTypes|GetField\("ScopeId"|ScopeDefinitionIds\.Resolve' \
    LayerBase \
    LayerBase.Test \
    LayerBase.Usage
```

Save the output in the task log. Every hit must be classified and removed or explicitly retained as `MainScope.ScopeId`.

- [ ] **Step 2: Apply the mechanical declaration migration**

For every old custom Scope:

```csharp
public readonly struct InventoryScope : IScopeDefinition
{
    public const int ScopeId = 71;
}
```

replace it with:

```csharp
public sealed class InventoryScope : IScopeDefinition
{
    // Inline preserves the previous custom-Scope default used by
    // RuntimeCompositionPlan before this migration.
    public ScopeOptions Options => ScopeOptions.Inline;
}
```

For tests that intentionally exercise worker behavior, use the exact former test setup:

```csharp
public sealed class WorkerScope : IScopeDefinition
{
    public ScopeOptions Options =>
        ScopeOptions.Worker(tickRateHz: 60);
}
```

Do not add `[ScopeIdentity]` everywhere. Add it only to public sample/business Scope types whose identity is expected to survive refactors, for example:

```csharp
[ScopeIdentity("layerbase.business.inventory")]
public sealed class BusinessInventoryScope : IScopeDefinition
{
    public ScopeOptions Options => ScopeOptions.Inline;
}
```

- [ ] **Step 3: Remove hardcoded custom ID assertions**

Replace:

```csharp
Assert.That(scope.Address.ScopeId, Is.EqualTo(71));
```

with behavior-based assertions:

```csharp
Assert.That(
    runtime.TryGetScope<InventoryScope>(
        out ScopeRef<InventoryScope> scope),
    Is.True);

Assert.That(scope.Address.ScopeId, Is.GreaterThan(0));
```

When a test needs to send to the ID, obtain it from the resolved Scope:

```csharp
int scopeId = scope.Address.ScopeId;
```

When a stability test needs a fixed identity, compare two generated/runtime builds of the same Scope identity; do not copy the generated numeric literal into source.

Keep:

```csharp
Assert.That(runtime.Main.Address.ScopeId, Is.EqualTo(MainScope.ScopeId));
```

because ID zero is a framework constant, not a user-authored custom ID.

- [ ] **Step 4: Update hand-written fake providers**

Any test fake implementing `IGeneratedScopeDefinitionProvider` must now return:

```csharp
GeneratedScopeDefinition[]
    IGeneratedScopeDefinitionProvider.__GetScopeDefinitions()
{
    return
    [
        new GeneratedScopeDefinition(
            scopeId: 1001,
            identity: "scope-key:layerbase.tests.fake",
            scopeType: typeof(FakeScope),
            factory: static () => new FakeScope())
    ];
}
```

Only test fakes may use an explicit descriptor ID. Production Scope types must use generated IDs.

- [ ] **Step 5: Verify forbidden patterns are gone**

```bash
rg -n \
    'const int ScopeId|static int ScopeId|__GetScopeDefinitionTypes|GetField\("ScopeId"|ScopeDefinitionIds\.Resolve' \
    LayerBase \
    LayerBase.Test \
    LayerBase.Usage
```

Expected output:

```text
Only LayerBase/Scope/ScopeDefinitions.cs may contain MainScope.ScopeId.
No custom Scope, provider, or runtime reflection hits remain.
```

Also run:

```bash
rg -n 'struct .*IScopeDefinition|readonly struct .*Scope' \
    LayerBase \
    LayerBase.Test \
    LayerBase.Usage
```

Expected: no `IScopeDefinition` value types.

- [ ] **Step 6: Build to find complete migration surface**

```bash
dotnet build LayerBase.sln -c Release
```

Fix every compiler error caused by:

- missing `Options`;
- generic `class` constraint;
- removed `__GetScopeDefinitionTypes`;
- changed `ScopeExecutionPlan`;
- changed `ScopeDescriptor`;
- changed Module manifest constructor.

Do not reintroduce compatibility reflection.

- [ ] **Step 7: Run Scope-focused suites**

```bash
dotnet test LayerBase.Test/LayerBase.Test.csproj \
    -c Release \
    --filter "FullyQualifiedName~Scope|FullyQualifiedName~RuntimeIsolation|FullyQualifiedName~LayerToolRegistry|FullyQualifiedName~AssemblyModule"
```

Expected: all pass.

- [ ] **Step 8: Commit**

```bash
git add \
    LayerBase \
    LayerBase.Test \
    LayerBase.Usage

git commit -m "refactor(scope): migrate custom scope declarations"
```

Before committing, inspect:

```bash
git diff --check
git diff --stat
git diff --name-only
```

Remove unrelated formatting changes.

---

# Task 8: Add architecture fences and documentation

**Files:**
- Modify: `LayerBase.Test/ScopeArchitectureAcceptanceTests.cs`
- Modify: `LayerBase.Usage/BusinessRetailContracts.cs`
- Modify: the primary Scope documentation or README section that currently shows manual `ScopeId`.

**Interfaces:**
- Produces: Regression fences against reflection/manual IDs and a canonical usage example.

- [ ] **Step 1: Add architecture acceptance tests**

Add to `ScopeArchitectureAcceptanceTests`:

```csharp
[Test]
public void Scope_definition_api_is_object_based_and_options_are_public()
{
    Assert.That(typeof(IScopeDefinition).IsInterface, Is.True);

    PropertyInfo? options = typeof(IScopeDefinition)
        .GetProperty(nameof(IScopeDefinition.Options));

    Assert.That(options, Is.Not.Null);
    Assert.That(options!.PropertyType, Is.EqualTo(typeof(ScopeOptions)));
    Assert.That(typeof(ScopeOptions).IsPublic, Is.True);
    Assert.That(typeof(ScopeThreadingMode).IsPublic, Is.True);
    Assert.That(typeof(ScopeClockMode).IsPublic, Is.True);
}

[Test]
public void Custom_scope_definition_does_not_require_static_scope_id()
{
    Assert.That(
        typeof(IScopeDefinition).GetMember("ScopeId"),
        Is.Empty);

    Assert.That(
        typeof(GeneratedScopeDefinition)
            .GetMethod(nameof(GeneratedScopeDefinition.CreateDefinition)),
        Is.Not.Null);
}

[Test]
public void Generated_scope_provider_returns_complete_descriptors()
{
    MethodInfo method = typeof(IGeneratedScopeDefinitionProvider)
        .GetMethod(
            nameof(IGeneratedScopeDefinitionProvider.__GetScopeDefinitions))!;

    Assert.That(
        method.ReturnType,
        Is.EqualTo(typeof(GeneratedScopeDefinition[])));
}
```

Add a repository-source fence only if existing test infrastructure already exposes the repository root. The required forbidden strings are:

```text
GetField("ScopeId")
ScopeDefinitionIds.Resolve(
__GetScopeDefinitionTypes
Activator.CreateInstance
```

Do not ban all uses of `Type` or reflection in the assembly; architecture tests already legitimately use reflection for public API inspection.

- [ ] **Step 2: Update the usage example**

`LayerBase.Usage/BusinessRetailContracts.cs` must show:

```csharp
[ScopeIdentity("layerbase.business.inventory")]
public sealed class BusinessInventoryScope : IScopeDefinition
{
    public ScopeOptions Options { get; } =
        ScopeOptions.Worker(
            tickRateHz: 60,
            faultPolicy: ScopeFaultPolicy.ReportAndContinue);
}
```

Comments must explain:

- `ScopeIdentity` is optional.
- Without it, identity derives from assembly + full metadata type name.
- `tickRateHz` controls fixed worker updates per second.
- `faultPolicy` controls unhandled Scope failures.
- The generator supplies the ID and factory.

- [ ] **Step 3: Document migration**

The documentation must include this exact before/after distinction:

```csharp
// Before: remove this form.
public readonly struct InventoryScope : IScopeDefinition
{
    public const int ScopeId = 71;
}

// After: zero manual ID assignment.
public sealed class InventoryScope : IScopeDefinition
{
    public ScopeOptions Options => ScopeOptions.Inline;
}
```

Document the two identity modes:

```text
Default:
scope:{assembly-simple-name}:{fully-qualified-metadata-type-name}

Override:
scope-key:{ScopeIdentityAttribute.Value}
```

Document rename behavior:

| Change | Default identity | `[ScopeIdentity]` |
|---|---:|---:|
| Add/delete unrelated Scope | unchanged | unchanged |
| Move source file | unchanged | unchanged |
| Rename class | changes | unchanged |
| Change namespace | changes | unchanged |
| Change assembly | changes | unchanged |

- [ ] **Step 4: Run acceptance tests**

```bash
dotnet test LayerBase.Test/LayerBase.Test.csproj \
    -c Release \
    --filter FullyQualifiedName~ScopeArchitectureAcceptanceTests
```

Expected: all pass.

- [ ] **Step 5: Commit**

```bash
git add \
    LayerBase.Test/ScopeArchitectureAcceptanceTests.cs \
    LayerBase.Usage/BusinessRetailContracts.cs \
    README.md \
    docs

git commit -m "docs(scope): document generated stable identities"
```

Only stage documentation files actually changed.

---

# Task 9: Full verification and review gate

**Files:** No planned source changes. Fix only defects proven by verification.

- [ ] **Step 1: Restore and build both configurations**

```bash
dotnet restore LayerBase.sln
dotnet build LayerBase.sln -c Debug --no-restore
dotnet build LayerBase.sln -c Release --no-restore
```

Expected: zero warnings newly introduced by this branch and zero errors.

- [ ] **Step 2: Run all tests**

```bash
dotnet test LayerBase.Test/LayerBase.Test.csproj \
    -c Debug \
    --no-build

dotnet test LayerBase.Test/LayerBase.Test.csproj \
    -c Release \
    --no-build
```

Expected: all NUnit tests pass in both configurations.

- [ ] **Step 3: Run generator-specific tests separately for readable failure output**

```bash
dotnet test LayerBase.Test/LayerBase.Test.csproj \
    -c Release \
    --filter "FullyQualifiedName~Generator"
```

Expected: all generator tests pass.

- [ ] **Step 4: Run the forbidden-pattern audit**

```bash
set -euo pipefail

CUSTOM_SCOPE_ID_HITS="$(
    rg -n \
        'const int ScopeId|static int ScopeId' \
        LayerBase \
        LayerBase.Test \
        LayerBase.Usage \
    | grep -v 'MainScope' || true
)"

REFLECTION_HITS="$(
    rg -n \
        'GetField\("ScopeId"|ScopeDefinitionIds\.Resolve|__GetScopeDefinitionTypes|Activator\.CreateInstance' \
        LayerBase \
        LayerBase.Generator \
        LayerBase.Test \
        LayerBase.Usage || true
)"

if [ -n "$CUSTOM_SCOPE_ID_HITS" ]; then
    printf '%s\n' "$CUSTOM_SCOPE_ID_HITS"
    exit 1
fi

if [ -n "$REFLECTION_HITS" ]; then
    printf '%s\n' "$REFLECTION_HITS"
    exit 1
fi
```

Expected: exit code `0`, no output.

- [ ] **Step 5: Inspect diff quality**

```bash
git diff --check
git status --short
git log --oneline --decorate -10
git diff "$(git merge-base HEAD HEAD~9)"..HEAD --stat
```

Verify:

- no temporary scripts;
- no generated `bin/` or `obj/`;
- no unrelated reformatting;
- comments explain non-obvious hashing, identity, and factory logic;
- all new public APIs have XML documentation;
- each commit is independently understandable.

- [ ] **Step 6: Final implementation report**

The agent's final report must include:

1. Branch name and final commit SHA.
2. Exact files created/modified.
3. Identity algorithm:
   - default identity format;
   - stable key format;
   - SHA-256 → positive non-zero `int`.
4. Runtime behavior:
   - no Scope reflection;
   - one Scope definition instance per runtime;
   - options sourced from definition object.
5. Migration result:
   - count of custom manual `ScopeId` declarations removed;
   - count of Scope structs converted to classes.
6. Commands executed and pass/fail results.
7. Any compatibility break:
   - custom Scope declarations are now classes with `Options`;
   - custom static `ScopeId` is forbidden;
   - renamed default-identity Scope receives a new ID unless `[ScopeIdentity]` is used.

Do not claim completion unless every command in Task 9 passes.

## Final acceptance criteria

- [ ] `IScopeDefinition` has a public `ScopeOptions Options { get; }`.
- [ ] `MainScope` is a class and remains ID `0`.
- [ ] Every custom Scope is a class.
- [ ] No production custom Scope declares `ScopeId`.
- [ ] `ScopeOptions`, `ScopeThreadingMode`, and `ScopeClockMode` are public.
- [ ] Layer generator emits ID, identity, type, and factory.
- [ ] Assembly Module generator emits the same descriptor shape.
- [ ] Runtime composition never reads a `ScopeId` field.
- [ ] Runtime composition never constructs a Scope with reflection.
- [ ] Adding/deleting unrelated Scope types does not change existing IDs.
- [ ] `[ScopeIdentity]` preserves IDs across type/namespace/assembly rename.
- [ ] Same descriptor duplicates deduplicate.
- [ ] Type/identity/ID conflicts fail with origin-rich diagnostics.
- [ ] Custom Scope options reach `ScopeRuntime`.
- [ ] One definition object is created per unique Scope per runtime.
- [ ] Debug and Release builds pass.
- [ ] Full NUnit suite passes.
