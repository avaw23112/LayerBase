using System.Collections.Immutable;
using LayerBase.Async;
using LayerBase.DI;
using LayerBase.Generator;
using LayerBase.Layers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace LayerBase.Test;

[TestFixture]
public class AssemblyModuleGeneratorTests
{
    [Test]
    public void Assembly_module_generator_emits_static_manifest_with_service_contribution()
    {
        const string source = """
                              using LayerBase.DI;
                              using LayerBase.Layers;
                              using LayerBase.Modules;
                              using LayerBase.Scope;

                              namespace Sample;

                              public sealed class GameLayer : Layer
                              {
                              }

                              public interface IGameService
                              {
                              }

                              public sealed class GameService : IGameService
                              {
                              }

                              [AssemblyModule("gameplay")]
                              [ModuleService(typeof(GameLayer), typeof(MainScope), typeof(IGameService), typeof(GameService), ServiceLifetime.Singleton)]
                              public sealed partial class GameplayModule
                              {
                              }
                              """;

        var result = RunGenerators(source, new AssemblyModuleGenerator());

        Assert.That(result.Diagnostics, Is.Empty,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        var errors = result.OutputCompilation.GetDiagnostics()
                           .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                           .ToImmutableArray();

        Assert.That(errors, Is.Empty,
            string.Join(Environment.NewLine, errors.Select(static error => error.ToString())));

        var generatedSource = result.GeneratedSources.Single(static sourceText =>
            sourceText.Contains("partial class GameplayModule"));

        Assert.That(generatedSource, Does.Contain(": global::LayerBase.Modules.IAssemblyModule"));
        Assert.That(generatedSource, Does.Contain("new global::LayerBase.Modules.AssemblyModuleId(\"gameplay\")"));
        Assert.That(generatedSource, Does.Contain("global::LayerBase.Modules.ServiceContribution.ForTypes("));
        Assert.That(generatedSource, Does.Contain("typeof(global::Sample.IGameService)"));
        Assert.That(generatedSource, Does.Contain("typeof(global::Sample.GameService)"));
        Assert.That(generatedSource, Does.Contain("typeof(global::Sample.GameLayer)"));
        Assert.That(generatedSource, Does.Contain("typeof(global::LayerBase.Scope.MainScope)"));
        Assert.That(generatedSource, Does.Contain("global::LayerBase.DI.ServiceLifetime.Singleton"));
    }

    [Test]
    public void Assembly_module_generator_does_not_emit_runtime_ownership_or_instance_creation()
    {
        const string source = """
                              using LayerBase.DI;
                              using LayerBase.Layers;
                              using LayerBase.Modules;
                              using LayerBase.Scope;

                              public sealed class GameLayer : Layer
                              {
                              }

                              public interface IGameService
                              {
                              }

                              public sealed class GameService : IGameService
                              {
                              }

                              [AssemblyModule("gameplay")]
                              [ModuleService(typeof(GameLayer), typeof(MainScope), typeof(IGameService), typeof(GameService))]
                              public sealed partial class GameplayModule
                              {
                              }
                              """;

        var result = RunGenerators(source, new AssemblyModuleGenerator());

        var generatedSource = result.GeneratedSources.Single(static sourceText =>
            sourceText.Contains("partial class GameplayModule"));

        Assert.That(generatedSource, Does.Not.Contain(".Push("));
        Assert.That(generatedSource, Does.Not.Contain("GetAssemblies"));
        Assert.That(generatedSource, Does.Not.Contain("ScopeRuntime"));
        Assert.That(generatedSource, Does.Not.Contain("new global::GameService"));
    }

    private static GeneratorTestResult RunGenerators(string source, params IIncrementalGenerator[] generators)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));
        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "AssemblyModuleGeneratorTests_" + Guid.NewGuid().ToString("N"),
            syntaxTrees: [syntaxTree],
            references: GetMetadataReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators.Select(static generator => generator.AsSourceGenerator()).ToArray(),
            parseOptions: new CSharpParseOptions(LanguageVersion.Preview));

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);
        var runResult = driver.GetRunResult();
        var diagnostics = runResult.Results
                                   .SelectMany(static result => result.Diagnostics)
                                   .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error ||
                                                               diagnostic.Severity == DiagnosticSeverity.Warning)
                                   .ToImmutableArray();

        var generatedSources = runResult.Results
                                        .SelectMany(static result => result.GeneratedSources)
                                        .Select(static generated => generated.SourceText.ToString())
                                        .ToImmutableArray();

        return new GeneratorTestResult(diagnostics, outputCompilation, generatedSources);
    }

    private static IEnumerable<MetadataReference> GetMetadataReferences()
    {
        var trustedPlatformAssemblies = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
        var paths = trustedPlatformAssemblies
                    .Split(Path.PathSeparator)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

        paths.Add(typeof(object).Assembly.Location);
        paths.Add(typeof(Enumerable).Assembly.Location);
        paths.Add(typeof(IService).Assembly.Location);
        paths.Add(typeof(Layer).Assembly.Location);
        paths.Add(typeof(LBTask).Assembly.Location);
        paths.Add(typeof(LayerServiceGenerator).Assembly.Location);

        foreach (var path in paths)
        {
            yield return MetadataReference.CreateFromFile(path);
        }
    }

    private readonly record struct GeneratorTestResult(ImmutableArray<Diagnostic> Diagnostics,
        Compilation OutputCompilation,
        ImmutableArray<string> GeneratedSources);
}
