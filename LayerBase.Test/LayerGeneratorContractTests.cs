using System.Collections.Immutable;
using LayerBase.Async;
using LayerBase.DI;
using LayerBase.Generator;
using LayerBase.Layers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LayerBase.Test;

[TestFixture]
public class LayerGeneratorContractTests
{
    [TestCase("LBOS001", """
                         using LayerBase.DI;
                         using LayerBase.DI.Options;

                         public sealed class NotAService
                         {
                         }

                         [OwnerService(typeof(NotAService))]
                         public sealed partial class BadContext : ILayerContext
                         {
                         }
                         """)]
    [TestCase("LBOS003", """
                         using System.Threading;
                         using LayerBase.Async;
                         using LayerBase.Call;
                         using LayerBase.DI;
                         using LayerBase.DI.Options;

                         public readonly struct TestRequest { }
                         public readonly struct TestResponse { }

                         public sealed partial class CombatService : IService
                         {
                             public void ConfigureServices(IServiceCollection services) { }
                         }

                         [OwnerService(typeof(CombatService))]
                         public sealed class BadHandler : IScopeLocalCallHandler<TestRequest, TestResponse>
                         {
                             public async LBTask<TestResponse> HandleAsync(TestRequest request, CancellationToken cancellationToken = default)
                             {
                                 await LBTask.CompletedTask;
                                 return default;
                             }
                         }
                         """)]
    [TestCase("LBOS004", """
                         using LayerBase.DI;
                         using LayerBase.DI.Options;

                         public sealed partial class ServiceA : IService
                         {
                             [Mount] private SharedContext _context = null!;
                             public void ConfigureServices(IServiceCollection services) { }
                         }

                         public sealed partial class ServiceB : IService
                         {
                             public void ConfigureServices(IServiceCollection services) { }
                         }

                         [OwnerService(typeof(ServiceB))]
                         public sealed partial class SharedContext : ILayerContext
                         {
                         }
                         """)]
    public void Layer_service_generator_reports_expected_diagnostic(string diagnosticId, string source)
    {
        var result = RunGenerators(source, new LayerServiceGenerator());

        Assert.That(result.Diagnostics.Select(static diagnostic => diagnostic.Id), Does.Contain(diagnosticId),
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
    }

    [Test]
    public void Service_method_marked_with_Call_generates_without_errors()
    {
        const string source = """
                              using LayerBase.Async;
                              using LayerBase.Call;
                              using LayerBase.DI;

                              public readonly struct TestRequest
                              {
                              }

                              public readonly struct TestResponse
                              {
                              }

                              public sealed partial class CombatService : IService
                              {
                                  public void ConfigureServices(IServiceCollection services) { }

                                  [Call]
                                  private async LBTask<TestResponse> Handle(TestRequest request)
                                  {
                                      await LBTask.CompletedTask;
                                      return default;
                                  }
                              }
                              """;

        var result = RunGenerators(source, new CallAutoBindGenerator());

        Assert.That(result.Diagnostics, Is.Empty,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        var errors = result.OutputCompilation.GetDiagnostics()
                           .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                           .ToImmutableArray();

        Assert.That(errors, Is.Empty,
                           string.Join(Environment.NewLine, errors.Select(static error => error.ToString())));
    }

    [Test]
    public void LayerContext_method_marked_with_Call_generates_without_errors()
    {
        const string source = """
                              using LayerBase.Async;
                              using LayerBase.Call;
                              using LayerBase.DI;

                              public readonly struct TestRequest
                              {
                              }

                              public readonly struct TestResponse
                              {
                              }

                              public sealed partial class CommerceContext : ILayerContext
                              {
                                  [Call]
                                  private async LBTask<TestResponse> Handle(TestRequest request)
                                  {
                                      await LBTask.CompletedTask;
                                      return default;
                                  }
                              }
                              """;

        var result = RunGenerators(source, new CallAutoBindGenerator());

        Assert.That(result.Diagnostics, Is.Empty,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        var errors = result.OutputCompilation.GetDiagnostics()
                           .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                           .ToImmutableArray();

        Assert.That(errors, Is.Empty,
            string.Join(Environment.NewLine, errors.Select(static error => error.ToString())));
    }

    [Test]
    public void Layer_method_marked_with_Call_generates_without_errors()
    {
        const string source = """
                              using LayerBase.Async;
                              using LayerBase.Call;
                              using LayerBase.DI;
                              using LayerBase.Layers;

                              public readonly struct TestRequest
                              {
                              }

                              public readonly struct TestResponse
                              {
                              }

                              public sealed partial class TestLayer : Layer
                              {
                                  [Call]
                                  private async LBTask<TestResponse> Handle(TestRequest request)
                                  {
                                      await LBTask.CompletedTask;
                                      return default;
                                  }
                              }
                              """;

        var result = RunGenerators(source, new CallAutoBindGenerator());

        Assert.That(result.Diagnostics, Is.Empty,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        var errors = result.OutputCompilation.GetDiagnostics()
                           .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                           .ToImmutableArray();

        Assert.That(errors, Is.Empty,
            string.Join(Environment.NewLine, errors.Select(static error => error.ToString())));
    }

    [Test]
    public void Service_method_marked_with_SubscribeScopeCall_generates_without_errors()
    {
        const string source = """
                              using System.Threading;
                              using LayerBase.Async;
                              using LayerBase.DI;
                              using LayerBase.Layers;
                              using LayerBase.Scope;

                              public sealed class InventoryScope : IScopeDefinition{public const int ScopeId = 71;public ScopeOptions Options => ScopeOptions.Inline;}

                              public readonly struct TestRequest
                              {
                              }

                              public readonly struct TestResponse
                              {
                              }

                              public sealed partial class CommerceLayer : Layer
                              {
                              }

                              [OwnerLayer(typeof(CommerceLayer))]
                              [Scope<InventoryScope>]
                              public sealed partial class InventoryService : IService
                              {
                                  public void ConfigureServices(IServiceCollection services) { }

                                  [SubscribeScopeCall]
                                  private async LBTask<TestResponse> ReserveAsync(
                                      TestRequest request,
                                      CancellationToken cancellationToken = default)
                                  {
                                      await LBTask.CompletedTask;
                                      return default;
                                  }
                              }
                              """;

        var result = RunGenerators(source, new ScopeEndpointAutoBindGenerator());

        Assert.That(result.Diagnostics, Is.Empty,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        var errors = result.OutputCompilation.GetDiagnostics()
                           .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                           .ToImmutableArray();

        Assert.That(errors, Is.Empty,
            string.Join(Environment.NewLine, errors.Select(static error => error.ToString())));
    }

    [Test]
    public void LayerContext_method_marked_with_SubscribeScopeCall_generates_without_errors()
    {
        const string source = """
                              using System.Threading;
                              using LayerBase.Async;
                              using LayerBase.DI;
                              using LayerBase.Scope;

                              public readonly struct TestRequest
                              {
                              }

                              public readonly struct TestResponse
                              {
                              }

                              public sealed partial class InventoryContext : ILayerContext
                              {
                                  [SubscribeScopeCall]
                                  private async LBTask<TestResponse> ReserveAsync(
                                      TestRequest request,
                                      CancellationToken cancellationToken = default)
                                  {
                                      await LBTask.CompletedTask;
                                      return default;
                                  }
                              }
                              """;

        var result = RunGenerators(source, new ScopeEndpointAutoBindGenerator());

        Assert.That(result.Diagnostics, Is.Empty,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        var errors = result.OutputCompilation.GetDiagnostics()
                           .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                           .ToImmutableArray();

        Assert.That(errors, Is.Empty,
            string.Join(Environment.NewLine, errors.Select(static error => error.ToString())));
    }

    [Test]
    public void Service_method_marked_with_SubscribeScopeEvent_generates_without_errors()
    {
        const string source = """
                              using LayerBase.DI;
                              using LayerBase.Layers;
                              using LayerBase.Scope;

                              public sealed class InventoryScope : IScopeDefinition{public const int ScopeId = 71;public ScopeOptions Options => ScopeOptions.Inline;}

                              public readonly struct StockArrived
                              {
                              }

                              public sealed partial class CommerceLayer : Layer
                              {
                              }

                              [OwnerLayer(typeof(CommerceLayer))]
                              [Scope<InventoryScope>]
                              public sealed partial class InventoryService : IService
                              {
                                  public void ConfigureServices(IServiceCollection services) { }

                                  [SubscribeScopeEvent]
                                  private void OnStockArrived(in StockArrived value)
                                  {
                                  }
                              }
                              """;

        var result = RunGenerators(source, new ScopeEndpointAutoBindGenerator());

        Assert.That(result.Diagnostics, Is.Empty,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        var errors = result.OutputCompilation.GetDiagnostics()
                           .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                           .ToImmutableArray();

        Assert.That(errors, Is.Empty,
            string.Join(Environment.NewLine, errors.Select(static error => error.ToString())));
    }

    [Test]
    public void LayerContext_method_marked_with_SubscribeScopeEvent_generates_without_errors()
    {
        const string source = """
                              using LayerBase.DI;
                              using LayerBase.Scope;

                              public readonly struct StockArrived
                              {
                              }

                              public sealed partial class InventoryContext : ILayerContext
                              {
                                  [SubscribeScopeEvent]
                                  private void OnStockArrived(in StockArrived value)
                                  {
                                  }
                              }
                              """;

        var result = RunGenerators(source, new ScopeEndpointAutoBindGenerator());

        Assert.That(result.Diagnostics, Is.Empty,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        var errors = result.OutputCompilation.GetDiagnostics()
                           .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                           .ToImmutableArray();

        Assert.That(errors, Is.Empty,
            string.Join(Environment.NewLine, errors.Select(static error => error.ToString())));
    }

    [Test]
    public void Call_attribute_method_without_async_reports_analyzer_diagnostic()
    {
        const string source = """
                              using LayerBase.Async;
                              using LayerBase.Call;
                              using LayerBase.Layers;

                              public readonly struct TestRequest
                              {
                              }

                              public readonly struct TestResponse
                              {
                              }

                              public sealed partial class TestLayer : Layer
                              {
                                  [Call]
                                  private LBTask<TestResponse> Handle(TestRequest request)
                                  {
                                      return LBTask<TestResponse>.FromResult(default);
                                  }
                              }
                              """;

        var diagnostics = RunCallAnalyzer(source);

        Assert.That(diagnostics.Select(static diagnostic => diagnostic.Id), Does.Contain("LBG305"),
            string.Join(Environment.NewLine, diagnostics.Select(static diagnostic => diagnostic.ToString())));
    }

    [Test]
    public void Scope_local_call_handler_without_async_reports_analyzer_diagnostic()
    {
        const string source = """
                              using System.Threading;
                              using LayerBase.Async;
                              using LayerBase.Call;

                              public readonly struct TestRequest
                              {
                              }

                              public readonly struct TestResponse
                              {
                              }

                              public sealed class TestHandler : IScopeLocalCallHandler<TestRequest, TestResponse>
                              {
                                  public LBTask<TestResponse> HandleAsync(
                                      TestRequest request,
                                      CancellationToken cancellationToken = default)
                                  {
                                      return LBTask<TestResponse>.FromResult(default);
                                  }
                              }
                              """;

        var diagnostics = RunCallAnalyzer(source);

        Assert.That(diagnostics.Select(static diagnostic => diagnostic.Id), Does.Contain("LBG305"),
            string.Join(Environment.NewLine, diagnostics.Select(static diagnostic => diagnostic.ToString())));
    }

    [Test]
    public void SubscribeScopeCall_attribute_method_without_async_reports_analyzer_diagnostic()
    {
        const string source = """
                              using LayerBase.Async;
                              using LayerBase.Scope;

                              public readonly struct TestRequest
                              {
                              }

                              public readonly struct TestResponse
                              {
                              }

                              public sealed partial class TestService
                              {
                                  [SubscribeScopeCall]
                                  private LBTask<TestResponse> Handle(TestRequest request)
                                  {
                                      return LBTask<TestResponse>.FromResult(default);
                                  }
                              }
                              """;

        var diagnostics = RunCallAnalyzer(source);

        Assert.That(diagnostics.Select(static diagnostic => diagnostic.Id), Does.Contain("LBG305"),
            string.Join(Environment.NewLine, diagnostics.Select(static diagnostic => diagnostic.ToString())));
    }

    private static GeneratorTestResult RunGenerators(string source, params IIncrementalGenerator[] generators)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));
        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "LayerGeneratorTests_" + Guid.NewGuid().ToString("N"),
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

        return new GeneratorTestResult(diagnostics, outputCompilation);
    }

    private static ImmutableArray<Diagnostic> RunCallAnalyzer(string source)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));
        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "CallAnalyzerTests_" + Guid.NewGuid().ToString("N"),
            syntaxTrees: [syntaxTree],
            references: GetMetadataReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return compilation
            .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new CallReceiverAnalyzer()))
            .GetAnalyzerDiagnosticsAsync()
            .GetAwaiter()
            .GetResult()
            .Where(static diagnostic => diagnostic.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning)
            .ToImmutableArray();
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
        Compilation OutputCompilation);
}
