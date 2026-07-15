using System.Collections.Immutable;
using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.Generator;
using LayerBase.Layers;
using LayerBase.Scope;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace LayerBase.Test;

[TestFixture]
public sealed class MountScopeBoundaryGeneratorTests
{
    [TestCase("LBMOUNT007", """
                             using LayerBase.DI;
                             using LayerBase.DI.Options;
                             using LayerBase.Layers;
                             using LayerBase.Scope;

                             public readonly struct CombatScope : IScopeDefinition { }
                             public readonly struct PresentationScope : IScopeDefinition { }

                             public sealed partial class GameplayLayer : Layer
                             {
                                 [Mount] private CombatService _combat = null!;
                             }

                             [Scope<CombatScope>]
                             [OwnerLayer(typeof(GameplayLayer))]
                             public sealed partial class CombatService : IService
                             {
                                 public void ConfigureServices(IServiceCollection services) { }
                             }
                             """)]
    [TestCase("LBMOUNT008", """
                             using LayerBase.DI;
                             using LayerBase.DI.Options;
                             using LayerBase.Scope;

                             public readonly struct CombatScope : IScopeDefinition { }

                             [Scope<CombatScope>]
                             public sealed partial class CombatService : IService
                             {
                                 [Mount] private MainScopeContext _context = null!;
                                 public void ConfigureServices(IServiceCollection services) { }
                             }

                             [Scope<PresentationScope>]
                             [OwnerService(typeof(CombatService))]
                             public sealed partial class MainScopeContext : ILayerContext
                             {
                             }
                             """)]
    [TestCase("LBMOUNT009", """
                             using LayerBase.DI;
                             using LayerBase.DI.Options;
                             using LayerBase.Layers;

                             public sealed partial class GameplayLayer : Layer { }
                             public sealed partial class PresentationLayer : Layer { }

                             [OwnerLayer(typeof(GameplayLayer))]
                             public sealed partial class GameplayService : IService
                             {
                                 [Mount] private PresentationContext _context = null!;
                                 public void ConfigureServices(IServiceCollection services) { }
                             }

                             [OwnerLayer(typeof(PresentationLayer))]
                             public sealed partial class PresentationService : IService
                             {
                                 public void ConfigureServices(IServiceCollection services) { }
                             }

                             [OwnerService(typeof(PresentationService))]
                             public sealed partial class PresentationContext : ILayerContext
                             {
                             }
                             """)]
    public void Mount_generator_reports_scope_boundary_diagnostics(string diagnosticId, string source)
    {
        var result = RunGenerators(source, new LayerServiceGenerator());

        Assert.That(result.Diagnostics.Select(static diagnostic => diagnostic.Id), Does.Contain(diagnosticId),
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
    }

    [Test]
    public void Mount_generator_accepts_named_implementation_property()
    {
        const string source = """
                              using LayerBase.DI;
                              using LayerBase.DI.Options;

                              public sealed partial class CombatService : IService
                              {
                                  [Mount(Implementation = typeof(DamageContext))]
                                  private IDamageContext _damage = null!;

                                  public void ConfigureServices(IServiceCollection services) { }
                              }

                              public interface IDamageContext
                              {
                              }

                              public sealed partial class DamageContext : IDamageContext, ILayerContext
                              {
                              }
                              """;

        var result = RunGenerators(source, new LayerServiceGenerator());

        Assert.That(result.Diagnostics, Is.Empty,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
        Assert.That(result.GeneratedSources.Any(static sourceText => sourceText.Contains("DamageContext")), Is.True);
        Assert.That(result.GeneratedSources.Any(static sourceText => sourceText.Contains("IGeneratedMountInject")), Is.True);
        Assert.That(result.GeneratedSources.Any(static sourceText => sourceText.Contains("__InjectMounts")), Is.True);
    }

    [Test]
    public void No_runtime_reflection_mount_path_remains()
    {
        var serviceProviderSource = File.ReadAllText(FindRepositoryFile("LayerBase", "DI", "ServiceProvider.cs"));

        Assert.That(serviceProviderSource, Does.Not.Contain("GetFields(BindingFlags.Instance"));
        Assert.That(serviceProviderSource, Does.Not.Contain("GetProperties(BindingFlags.Instance"));
        Assert.That(serviceProviderSource, Does.Not.Contain("CreateFieldSetter"));
        Assert.That(serviceProviderSource, Does.Not.Contain("CreatePropertySetter"));
    }

    private static GeneratorTestResult RunGenerators(string source, params IIncrementalGenerator[] generators)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));
        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "MountScopeBoundaryTests_" + Guid.NewGuid().ToString("N"),
            syntaxTrees: [syntaxTree],
            references: GetMetadataReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators.Select(static generator => generator.AsSourceGenerator()).ToArray(),
            parseOptions: new CSharpParseOptions(LanguageVersion.Preview));

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);
        var diagnostics = driver.GetRunResult().Results
                                .SelectMany(static result => result.Diagnostics)
                                .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error ||
                                                            diagnostic.Severity == DiagnosticSeverity.Warning)
                                .ToImmutableArray();

        return new GeneratorTestResult(
            diagnostics,
            outputCompilation,
            driver.GetRunResult().Results.SelectMany(static result => result.GeneratedSources)
                  .Select(static source => source.SourceText.ToString())
                  .ToImmutableArray());
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
        paths.Add(typeof(ScopeAttribute<>).Assembly.Location);
        paths.Add(typeof(LayerServiceGenerator).Assembly.Location);

        foreach (var path in paths)
        {
            yield return MetadataReference.CreateFromFile(path);
        }
    }

    private static string FindRepositoryFile(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            var candidate = Path.Combine(new[] { directory.FullName }.Concat(segments).ToArray());
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not locate repository file.", Path.Combine(segments));
    }

    private readonly record struct GeneratorTestResult(
        ImmutableArray<Diagnostic> Diagnostics,
        Compilation OutputCompilation,
        ImmutableArray<string> GeneratedSources);
}

[TestFixture]
public partial class MountScopeBoundaryRuntimeTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Service_mount_resolves_same_layer_same_scope_service()
    {
        var layer = new MountBoundaryLayer();

        LayerHub.CreateLayers()
                .Push(layer)
                .Build();

        var service = layer.GetService<MountBoundaryRootService>();

        Assert.That(service, Is.Not.Null);
        Assert.That(service!.MountedService, Is.Not.Null);
    }

    [Test]
    public void Service_mount_resolves_same_layer_same_scope_context()
    {
        var layer = new MountBoundaryLayer();

        LayerHub.CreateLayers()
                .Push(layer)
                .Build();

        var service = layer.GetService<MountBoundaryRootService>();

        Assert.That(service, Is.Not.Null);
        Assert.That(service!.MountedContext, Is.Not.Null);
    }
}

public readonly struct MountBoundaryScope : IScopeDefinition
{
}

public sealed partial class MountBoundaryLayer : Layer
{
}

[Scope<MountBoundaryScope>]
[OwnerLayer(typeof(MountBoundaryLayer))]
public sealed partial class MountBoundaryRootService : IService
{
    [Mount] private MountBoundaryChildService _childService = null!;
    [Mount] private MountBoundaryContext _context = null!;

    public MountBoundaryChildService? MountedService => _childService;
    public MountBoundaryContext? MountedContext => _context;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }
}

[Scope<MountBoundaryScope>]
[OwnerLayer(typeof(MountBoundaryLayer))]
public sealed partial class MountBoundaryChildService : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
    }
}

[Scope<MountBoundaryScope>]
[OwnerService(typeof(MountBoundaryRootService))]
public sealed partial class MountBoundaryContext : ILayerContext
{
}
