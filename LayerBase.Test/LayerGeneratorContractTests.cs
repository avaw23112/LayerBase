using System.Collections.Immutable;
using LayerBase.Async;
using LayerBase.DI;
using LayerBase.Generator;
using LayerBase.Generator.Diagnostics;
using LayerBase.Layers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LayerBase.Test;

[TestFixture]
public class LayerGeneratorContractTests
{
    [Test]
    public void Query_bring_generator_emits_direct_plain_query_for_services()
    {
        const string source = """
                              using LayerBase.Core;
                              using LayerBase.DI;
                              using LayerBase.DI.Options;
                              using LayerBase.ECS;

                              namespace QueryGeneratorScopeContract;

                              public struct Position : IComponent
                              {
                                  public int Value;
                              }

                              public struct Velocity : IComponent
                              {
                                  public int Value;
                              }

                              public sealed partial class MovementService : IService
                              {
                                  [Query]
                                  private static void OnMove(ref Position position, in Velocity velocity)
                                  {
                                      position.Value += velocity.Value;
                                  }

                                  public void ConfigureServices(IServiceCollection services) { }
                              }
                              """;

        var result = RunGenerators(source, new QueryBringGenerator());

        Assert.That(result.Diagnostics, Is.Empty,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
        var errors = result.OutputCompilation.GetDiagnostics()
                           .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                           .ToImmutableArray();
        Assert.That(errors, Is.Empty,
            string.Join(Environment.NewLine, errors.Select(static diagnostic => diagnostic.ToString())));

        string generated = string.Join(Environment.NewLine, result.GeneratedSources);
        Assert.That(generated, Does.Contain("global::LayerBase.ServiceECSExtensions"));
        Assert.That(generated, Does.Contain(".Query<global::QueryGeneratorScopeContract.Position, global::QueryGeneratorScopeContract.Velocity>(this)"));
        Assert.That(generated, Does.Contain(".ForEach(ref job);"));
        Assert.That(generated, Does.Not.Contain("GeneratedEcsQueryExecutor"));
        Assert.That(generated, Does.Not.Contain("SubmitPlainQuery"));
        Assert.That(generated, Does.Not.Contain("EcsScheduler"));
    }

    [Test]
    public void Ecs_analyzer_should_report_query_input_order_with_valid_diagnostic_id()
    {
        const string source = """
                              using LayerBase.Core;
                              using LayerBase.ECS;

                              namespace QueryAnalyzerContract;

                              public struct Position : IComponent
                              {
                              }

                              public sealed partial class QueryOwner
                              {
                                  [Query]
                                  private static void OnMove(ref Position position, int delta)
                                  {
                                  }
                              }
                              """;

        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));
        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "EcsAnalyzerTests_" + Guid.NewGuid().ToString("N"),
            syntaxTrees: [syntaxTree],
            references: GetMetadataReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        ImmutableArray<Diagnostic> diagnostics = compilation
                                                 .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(
                                                     new ECSAnalyzer()))
                                                 .GetAnalyzerDiagnosticsAsync()
                                                 .GetAwaiter()
                                                 .GetResult();

        Assert.That(diagnostics.Select(static diagnostic => diagnostic.Id), Does.Not.Contain("AD0001"),
            string.Join(Environment.NewLine, diagnostics.Select(static diagnostic => diagnostic.ToString())));
        Assert.That(diagnostics.Select(static diagnostic => diagnostic.Id), Does.Contain("LBECS012"),
            string.Join(Environment.NewLine, diagnostics.Select(static diagnostic => diagnostic.ToString())));
    }

    [Test]
    public void Shared_field_analyzer_validates_cross_assembly_provider_metadata()
    {
        const string providerSource = """
                                      namespace ProviderLib;

                                      using System.Collections.Generic;
                                      using LayerBase.DI;

                                      public sealed partial class InventoryProvider : IService
                                      {
                                          [Provide("items")]
                                          private readonly List<int> _items = new();

                                          public void ConfigureServices(IServiceCollection services) { }
                                      }
                                      """;
        const string consumerSource = """
                                      namespace ConsumerLib;

                                      using System.Collections.Generic;
                                      using LayerBase.DI;
                                      using ProviderLib;

                                      public sealed partial class InventoryConsumer : IService
                                      {
                                          [From(typeof(InventoryProvider), "missing")]
                                          private IReadOnlyList<int>? _missing;

                                          [From(typeof(InventoryProvider), "items")]
                                          private Dictionary<int, int>? _wrongType;

                                          public void ConfigureServices(IServiceCollection services) { }
                                      }
                                      """;

        MetadataReference providerReference = CompileReferenceAssembly(providerSource, new ScopeResourceGenerator());
        var result = RunGeneratorsWithReferences(
            consumerSource,
            [providerReference],
            new SharedFieldAnalyzer());

        string diagnostics = string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString()));
        Assert.That(result.Diagnostics.Select(static diagnostic => diagnostic.Id), Does.Not.Contain("AD0001"), diagnostics);
        Assert.That(result.Diagnostics.Select(static diagnostic => diagnostic.Id), Does.Contain("LBG403"), diagnostics);
        Assert.That(result.Diagnostics.Select(static diagnostic => diagnostic.Id), Does.Contain("LBG402"), diagnostics);
    }

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

    [Test]
    public void Partial_service_generator_emits_scope_binding_helper_without_service_base()
    {
        const string source = """
                              using LayerBase.DI;

                              public sealed partial class CombatService : IService
                              {
                                  public int ReadScopeId() => OwnerScope.ScopeId;

                                  public int ReadServiceId() => ServiceId;

                                  public LayerBase.Scope.ScopeRef<UiScope> ReadUiScope() => Scope<UiScope>();

                                  public void ConfigureServices(IServiceCollection services) { }
                              }

                              public sealed class UiScope
                              {
                              }
                              """;

        var result = RunGenerators(source, new ManagerAutoSubscribeGenerator());

        Assert.That(result.Diagnostics, Is.Empty,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        var errors = result.OutputCompilation.GetDiagnostics()
                           .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                           .ToImmutableArray();

        Assert.That(errors, Is.Empty,
            string.Join(Environment.NewLine, errors.Select(static error => error.ToString())));

        string generated = string.Join(Environment.NewLine, result.GeneratedSources);
        Assert.That(generated, Does.Contain("global::LayerBase.Scope.IScopeObjectBindingAccessor"));
        Assert.That(generated, Does.Contain("private global::LayerBase.Scope.ScopeObjectBinding? __scopeObjectBinding;"));
        Assert.That(generated, Does.Contain("protected global::LayerBase.Scope.ScopeRuntime OwnerScope"));
        Assert.That(generated, Does.Contain("protected int ServiceId => __scopeObjectBinding?.ServiceSlot ?? -1;"));
        Assert.That(generated, Does.Contain("protected global::LayerBase.Scope.ScopeRef<TScope> Scope<TScope>()"));
    }

    [Test]
    public void Scope_event_generator_emits_strongly_typed_post_extension()
    {
        const string source = """
                              namespace Game;

                              using LayerBase.Async;
                              using LayerBase.Async;
                              using LayerBase.Scope;

                              public sealed class CombatScope
                              {
                              }

                              [ScopeEvent<CombatScope>]
                              public readonly struct SpawnBulletEvent
                              {
                                  public SpawnBulletEvent(int bulletId)
                                  {
                                      BulletId = bulletId;
                                  }

                                  public int BulletId { get; }
                              }

                              public sealed class CombatGateway
                              {
                                  public bool Send(ScopeRef<CombatScope> scope)
                                  {
                                      return scope.Post(new SpawnBulletEvent(7));
                                  }
                              }
                              """;

        var result = RunGenerators(source, new ScopeRefPostGenerator());

        Assert.That(result.Diagnostics, Is.Empty,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        var errors = result.OutputCompilation.GetDiagnostics()
                           .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                           .ToImmutableArray();

        Assert.That(errors, Is.Empty,
            string.Join(Environment.NewLine, errors.Select(static error => error.ToString())));

        string generated = string.Join(Environment.NewLine, result.GeneratedSources);
        Assert.That(generated,
            Does.Contain(
                "public static bool Post(this global::LayerBase.Scope.ScopeRef<global::Game.CombatScope> scope, global::Game.SpawnBulletEvent message)"));
        Assert.That(generated, Does.Contain("return scope.TryPost("));
    }

    [Test]
    public void Scope_event_generator_assigns_ids_by_event_type_order()
    {
        const string source = """
                              namespace Game;

                              using LayerBase.Scope;

                              public sealed class AlphaScope
                              {
                              }

                              public sealed class ZuluScope
                              {
                              }

                              [ScopeEvent<ZuluScope>]
                              public readonly struct AlphaEvent
                              {
                              }

                              [ScopeEvent<AlphaScope>]
                              public readonly struct ZuluEvent
                              {
                              }

                              public sealed class Gateway
                              {
                                  public bool SendAlpha(ScopeRef<ZuluScope> scope)
                                  {
                                      return scope.Post(new AlphaEvent());
                                  }

                                  public bool SendZulu(ScopeRef<AlphaScope> scope)
                                  {
                                      return scope.Post(new ZuluEvent());
                                  }
                              }
                              """;

        var result = RunGenerators(source, new ScopeRefPostGenerator());

        Assert.That(result.Diagnostics, Is.Empty,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        string generated = string.Join(Environment.NewLine, result.GeneratedSources);
        Assert.That(generated,
            Does.Contain(
                "public static bool Post(this global::LayerBase.Scope.ScopeRef<global::Game.ZuluScope> scope, global::Game.AlphaEvent message)" +
                Environment.NewLine +
                "        {" +
                Environment.NewLine +
                "            return scope.TryPost(0, message);"));
        Assert.That(generated,
            Does.Contain(
                "public static bool Post(this global::LayerBase.Scope.ScopeRef<global::Game.AlphaScope> scope, global::Game.ZuluEvent message)" +
                Environment.NewLine +
                "        {" +
                Environment.NewLine +
                "            return scope.TryPost(1, message);"));
    }

    [Test]
    public void Scope_call_generator_emits_strongly_typed_call_extension()
    {
        const string source = """
                              namespace Game;

                              using LayerBase.Async;
                              using LayerBase.Scope;

                              public sealed class CombatScope
                              {
                              }

                              public readonly struct BulletTickResult
                              {
                                  public BulletTickResult(int value)
                                  {
                                      Value = value;
                                  }

                                  public int Value { get; }
                              }

                              [ScopeCall<CombatScope, BulletTickResult>]
                              public readonly struct BulletTickCall
                              {
                                  public BulletTickCall(float deltaTime)
                                  {
                                      DeltaTime = deltaTime;
                                  }

                                  public float DeltaTime { get; }
                              }

                              public sealed class CombatGateway
                              {
                                  public LBTask<BulletTickResult> Tick(ScopeRef<CombatScope> scope)
                                  {
                                      return scope.Call(new BulletTickCall(0.016f));
                                  }
                              }
                              """;

        var result = RunGenerators(source, new ScopeRefCallGenerator());

        Assert.That(result.Diagnostics, Is.Empty,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        var errors = result.OutputCompilation.GetDiagnostics()
                           .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                           .ToImmutableArray();

        Assert.That(errors, Is.Empty,
            string.Join(Environment.NewLine, errors.Select(static error => error.ToString())));

        string generated = string.Join(Environment.NewLine, result.GeneratedSources);
        Assert.That(generated,
            Does.Contain(
                "public static global::LayerBase.Async.LBTask<global::Game.BulletTickResult> Call(this global::LayerBase.Scope.ScopeRef<global::Game.CombatScope> scope, global::Game.BulletTickCall message)"));
        Assert.That(generated,
            Does.Contain("return scope.CallTask<global::Game.BulletTickResult>("));
    }

    [Test]
    public void Scope_call_generator_assigns_ids_by_request_type_order()
    {
        const string source = """
                              namespace Game;

                              using LayerBase.Async;
                              using LayerBase.Scope;

                              public sealed class AlphaScope
                              {
                              }

                              public sealed class ZuluScope
                              {
                              }

                              [ScopeCall<ZuluScope, int>]
                              public readonly struct AlphaCall
                              {
                              }

                              [ScopeCall<AlphaScope, int>]
                              public readonly struct ZuluCall
                              {
                              }

                              public sealed class Gateway
                              {
                                  public LBTask<int> SendAlpha(ScopeRef<ZuluScope> scope)
                                  {
                                      return scope.Call(new AlphaCall());
                                  }

                                  public LBTask<int> SendZulu(ScopeRef<AlphaScope> scope)
                                  {
                                      return scope.Call(new ZuluCall());
                                  }
                              }
                              """;

        var result = RunGenerators(source, new ScopeRefCallGenerator());

        Assert.That(result.Diagnostics, Is.Empty,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        string generated = string.Join(Environment.NewLine, result.GeneratedSources);
        Assert.That(generated,
            Does.Contain(
                "public static global::LayerBase.Async.LBTask<int> Call(this global::LayerBase.Scope.ScopeRef<global::Game.ZuluScope> scope, global::Game.AlphaCall message)" +
                Environment.NewLine +
                "        {" +
                Environment.NewLine +
                "            return scope.CallTask<int>(0, message);"));
        Assert.That(generated,
            Does.Contain(
                "public static global::LayerBase.Async.LBTask<int> Call(this global::LayerBase.Scope.ScopeRef<global::Game.AlphaScope> scope, global::Game.ZuluCall message)" +
                Environment.NewLine +
                "        {" +
                Environment.NewLine +
                "            return scope.CallTask<int>(1, message);"));
    }

    [Test]
    public void Scope_call_dispatch_generator_emits_switch_and_private_method_bridge()
    {
        const string source = """
                              namespace Game;

                              using LayerBase.Async;
                              using LayerBase.Async;
                              using LayerBase.DI;
                              using LayerBase.Scope;

                              public sealed class CombatScope
                              {
                              }

                              public readonly struct BulletTickResult
                              {
                                  public BulletTickResult(int value)
                                  {
                                      Value = value;
                                  }

                                  public int Value { get; }
                              }

                              [ScopeCall<CombatScope, BulletTickResult>]
                              public readonly struct BulletTickCall
                              {
                                  public BulletTickCall(int value)
                                  {
                                      Value = value;
                                  }

                                  public int Value { get; }
                              }

                              [Scope<CombatScope>]
                              public sealed partial class CombatService : IService
                              {
                                  [ScopeCall]
                                  private async LBTask<BulletTickResult> Tick(BulletTickCall call)
                                  {
                                      await LBTask.CompletedTask;
                                      return new BulletTickResult(call.Value + 1);
                                  }

                                  public void ConfigureServices(IServiceCollection services) { }
                              }
                              """;

        var result = RunGenerators(source, new ScopeCallDispatchGenerator());

        Assert.That(result.Diagnostics, Is.Empty,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        var errors = result.OutputCompilation.GetDiagnostics()
                           .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                           .ToImmutableArray();

        Assert.That(errors, Is.Empty,
            string.Join(Environment.NewLine, errors.Select(static error => error.ToString())));

        string generated = string.Join(Environment.NewLine, result.GeneratedSources);
        Assert.That(generated, Does.Contain("public static void Dispatch(global::LayerBase.Scope.ScopeRuntime scope, global::LayerBase.Scope.ScopeCallMessage message)"));
        Assert.That(generated, Does.Contain("switch (message.CallId)"));
        Assert.That(generated, Does.Contain("__LayerBaseScopeCall_"));
        Assert.That(generated, Does.Contain("var task = service.__LayerBaseScopeCall_"));
        Assert.That(generated, Does.Contain("awaiter.OnCompleted(() =>"));
    }

    [TestCase("LBSC001", """
                         namespace Game;

                         using LayerBase.DI;
                         using LayerBase.Scope;

                         public sealed class CombatScope
                         {
                         }

                         public readonly struct BulletTickResult
                         {
                         }

                         [ScopeCall<CombatScope, BulletTickResult>]
                         public readonly struct BulletTickCall
                         {
                         }

                         [Scope<CombatScope>]
                         public sealed partial class CombatService : IService
                         {
                             [ScopeCall]
                             private void Tick(BulletTickCall call)
                             {
                             }

                             public void ConfigureServices(IServiceCollection services) { }
                         }
                         """)]
    [TestCase("LBSC003", """
                         namespace Game;

                         using LayerBase.Async;
                         using LayerBase.DI;
                         using LayerBase.Scope;

                         public sealed class CombatScope
                         {
                         }

                         public readonly struct BulletTickResult
                         {
                         }

                         public readonly struct WrongResult
                         {
                         }

                         [ScopeCall<CombatScope, BulletTickResult>]
                         public readonly struct BulletTickCall
                         {
                         }

                         [Scope<CombatScope>]
                         public sealed partial class CombatService : IService
                         {
                             [ScopeCall]
                             private async LBTask<WrongResult> Tick(BulletTickCall call)
                             {
                                 await LBTask.CompletedTask;
                                 return default;
                             }

                             public void ConfigureServices(IServiceCollection services) { }
                         }
                         """)]
    [TestCase("LBSC004", """
                         namespace Game;

                         using LayerBase.Async;
                         using LayerBase.Scope;

                         public sealed class CombatScope
                         {
                         }

                         public readonly struct BulletTickResult
                         {
                         }

                         [ScopeCall<CombatScope, BulletTickResult>]
                         public readonly struct BulletTickCall
                         {
                         }

                         [Scope<CombatScope>]
                         public sealed partial class CombatHandler
                         {
                             [ScopeCall]
                             private async LBTask<BulletTickResult> Tick(BulletTickCall call)
                             {
                                 await LBTask.CompletedTask;
                                 return default;
                             }
                         }
                         """)]
    [TestCase("LBSC005", """
                         namespace Game;

                         using LayerBase.Async;
                         using LayerBase.DI;
                         using LayerBase.Scope;

                         public sealed class CombatScope
                         {
                         }

                         public readonly struct BulletTickResult
                         {
                         }

                         public readonly struct BulletTickCall
                         {
                         }

                         [Scope<CombatScope>]
                         public sealed partial class CombatService : IService
                         {
                             [ScopeCall]
                             private async LBTask<BulletTickResult> Tick(BulletTickCall call)
                             {
                                 await LBTask.CompletedTask;
                                 return default;
                             }

                             public void ConfigureServices(IServiceCollection services) { }
                         }
                         """)]
    [TestCase("LBSC006", """
                         namespace Game;

                         using LayerBase.DI;
                         using LayerBase.Scope;

                         public sealed class CombatScope
                         {
                         }

                         public readonly struct BulletTickResult
                         {
                         }

                         [ScopeCall<CombatScope, BulletTickResult>]
                         public readonly struct BulletTickCall
                         {
                         }

                         [Scope<CombatScope>]
                         public sealed partial class CombatService : IService
                         {
                             [ScopeCall]
                             private BulletTickResult Tick(BulletTickCall call)
                             {
                                 return default;
                             }

                             public void ConfigureServices(IServiceCollection services) { }
                         }
                         """)]
    public void Scope_call_dispatch_generator_reports_expected_diagnostic(string diagnosticId, string source)
    {
        var result = RunGenerators(source, new ScopeCallDispatchGenerator());

        Assert.That(result.Diagnostics.Select(static diagnostic => diagnostic.Id), Does.Contain(diagnosticId),
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
    }

    [Test]
    public void Scope_post_dispatch_generator_emits_switch_and_private_method_bridge()
    {
        const string source = """
                              namespace Game;

                              using LayerBase.Async;
                              using LayerBase.DI;
                              using LayerBase.Scope;

                              public sealed class CombatScope
                              {
                              }

                              [ScopeEvent<CombatScope>]
                              public readonly struct SpawnBulletEvent
                              {
                                  public SpawnBulletEvent(int bulletId)
                                  {
                                      BulletId = bulletId;
                                  }

                                  public int BulletId { get; }
                              }

                              [Scope<CombatScope>]
                              public sealed partial class CombatService : IService
                              {
                                  public int LastBulletId { get; private set; }

                                  [ScopeEvent]
                                  private void Spawn(SpawnBulletEvent message)
                                  {
                                      LastBulletId = message.BulletId;
                                  }

                                  public void ConfigureServices(IServiceCollection services) { }
                              }
                              """;

        var result = RunGenerators(source, new ScopePostDispatchGenerator());

        Assert.That(result.Diagnostics, Is.Empty,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        var errors = result.OutputCompilation.GetDiagnostics()
                           .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                           .ToImmutableArray();

        Assert.That(errors, Is.Empty,
            string.Join(Environment.NewLine, errors.Select(static error => error.ToString())));

        string generated = string.Join(Environment.NewLine, result.GeneratedSources);
        Assert.That(generated, Does.Contain("public static void Dispatch(global::LayerBase.Scope.ScopeRuntime scope, global::LayerBase.Scope.ScopePostMessage message)"));
        Assert.That(generated, Does.Contain("switch (message.EventId)"));
        Assert.That(generated, Does.Contain("__LayerBaseScopeEvent_"));
        Assert.That(generated, Does.Contain(".__LayerBaseScopeEvent_"));
    }

    [Test]
    public void Scope_post_dispatch_generator_allows_multiple_handlers_for_same_event_on_one_service()
    {
        const string source = """
                              namespace Game;

                              using LayerBase.DI;
                              using LayerBase.Scope;

                              public sealed class CombatScope
                              {
                              }

                              [ScopeEvent<CombatScope>]
                              public readonly struct SpawnBulletEvent
                              {
                                  public SpawnBulletEvent(int bulletId)
                                  {
                                      BulletId = bulletId;
                                  }

                                  public int BulletId { get; }
                              }

                              [Scope<CombatScope>]
                              public sealed partial class CombatService : IService
                              {
                                  public int FirstCount { get; private set; }
                                  public int SecondCount { get; private set; }

                                  [ScopeEvent]
                                  private void First(SpawnBulletEvent message)
                                  {
                                      FirstCount += message.BulletId;
                                  }

                                  [ScopeEvent]
                                  private void Second(SpawnBulletEvent message)
                                  {
                                      SecondCount += message.BulletId;
                                  }

                                  public void ConfigureServices(IServiceCollection services) { }
                              }
                              """;

        var result = RunGenerators(source, new ScopePostDispatchGenerator());

        Assert.That(result.Diagnostics, Is.Empty,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        var errors = result.OutputCompilation.GetDiagnostics()
                           .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                           .ToImmutableArray();

        Assert.That(errors, Is.Empty,
            string.Join(Environment.NewLine, errors.Select(static error => error.ToString())));

        string generated = string.Join(Environment.NewLine, result.GeneratedSources);
        Assert.That(generated, Does.Contain("First(message);"));
        Assert.That(generated, Does.Contain("Second(message);"));
    }

    [TestCase("LBSE001", """
                         namespace Game;

                         using LayerBase.DI;
                         using LayerBase.Scope;

                         public sealed class CombatScope
                         {
                         }

                         [ScopeEvent<CombatScope>]
                         public readonly struct SpawnBulletEvent
                         {
                         }

                         [Scope<CombatScope>]
                         public sealed partial class CombatService : IService
                         {
                             [ScopeEvent]
                             private int Spawn(SpawnBulletEvent message)
                             {
                                 return 1;
                             }

                             public void ConfigureServices(IServiceCollection services) { }
                         }
                         """)]
    [TestCase("LBSE002", """
                         namespace Game;

                         using LayerBase.DI;
                         using LayerBase.Scope;

                         public sealed class CombatScope
                         {
                         }

                         [ScopeEvent<CombatScope>]
                         public readonly struct SpawnBulletEvent
                         {
                         }

                         [Scope<CombatScope>]
                         public sealed class CombatService : IService
                         {
                             [ScopeEvent]
                             private void Spawn(SpawnBulletEvent message)
                             {
                             }

                             public void ConfigureServices(IServiceCollection services) { }
                         }
                         """)]
    [TestCase("LBSE003", """
                         namespace Game;

                         using LayerBase.Scope;

                         public sealed class CombatScope
                         {
                         }

                         [ScopeEvent<CombatScope>]
                         public readonly struct SpawnBulletEvent
                         {
                         }

                         [Scope<CombatScope>]
                         public sealed partial class CombatHandler
                         {
                             [ScopeEvent]
                             private void Spawn(SpawnBulletEvent message)
                             {
                             }
                         }
                         """)]
    [TestCase("LBSE004", """
                         namespace Game;

                         using LayerBase.DI;
                         using LayerBase.Scope;

                         public sealed class CombatScope
                         {
                         }

                         public readonly struct SpawnBulletEvent
                         {
                         }

                         [Scope<CombatScope>]
                         public sealed partial class CombatService : IService
                         {
                             [ScopeEvent]
                             private void Spawn(SpawnBulletEvent message)
                             {
                             }

                             public void ConfigureServices(IServiceCollection services) { }
                         }
                         """)]
    public void Scope_post_dispatch_generator_reports_expected_diagnostic(string diagnosticId, string source)
    {
        var result = RunGenerators(source, new ScopePostDispatchGenerator());

        Assert.That(result.Diagnostics.Select(static diagnostic => diagnostic.Id), Does.Contain(diagnosticId),
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
    }

    [Test]
    public void Scope_runtime_host_generator_emits_factory_with_generated_dispatchers()
    {
        const string source = """
                              namespace Game;

                              using LayerBase.Async;
                              using LayerBase.DI;
                              using LayerBase.Scope;

                              [ScopeOptions]
                              public sealed partial class CombatScope
                              {
                              }

                              [ScopeEvent<CombatScope>]
                              public readonly struct SpawnBulletEvent
                              {
                              }

                              public readonly struct BulletTickResult
                              {
                              }

                              [ScopeCall<CombatScope, BulletTickResult>]
                              public readonly struct BulletTickCall
                              {
                              }

                              [Scope<CombatScope>]
                              public sealed partial class CombatService : IService
                              {
                                  [ScopeEvent]
                                  private void Spawn(SpawnBulletEvent message)
                                  {
                                  }

                                  [ScopeCall]
                                  private async LBTask<BulletTickResult> Tick(BulletTickCall call)
                                  {
                                      await LBTask.CompletedTask;
                                      return default;
                                  }

                                  public void ConfigureServices(IServiceCollection services) { }
                              }
                              """;

        var result = RunGenerators(
            source,
            new ScopePostDispatchGenerator(),
            new ScopeCallDispatchGenerator(),
            new ScopeRuntimeHostGenerator());

        Assert.That(result.Diagnostics, Is.Empty,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        var errors = result.OutputCompilation.GetDiagnostics()
                           .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                           .ToImmutableArray();

        Assert.That(errors, Is.Empty,
            string.Join(Environment.NewLine, errors.Select(static error => error.ToString())));

        string generated = string.Join(Environment.NewLine, result.GeneratedSources);
        Assert.That(generated, Does.Contain("public static global::LayerBase.Scope.ScopeRuntimeHost Create(global::System.Collections.Generic.IReadOnlyList<global::LayerBase.DI.IService> services"));
        Assert.That(generated, Does.Contain("global::LayerBase.Scope.GeneratedScopeRuntimePlanner.Build(services)"));
        Assert.That(generated, Does.Contain("scopeTypeResolver: global::LayerBase.Scope.GeneratedScopeRuntimePlanner.TryGetScopeId"));
        Assert.That(generated, Does.Contain("public static bool TryGetScopeId(global::System.Type scopeType, out int scopeId)"));
        Assert.That(generated, Does.Contain("__LayerBaseCreateScopeDescriptor"));
        Assert.That(generated, Does.Contain("partial class CombatScope"));
        Assert.That(generated, Does.Contain("global::LayerBase.Scope.GeneratedScopePostDispatcher.Dispatch"));
        Assert.That(generated, Does.Contain("global::LayerBase.Scope.GeneratedScopeCallDispatcher.Dispatch"));
    }

    [Test]
    public void Scope_runtime_host_generator_reports_non_partial_scope_options_owner()
    {
        const string source = """
                              namespace Game;

                              using LayerBase.Scope;

                              [ScopeOptions]
                              public sealed class CombatScope
                              {
                              }
                              """;

        var result = RunGenerators(source, new ScopeRuntimeHostGenerator());

        Assert.That(result.Diagnostics.Select(static diagnostic => diagnostic.Id), Does.Contain("LBSD003"),
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
    }

    [Test]
    public void Scope_runtime_host_generator_reports_scope_attribute_owner_without_iservice()
    {
        const string source = """
                              namespace Game;

                              using LayerBase.Scope;

                              [ScopeOptions]
                              public sealed partial class CombatScope
                              {
                              }

                              [Scope<CombatScope>]
                              public sealed class CombatSystem
                              {
                              }
                              """;

        var result = RunGenerators(source, new ScopeRuntimeHostGenerator());

        Assert.That(result.Diagnostics.Select(static diagnostic => diagnostic.Id), Does.Contain("LBSD004"),
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
    }

    [TestCase("LBSD001", """
                         namespace Game;

                         using LayerBase.Scope;

                         [ScopeOptions(tickRateHz: -1)]
                         public sealed class CombatScope
                         {
                         }
                         """)]
    [TestCase("LBSD002", """
                         namespace Game;

                         using LayerBase.Scope;

                         [ScopeOptions(clock: ScopeClockMode.FixedRate)]
                         public sealed class CombatScope
                         {
                         }
                         """)]
    public void Scope_runtime_host_generator_reports_scope_options_diagnostic(string diagnosticId, string source)
    {
        var result = RunGenerators(source, new ScopeRuntimeHostGenerator());

        Assert.That(result.Diagnostics.Select(static diagnostic => diagnostic.Id), Does.Contain(diagnosticId),
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
    }

    [Test]
    public void Scope_resource_generator_emits_compilable_partial_type_declarations()
    {
        const string source = """
                              namespace Game.Resources;

                              using System.Collections.Generic;
                              using LayerBase.DI;
                              using LayerBase.Scope;

                              public sealed partial class InventoryService
                              {
                                  [Provide("items")]
                                  private readonly List<int> _items = new();
                              }

                              public sealed partial class InventoryQuery
                              {
                                  [From(typeof(InventoryService), "items")]
                                  private IReadOnlyList<int>? _items;
                              }
                              """;

        var result = RunGenerators(source, new ScopeResourceGenerator());

        Assert.That(result.Diagnostics, Is.Empty,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        var errors = result.OutputCompilation.GetDiagnostics()
                           .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                           .ToImmutableArray();

        Assert.That(errors, Is.Empty,
            string.Join(Environment.NewLine, errors.Select(static error => error.ToString())));

        string generated = string.Join(Environment.NewLine, result.GeneratedSources);
        Assert.That(generated, Does.Contain("namespace Game.Resources"));
        Assert.That(generated, Does.Contain("public sealed partial class InventoryService"));
        Assert.That(generated, Does.Not.Contain("partial class global::"));
    }

    [Test]
    public void Assembly_module_generator_emits_manifest_contributions_without_layer_or_scope_partial()
    {
        const string source = """
                              namespace Game.ModuleContract;

                              using LayerBase.Async;
                              using LayerBase.Core;
                              using LayerBase.DI;
                              using LayerBase.DI.Options;
                              using LayerBase.Layers;
                              using LayerBase.Modules;
                              using LayerBase.Scope;

                              [AssemblyModule]
                              public sealed partial class CombatModule
                              {
                              }

                              public sealed class GameplayLayer : Layer
                              {
                              }

                              [ScopeOptions(
                                  threading: ScopeThreadingMode.Worker,
                                  clock: ScopeClockMode.FixedRate,
                                  tickRateHz: 60,
                                  stopPolicy: ScopeStopPolicy.Drain)]
                              public sealed class CombatScope
                              {
                              }

                              public readonly struct DamageResult
                              {
                              }

                              [ScopeCall<CombatScope, DamageResult>]
                              public readonly struct CalculateDamageCall
                              {
                              }

                              [ScopeEvent<CombatScope>]
                              public readonly struct UpsertCombatantEvent
                              {
                              }

                              [OwnerLayer(typeof(GameplayLayer))]
                              [Scope<CombatScope>]
                              public sealed partial class CombatService : IService
                              {
                                  public void ConfigureServices(IServiceCollection services) { }

                                  [ScopeEvent]
                                  private void OnUpsertCombatant(UpsertCombatantEvent message)
                                  {
                                  }

                                  [ScopeCall]
                                  private async LBTask<DamageResult> OnCalculateDamage(CalculateDamageCall request)
                                  {
                                      await LBTask.CompletedTask;
                                      return default;
                                  }
                              }

                              [OwnerService(typeof(CombatService))]
                              public sealed partial class CombatContext : ILayerContext
                              {
                              }
                              """;

        var result = RunGenerators(source, new AssemblyModuleGenerator(), new ScopeRuntimeHostGenerator());

        Assert.That(result.Diagnostics, Is.Empty,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        var errors = result.OutputCompilation.GetDiagnostics()
                           .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                           .ToImmutableArray();

        Assert.That(errors, Is.Empty,
            string.Join(Environment.NewLine, errors.Select(static error => error.ToString())));

        string generated = string.Join(Environment.NewLine, result.GeneratedSources);
        Assert.That(generated, Does.Contain("partial class CombatModule : global::LayerBase.Modules.ILayerBaseModule"));
        Assert.That(generated, Does.Contain("new global::LayerBase.Modules.LayerContractContribution(typeof(global::Game.ModuleContract.GameplayLayer).TypeHandle)"));
        Assert.That(generated, Does.Contain("new global::LayerBase.Modules.ScopeDefinitionContribution("));
        Assert.That(generated, Does.Contain("typeof(global::Game.ModuleContract.CombatScope).TypeHandle"));
        Assert.That(generated, Does.Contain("global::LayerBase.Modules.ScopeMessageKind.Call"));
        Assert.That(generated, Does.Contain("global::LayerBase.Modules.ScopeMessageKind.Event"));
        Assert.That(generated, Does.Contain("new global::LayerBase.Modules.ServiceContribution("));
        Assert.That(generated, Does.Contain("typeof(global::Game.ModuleContract.CombatService).TypeHandle"));
        Assert.That(generated, Does.Contain("typeof(global::Game.ModuleContract.GameplayLayer).TypeHandle"));
        Assert.That(generated, Does.Contain("new global::LayerBase.Modules.ContextContribution("));
        Assert.That(generated, Does.Contain("new global::LayerBase.Modules.ScopeHandlerContribution("));
        Assert.That(generated, Does.Not.Contain("partial class GameplayLayer"));
        Assert.That(generated, Does.Not.Contain("partial class CombatScope"));
    }

    [Test]
    public void Assembly_module_generator_reports_non_partial_module_owner()
    {
        const string source = """
                              using LayerBase.Modules;

                              namespace Game.ModuleContract;

                              [AssemblyModule]
                              public sealed class CombatModule
                              {
                              }
                              """;

        var result = RunGenerators(source, new AssemblyModuleGenerator());

        Assert.That(result.Diagnostics.Select(static diagnostic => diagnostic.Id), Does.Contain("LBM001"),
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
    }

    [Test]
    public void Module_catalog_generator_emits_current_assembly_modules()
    {
        const string source = """
                              using LayerBase.Modules;

                              namespace Game.Bootstrap;

                              [AssemblyModule]
                              public sealed partial class BootstrapModule
                              {
                              }
                              """;

        var result = RunGenerators(source, new AssemblyModuleGenerator(), new ModuleCatalogGenerator());

        Assert.That(result.Diagnostics, Is.Empty,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        var errors = result.OutputCompilation.GetDiagnostics()
                           .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                           .ToImmutableArray();

        Assert.That(errors, Is.Empty,
            string.Join(Environment.NewLine, errors.Select(static error => error.ToString())));

        string generated = string.Join(Environment.NewLine, result.GeneratedSources);
        Assert.That(generated, Does.Contain("public static class GeneratedModuleCatalog"));
        Assert.That(generated, Does.Contain("global::Game.Bootstrap.BootstrapModule.Instance"));
    }

    private static GeneratorTestResult RunGenerators(string source, params IIncrementalGenerator[] generators)
    {
        return RunGeneratorsWithReferences(source, Array.Empty<MetadataReference>(), generators);
    }

    private static GeneratorTestResult RunGeneratorsWithReferences(
        string source,
        IEnumerable<MetadataReference> additionalReferences,
        params IIncrementalGenerator[] generators)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));
        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "LayerGeneratorTests_" + Guid.NewGuid().ToString("N"),
            syntaxTrees: [syntaxTree],
            references: GetMetadataReferences().Concat(additionalReferences),
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

    private static MetadataReference CompileReferenceAssembly(
        string source,
        params IIncrementalGenerator[] generators)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));
        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "LayerGeneratorReference_" + Guid.NewGuid().ToString("N"),
            syntaxTrees: [syntaxTree],
            references: GetMetadataReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        Compilation outputCompilation = compilation;
        if (generators.Length > 0)
        {
            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                generators.Select(static generator => generator.AsSourceGenerator()).ToArray(),
                parseOptions: new CSharpParseOptions(LanguageVersion.Preview));
            driver.RunGeneratorsAndUpdateCompilation(compilation, out outputCompilation, out _);
        }

        using var stream = new MemoryStream();
        var emitResult = outputCompilation.Emit(stream);
        Assert.That(emitResult.Success, Is.True,
            string.Join(Environment.NewLine, emitResult.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        return MetadataReference.CreateFromImage(stream.ToArray());
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
