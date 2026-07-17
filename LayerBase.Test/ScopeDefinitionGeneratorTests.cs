using System.Collections.Immutable;
using LayerBase.Generator;
using LayerBase.Scope;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace LayerBase.Test;

[TestFixture]
public sealed class ScopeDefinitionGeneratorTests
{
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

    [TestCase("LBSC003", "public struct BadScope : IScopeDefinition")]
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

    private static int GetGeneratedScopeId(
        string source,
        string assemblyName,
        string scopeTypeName)
    {
        GeneratorDriverRunResult result = RunScopeGenerators(source, assemblyName);

        foreach (var generated in result.GeneratedTrees)
        {
            string text = generated.GetText().ToString();
            if (text.Contains("scopeType") && text.Contains(scopeTypeName))
            {
                var match = System.Text.RegularExpressions.Regex.Match(
                    text, @"scopeId:\s*(\d+)");
                if (match.Success)
                    return int.Parse(match.Groups[1].Value);
            }
        }

        throw new InvalidOperationException(
            $"No generated descriptor found for scope type '{scopeTypeName}'.");
    }

    private static string CreateSingleScopeSource(
        string typeName,
        string? stableKey)
    {
        string attribute = stableKey != null
            ? $"[ScopeIdentity(\"{stableKey}\")]\n    "
            : "";

        return $$"""
            using LayerBase.Scope;

            {{attribute}}public sealed class {{typeName}} : IScopeDefinition
            {
                public ScopeOptions Options => ScopeOptions.Inline;
            }
            """;
    }

    private static GeneratorDriverRunResult RunScopeGenerators(
        string source,
        string assemblyName)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(
            source,
            new CSharpParseOptions(LanguageVersion.Preview));

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: assemblyName,
            syntaxTrees: [syntaxTree],
            references: GetMetadataReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new ScopeDefinitionGenerator().AsSourceGenerator()],
            parseOptions: new CSharpParseOptions(LanguageVersion.Preview));

        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation, out _, out _);

        return driver.GetRunResult();
    }

    private static IEnumerable<MetadataReference> GetMetadataReferences()
    {
        var trusted = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
        var paths = trusted.Split(Path.PathSeparator).ToHashSet(StringComparer.OrdinalIgnoreCase);

        paths.Add(typeof(object).Assembly.Location);
        paths.Add(typeof(IScopeDefinition).Assembly.Location);

        foreach (string path in paths)
            yield return MetadataReference.CreateFromFile(path);
    }
}
