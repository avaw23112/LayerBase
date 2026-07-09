using System.Collections.Immutable;
using LayerBase.Async;
using LayerBase.DI;
using LayerBase.Generator;
using LayerBase.Layers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace LayerBase.Test;

[TestFixture]
public class LayerGeneratorContractTests
{
    [Test]
    public void Layer_tool_generator_emits_registry_for_constructor_and_static_factory()
    {
        const string source = """
                              using LayerBase.Tooling;

                              public interface IUiView
                              {
                              }

                              [LayerTool("ui.view", Contract = typeof(IUiView))]
                              [System.AttributeUsage(System.AttributeTargets.Class)]
                              public sealed class UiViewAttribute : System.Attribute
                              {
                                  public UiViewAttribute(string key)
                                  {
                                      Key = key;
                                  }

                                  public string Key { get; }
                                  public string? Path { get; set; }
                                  public bool Cache { get; set; }
                                  public System.Type? Layer { get; set; }
                                  public System.Type? Service { get; set; }
                                  public System.Type? Manager { get; set; }
                              }

                              public sealed class ViewLayer
                              {
                              }

                              public sealed class ViewService
                              {
                              }

                              public sealed class ViewManager
                              {
                              }

                              [UiView("Inventory", Path = "UI/Inventory", Cache = true, Layer = typeof(ViewLayer), Service = typeof(ViewService), Manager = typeof(ViewManager))]
                              public sealed class InventoryView : IUiView
                              {
                                  public InventoryView()
                                  {
                                  }
                              }

                              [UiView("Settings")]
                              public sealed class SettingsView : IUiView
                              {
                                  private SettingsView()
                                  {
                                  }

                                  [LayerToolFactory]
                                  public static SettingsView Create(LayerToolCreateContext context)
                                  {
                                      return new SettingsView();
                                  }
                              }
                              """;

        var result = RunGenerators(source, new LayerToolGenerator());

        Assert.That(result.Diagnostics, Is.Empty,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        var errors = result.OutputCompilation.GetDiagnostics()
                           .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                           .ToImmutableArray();

        Assert.That(errors, Is.Empty,
            string.Join(Environment.NewLine, errors.Select(static error => error.ToString())));

        var generated = string.Join(Environment.NewLine, result.GeneratedSources);

        Assert.That(generated, Does.Contain("registry.Register<global::IUiView, global::InventoryView>"));
        Assert.That(generated, Does.Contain("toolId: \"ui.view\""));
        Assert.That(generated, Does.Contain("key: \"Inventory\""));
        Assert.That(generated, Does.Contain("path: \"UI/Inventory\""));
        Assert.That(generated, Does.Contain("cache: true"));
        Assert.That(generated, Does.Contain("ownerLayerType: typeof(global::ViewLayer)"));
        Assert.That(generated, Does.Contain("ownerServiceType: typeof(global::ViewService)"));
        Assert.That(generated, Does.Contain("ownerManagerType: typeof(global::ViewManager)"));
        Assert.That(generated, Does.Contain("factory: static context => new global::InventoryView()"));
        Assert.That(generated, Does.Contain("factory: static context => global::SettingsView.Create(context)"));
        Assert.That(generated, Does.Contain("UseGeneratedLayerTools"));
        Assert.That(generated, Does.Not.Contain("Activator.CreateInstance"));
        Assert.That(generated, Does.Not.Contain("GetConstructor"));
        Assert.That(generated, Does.Not.Contain("GetCustomAttribute"));
        Assert.That(generated, Does.Not.Contain("Assembly.GetTypes"));
    }

    [Test]
    public void Layer_tool_generator_prefers_external_factory_when_static_factory_is_absent()
    {
        const string source = """
                              using LayerBase.Tooling;

                              public interface IUiView
                              {
                              }

                              [LayerTool("ui.view", Contract = typeof(IUiView))]
                              [System.AttributeUsage(System.AttributeTargets.Class)]
                              public sealed class UiViewAttribute : System.Attribute
                              {
                                  public UiViewAttribute(string key)
                                  {
                                      Key = key;
                                  }

                                  public string Key { get; }
                                  public System.Type? Factory { get; set; }
                              }

                              public sealed class InventoryFactory : ILayerToolFactory<InventoryView>
                              {
                                  public InventoryView Create(LayerToolCreateContext context, LayerToolEntry entry)
                                  {
                                      return new InventoryView();
                                  }
                              }

                              [UiView("Inventory", Factory = typeof(InventoryFactory))]
                              public sealed class InventoryView : IUiView
                              {
                                  public InventoryView()
                                  {
                                  }

                                  public static InventoryView CreateForTest()
                                  {
                                      return new InventoryView();
                                  }
                              }
                              """;

        var result = RunGenerators(source, new LayerToolGenerator());

        Assert.That(result.Diagnostics, Is.Empty,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        var errors = result.OutputCompilation.GetDiagnostics()
                           .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                           .ToImmutableArray();

        Assert.That(errors, Is.Empty,
            string.Join(Environment.NewLine, errors.Select(static error => error.ToString())));

        var generated = string.Join(Environment.NewLine, result.GeneratedSources);

        Assert.That(generated, Does.Contain("context.GetFactory<global::InventoryFactory>().Create(context, context.Registry.GetEntry<global::InventoryView>())"));
    }

    [TestCase("LBTOOL004", """
                           using LayerBase.Tooling;

                           public interface IUiView { }

                           [LayerTool("ui.view", Contract = typeof(IUiView))]
                           [System.AttributeUsage(System.AttributeTargets.Class)]
                           public sealed class UiViewAttribute : System.Attribute
                           {
                               public UiViewAttribute(string key) { Key = key; }
                               public string Key { get; }
                           }

                           [UiView("Inventory")]
                           public sealed class InventoryView
                           {
                               public InventoryView() { }
                           }
                           """)]
    [TestCase("LBTOOL007", """
                           using LayerBase.Tooling;

                           public interface IUiView { }

                           [LayerTool("ui.view", Contract = typeof(IUiView))]
                           [System.AttributeUsage(System.AttributeTargets.Class)]
                           public sealed class UiViewAttribute : System.Attribute
                           {
                               public UiViewAttribute(string key) { Key = key; }
                               public string Key { get; }
                           }

                           [UiView("Inventory")]
                           public sealed class InventoryView : IUiView
                           {
                               private InventoryView() { }
                           }
                           """)]
    [TestCase("LBTOOL006", """
                           using LayerBase.Tooling;

                           public interface IUiView { }

                           [LayerTool("ui.view", Contract = typeof(IUiView))]
                           [System.AttributeUsage(System.AttributeTargets.Class)]
                           public sealed class UiViewAttribute : System.Attribute
                           {
                               public UiViewAttribute(string key) { Key = key; }
                               public string Key { get; }
                           }

                           [UiView("Inventory")]
                           public sealed class InventoryView : IUiView
                           {
                               public InventoryView() { }
                           }

                           [UiView("Inventory")]
                           public sealed class DuplicateInventoryView : IUiView
                           {
                               public DuplicateInventoryView() { }
                           }
                           """)]
    [TestCase("LBTOOL005", """
                           using LayerBase.Tooling;

                           public interface IUiView { }

                           [LayerTool("ui.view", Contract = typeof(IUiView))]
                           [System.AttributeUsage(System.AttributeTargets.Class)]
                           public sealed class UiViewAttribute : System.Attribute
                           {
                               public UiViewAttribute(string key) { Key = key; }
                               public string Key { get; }
                           }

                           [UiView("")]
                           public sealed class InventoryView : IUiView
                           {
                               public InventoryView() { }
                           }
                           """)]
    [TestCase("LBTOOL001", """
                           using LayerBase.Tooling;

                           [LayerTool("bad.tool")]
                           public sealed class NotAnAttribute
                           {
                           }
                           """)]
    [TestCase("LBTOOL003", """
                           using LayerBase.Tooling;

                           [LayerTool("bad.tool", Contract = typeof(int))]
                           [System.AttributeUsage(System.AttributeTargets.Class)]
                           public sealed class BadToolAttribute : System.Attribute
                           {
                               public BadToolAttribute(string key) { Key = key; }
                               public string Key { get; }
                           }
                           """)]
    public void Layer_tool_generator_reports_expected_diagnostic(string diagnosticId, string source)
    {
        var result = RunGenerators(source, new LayerToolGenerator());

        Assert.That(result.Diagnostics.Select(static diagnostic => diagnostic.Id), Does.Contain(diagnosticId),
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
    }

    [TestCase("LBTOOL008", """
                           using LayerBase.Tooling;

                           public interface IUiView { }

                           [LayerTool("ui.view", Contract = typeof(IUiView))]
                           [System.AttributeUsage(System.AttributeTargets.Class)]
                           public sealed class UiViewAttribute : System.Attribute
                           {
                               public UiViewAttribute(string key) { Key = key; }
                               public string Key { get; }
                           }

                           [UiView("Inventory")]
                           public sealed class InventoryView : IUiView
                           {
                               private InventoryView() { }

                               [LayerToolFactory]
                               public InventoryView Create(LayerToolCreateContext context)
                               {
                                   return this;
                               }
                           }
                           """)]
    [TestCase("LBTOOL009", """
                           using LayerBase.Tooling;

                           public interface IUiView { }

                           [LayerTool("ui.view", Contract = typeof(IUiView))]
                           [System.AttributeUsage(System.AttributeTargets.Class)]
                           public sealed class UiViewAttribute : System.Attribute
                           {
                               public UiViewAttribute(string key) { Key = key; }
                               public string Key { get; }
                           }

                           [UiView("Inventory")]
                           public sealed class InventoryView : IUiView
                           {
                               private InventoryView() { }

                               [LayerToolFactory]
                               public static InventoryView Create(LayerToolCreateContext context)
                               {
                                   return new InventoryView();
                               }

                               [LayerToolFactory]
                               public static InventoryView CreateOther(LayerToolCreateContext context)
                               {
                                   return new InventoryView();
                               }
                           }
                           """)]
    [TestCase("LBTOOL010", """
                           using LayerBase.Tooling;

                           [LayerTool("ui.view")]
                           [System.AttributeUsage(System.AttributeTargets.Class)]
                           public sealed class UiViewAttribute : System.Attribute
                           {
                               public UiViewAttribute(string key) { Key = key; }
                               public string Key { get; }
                               public string Cache { get; set; } = "";
                           }
                           """)]
    [TestCase("LBTOOL011", """
                           using LayerBase.Tooling;

                           [LayerTool("ui.view")]
                           [System.AttributeUsage(System.AttributeTargets.Class)]
                           public sealed class UiViewAttribute : System.Attribute
                           {
                               public UiViewAttribute(string key) { Key = key; }
                               public string Key { get; }
                               public int Path { get; set; }
                           }
                           """)]
    [TestCase("LBTOOL012", """
                           using LayerBase.Tooling;

                           [LayerTool("ui.view")]
                           [System.AttributeUsage(System.AttributeTargets.Class)]
                           public sealed class UiViewAttribute : System.Attribute
                           {
                               public UiViewAttribute(string key) { Key = key; }
                               public string Key { get; }
                               public string Factory { get; set; } = "";
                           }
                           """)]
    [TestCase("LBTOOL013", """
                           using LayerBase.Tooling;

                           public interface IUiView { }

                           [LayerTool("ui.view", Contract = typeof(IUiView))]
                           [System.AttributeUsage(System.AttributeTargets.Class)]
                           public sealed class UiViewAttribute : System.Attribute
                           {
                               public UiViewAttribute(string key) { Key = key; }
                               public string Key { get; }
                               public System.Type? Factory { get; set; }
                           }

                           public sealed class InvalidFactory
                           {
                           }

                           [UiView("Inventory", Factory = typeof(InvalidFactory))]
                           public sealed class InventoryView : IUiView
                           {
                               private InventoryView() { }
                           }
                           """)]
    public void Layer_tool_generator_reports_factory_and_shape_diagnostic(string diagnosticId, string source)
    {
        var result = RunGenerators(source, new LayerToolGenerator());

        Assert.That(result.Diagnostics.Select(static diagnostic => diagnostic.Id), Does.Contain(diagnosticId),
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
    }

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
                         public sealed class BadHandler : ILayerCallHandler<TestRequest, TestResponse>
                         {
                             public LBTask<TestResponse> HandleAsync(TestRequest request, CancellationToken cancellationToken = default)
                             {
                                 return LBTask<TestResponse>.FromResult(default);
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
    public void Service_method_marked_with_Call_reports_expected_diagnostic()
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
                                  private LBTask<TestResponse> Handle(TestRequest request)
                                  {
                                      return LBTask<TestResponse>.FromResult(default);
                                  }
                              }
                              """;

        var result = RunGenerators(source, new CallAutoBindGenerator());

        Assert.That(result.Diagnostics.Select(static diagnostic => diagnostic.Id), Does.Contain("LBG302"),
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
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
                                  private LBTask<TestResponse> Handle(TestRequest request)
                                  {
                                      return LBTask<TestResponse>.FromResult(default);
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
        var generatedSources = driver.GetRunResult().Results
                                     .SelectMany(static result => result.GeneratedSources)
                                     .Select(static source => source.SourceText.ToString())
                                     .ToImmutableArray();
        var diagnostics = driver.GetRunResult().Results
                                .SelectMany(static result => result.Diagnostics)
                                .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error ||
                                                            diagnostic.Severity == DiagnosticSeverity.Warning)
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
