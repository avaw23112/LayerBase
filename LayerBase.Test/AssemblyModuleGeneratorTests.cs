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
    public void Assembly_module_generator_emits_empty_static_manifest_for_explicit_module_root()
    {
        const string source = """
                              using LayerBase.Modules;

                              namespace Sample;

                              [AssemblyModule("gameplay")]
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
        Assert.That(generatedSource, Does.Contain("public static GameplayModule Instance { get; } = new GameplayModule();"));
        Assert.That(generatedSource, Does.Contain("new global::LayerBase.Modules.AssemblyModuleId(\"gameplay\")"));
        Assert.That(generatedSource, Does.Contain("global::System.Array.Empty<global::LayerBase.Modules.ServiceContribution>()"));
    }

    [Test]
    public void Cross_assembly_owner_layer_service_is_transferred_to_single_assembly_module()
    {
        var aotReference = CreateReference("AotGame", """
                                                       using LayerBase.Layers;

                                                       namespace AotGame;

                                                       public sealed class GameplayLayer : Layer
                                                       {
                                                       }
                                                       """);

        const string source = """
                              using System;
                              using AotGame;
                              using LayerBase.DI;
                              using LayerBase.Layers;
                              using LayerBase.Modules;

                              namespace FeaturePack;

                              [AssemblyModule("feature")]
                              public sealed partial class FeatureModule
                              {
                              }

                              [OwnerLayer(typeof(GameplayLayer))]
                              public sealed partial class InventoryService : IService
                              {
                                  public void ConfigureServices(IServiceCollection services) { }
                              }
                              """;

        var result = RunGenerators(source, [aotReference], new AssemblyModuleGenerator(), new LayerServiceGenerator());

        Assert.That(result.Diagnostics, Is.Empty,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        var errors = result.OutputCompilation.GetDiagnostics()
                           .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                           .ToImmutableArray();

        Assert.That(errors, Is.Empty,
            string.Join(Environment.NewLine, errors.Select(static error => error.ToString())));

        var generatedModule = result.GeneratedSources.Single(static sourceText =>
            sourceText.Contains("partial class FeatureModule"));

        Assert.That(generatedModule, Does.Contain("global::LayerBase.Modules.ServiceContribution.ForTypes("));
        Assert.That(generatedModule, Does.Contain("typeof(global::FeaturePack.InventoryService)"));
        Assert.That(generatedModule, Does.Contain("typeof(global::AotGame.GameplayLayer)"));
        Assert.That(generatedModule, Does.Contain("typeof(global::LayerBase.Scope.MainScope)"));
        Assert.That(result.GeneratedSources, Has.None.Contains("__AutoMountServices"));
    }

    [Test]
    public void Cross_assembly_owner_layer_service_uses_scope_attribute_when_declared()
    {
        var aotReference = CreateReference("AotGame", """
                                                       using LayerBase.Layers;
                                                       using LayerBase.Scope;

                                                       namespace AotGame;

                                                       public sealed class GameplayLayer : Layer
                                                       {
                                                       }

                                                       public readonly struct BattleScope : IScopeDefinition
                                                       {
                                                       }
                                                       """);

        const string source = """
                              using System;
                              using AotGame;
                              using LayerBase.DI;
                              using LayerBase.Layers;
                              using LayerBase.Modules;
                              using LayerBase.Scope;

                              [AssemblyModule("feature")]
                              public sealed partial class FeatureModule
                              {
                              }

                              [Scope<BattleScope>]
                              [OwnerLayer(typeof(GameplayLayer))]
                              public sealed partial class InventoryService : IService
                              {
                                  public void ConfigureServices(IServiceCollection services) { }
                              }
                              """;

        var result = RunGenerators(source, [aotReference], new AssemblyModuleGenerator(), new LayerServiceGenerator());

        Assert.That(result.Diagnostics, Is.Empty,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        var generatedModule = result.GeneratedSources.Single(static sourceText =>
            sourceText.Contains("partial class FeatureModule"));

        Assert.That(generatedModule, Does.Contain("typeof(global::AotGame.BattleScope)"));
        Assert.That(generatedModule, Does.Not.Contain("typeof(global::LayerBase.Scope.MainScope)"));
    }

    [Test]
    public void Cross_assembly_owner_layer_call_handler_is_transferred_to_single_assembly_module()
    {
        var aotReference = CreateReference("AotGame", """
                                                       using LayerBase.Layers;

                                                       namespace AotGame;

                                                       public sealed class GameplayLayer : Layer
                                                       {
                                                       }
                                                       """);

        const string source = """
                              using AotGame;
                              using System.Threading;
                              using LayerBase.Async;
                              using LayerBase.Call;
                              using LayerBase.Layers;
                              using LayerBase.Modules;

                              namespace FeaturePack;

                              [AssemblyModule("feature")]
                              public sealed partial class FeatureModule
                              {
                              }

                              public readonly struct OpenInventoryRequest
                              {
                              }

                              public readonly struct OpenInventoryResponse
                              {
                              }

                              [OwnerLayer(typeof(GameplayLayer))]
                              public sealed class OpenInventoryCallHandler
                                  : IScopeLocalCallHandler<OpenInventoryRequest, OpenInventoryResponse>
                              {
                                  public async LBTask<OpenInventoryResponse> HandleAsync(
                                      OpenInventoryRequest request,
                                      CancellationToken cancellationToken = default)
                                  {
                                      await LBTask.CompletedTask;
                                      return new OpenInventoryResponse();
                                  }
                              }
                              """;

        var result = RunGenerators(source, [aotReference], new AssemblyModuleGenerator(), new LayerServiceGenerator());

        Assert.That(result.Diagnostics, Is.Empty,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        var errors = result.OutputCompilation.GetDiagnostics()
                           .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                           .ToImmutableArray();

        Assert.That(errors, Is.Empty,
            string.Join(Environment.NewLine, errors.Select(static error => error.ToString())));

        var generatedModule = result.GeneratedSources.Single(static sourceText =>
            sourceText.Contains("partial class FeatureModule"));

        Assert.That(generatedModule, Does.Contain("global::LayerBase.Modules.LocalCallContribution.ForTypes("));
        Assert.That(generatedModule, Does.Contain("typeof(global::FeaturePack.OpenInventoryRequest)"));
        Assert.That(generatedModule, Does.Contain("typeof(global::FeaturePack.OpenInventoryResponse)"));
        Assert.That(generatedModule, Does.Contain("typeof(global::FeaturePack.OpenInventoryCallHandler)"));
        Assert.That(generatedModule, Does.Contain("typeof(global::AotGame.GameplayLayer)"));
        Assert.That(generatedModule, Does.Contain("typeof(global::LayerBase.Scope.MainScope)"));
        Assert.That(generatedModule, Does.Not.Contain("ServiceContribution.ForTypes"));
        Assert.That(result.GeneratedSources, Has.None.Contains("__AutoMountServices"));
    }

    [Test]
    public void Cross_assembly_owner_service_context_is_transferred_to_single_assembly_module()
    {
        var aotReference = CreateReference("AotGame", """
                                                       using LayerBase.DI;
                                                       using LayerBase.Layers;
                                                       using LayerBase.Scope;

                                                       namespace AotGame;

                                                       public sealed class GameplayLayer : Layer
                                                       {
                                                       }

                                                       public readonly struct BattleScope : IScopeDefinition
                                                       {
                                                       }

                                                       [Scope<BattleScope>]
                                                       [OwnerLayer(typeof(GameplayLayer))]
                                                       public sealed partial class InventoryService : IService
                                                       {
                                                           public void ConfigureServices(IServiceCollection services) { }
                                                       }
                                                       """);

        const string source = """
                              using AotGame;
                              using LayerBase.DI;
                              using LayerBase.DI.Options;
                              using LayerBase.Modules;

                              namespace FeaturePack;

                              [AssemblyModule("feature")]
                              public sealed partial class FeatureModule
                              {
                              }

                              [OwnerService(typeof(InventoryService))]
                              public sealed class InventoryContext : ILayerContext
                              {
                              }
                              """;

        var result = RunGenerators(source, [aotReference], new AssemblyModuleGenerator(), new LayerServiceGenerator());

        Assert.That(result.Diagnostics, Is.Empty,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        var errors = result.OutputCompilation.GetDiagnostics()
                           .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                           .ToImmutableArray();

        Assert.That(errors, Is.Empty,
            string.Join(Environment.NewLine, errors.Select(static error => error.ToString())));

        var generatedModule = result.GeneratedSources.Single(static sourceText =>
            sourceText.Contains("partial class FeatureModule"));

        Assert.That(generatedModule, Does.Contain("global::LayerBase.Modules.ContextContribution.ForTypes("));
        Assert.That(generatedModule, Does.Contain("typeof(global::FeaturePack.InventoryContext)"));
        Assert.That(generatedModule, Does.Contain("typeof(global::AotGame.InventoryService)"));
        Assert.That(generatedModule, Does.Contain("typeof(global::AotGame.GameplayLayer)"));
        Assert.That(generatedModule, Does.Contain("typeof(global::AotGame.BattleScope)"));
        Assert.That(generatedModule, Does.Not.Contain("typeof(global::LayerBase.Scope.MainScope)"));
        Assert.That(result.GeneratedSources, Has.None.Contains("__AutoMountContexts"));
    }

    [Test]
    public void Cross_assembly_owner_service_event_handler_is_transferred_to_single_assembly_module()
    {
        var aotReference = CreateReference("AotGame", """
                                                       using LayerBase.DI;
                                                       using LayerBase.Layers;

                                                       namespace AotGame;

                                                       public sealed class GameplayLayer : Layer
                                                       {
                                                       }

                                                       [OwnerLayer(typeof(GameplayLayer))]
                                                       public sealed partial class InventoryService : IService
                                                       {
                                                           public void ConfigureServices(IServiceCollection services) { }
                                                       }
                                                       """);

        const string source = """
                              using AotGame;
                              using LayerBase.Core.EventHandler;
                              using LayerBase.DI.Options;
                              using LayerBase.Modules;

                              namespace FeaturePack;

                              [AssemblyModule("feature")]
                              public sealed partial class FeatureModule
                              {
                              }

                              public readonly struct InventoryChanged
                              {
                              }

                              [OwnerService(typeof(InventoryService))]
                              public sealed class InventoryChangedHandler : IEventHandler<InventoryChanged>
                              {
                                  public void Deal(in InventoryChanged @event) { }
                              }
                              """;

        var result = RunGenerators(source, [aotReference], new AssemblyModuleGenerator(), new LayerServiceGenerator());

        Assert.That(result.Diagnostics, Is.Empty,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        var errors = result.OutputCompilation.GetDiagnostics()
                           .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                           .ToImmutableArray();

        Assert.That(errors, Is.Empty,
            string.Join(Environment.NewLine, errors.Select(static error => error.ToString())));

        var generatedModule = result.GeneratedSources.Single(static sourceText =>
            sourceText.Contains("partial class FeatureModule"));

        Assert.That(generatedModule, Does.Contain("global::LayerBase.Modules.EventHandlerContribution.ForTypes("));
        Assert.That(generatedModule, Does.Contain("typeof(global::FeaturePack.InventoryChanged)"));
        Assert.That(generatedModule, Does.Contain("typeof(global::FeaturePack.InventoryChangedHandler)"));
        Assert.That(generatedModule, Does.Contain("typeof(global::AotGame.InventoryService)"));
        Assert.That(generatedModule, Does.Contain("typeof(global::AotGame.GameplayLayer)"));
        Assert.That(generatedModule, Does.Contain("typeof(global::LayerBase.Scope.MainScope)"));
        Assert.That(generatedModule, Does.Not.Contain("global::LayerBase.Modules.ContextContribution.ForTypes("));
    }

    [Test]
    public void Cross_assembly_owner_layer_service_requires_module_root()
    {
        var aotReference = CreateReference("AotGame", """
                                                       using LayerBase.Layers;

                                                       namespace AotGame;

                                                       public sealed class GameplayLayer : Layer
                                                       {
                                                       }
                                                       """);

        const string source = """
                              using AotGame;
                              using LayerBase.DI;
                              using LayerBase.Layers;

                              [OwnerLayer(typeof(GameplayLayer))]
                              public sealed partial class InventoryService : IService
                              {
                                  public void ConfigureServices(IServiceCollection services) { }
                              }
                              """;

        var result = RunGenerators(source, [aotReference], new AssemblyModuleGenerator(), new LayerServiceGenerator());

        Assert.That(result.Diagnostics.Select(static diagnostic => diagnostic.Id), Does.Contain("LBMOD001"),
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
    }

    [Test]
    public void Cross_assembly_owner_layer_service_requires_single_module_root()
    {
        var aotReference = CreateReference("AotGame", """
                                                       using LayerBase.Layers;

                                                       namespace AotGame;

                                                       public sealed class GameplayLayer : Layer
                                                       {
                                                       }
                                                       """);

        const string source = """
                              using AotGame;
                              using LayerBase.DI;
                              using LayerBase.Layers;
                              using LayerBase.Modules;

                              [AssemblyModule("inventory")]
                              public sealed partial class InventoryModule
                              {
                              }

                              [AssemblyModule("combat")]
                              public sealed partial class CombatModule
                              {
                              }

                              [OwnerLayer(typeof(GameplayLayer))]
                              public sealed partial class InventoryService : IService
                              {
                                  public void ConfigureServices(IServiceCollection services) { }
                              }
                              """;

        var result = RunGenerators(source, [aotReference], new AssemblyModuleGenerator(), new LayerServiceGenerator());

        Assert.That(result.Diagnostics.Select(static diagnostic => diagnostic.Id), Does.Contain("LBMOD002"),
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
    }

    [Test]
    public void Module_ignore_suppresses_cross_assembly_owner_layer_fallback()
    {
        var aotReference = CreateReference("AotGame", """
                                                       using LayerBase.Layers;

                                                       namespace AotGame;

                                                       public sealed class GameplayLayer : Layer
                                                       {
                                                       }
                                                       """);

        const string source = """
                              using AotGame;
                              using LayerBase.DI;
                              using LayerBase.Layers;
                              using LayerBase.Modules;

                              namespace FeaturePack;

                              [AssemblyModule("feature")]
                              public sealed partial class FeatureModule
                              {
                              }

                              [ModuleIgnore]
                              [OwnerLayer(typeof(GameplayLayer))]
                              public sealed partial class InventoryService : IService
                              {
                                  public void ConfigureServices(IServiceCollection services) { }
                              }
                              """;

        var result = RunGenerators(source, [aotReference], new AssemblyModuleGenerator(), new LayerServiceGenerator());

        Assert.That(result.Diagnostics, Is.Empty,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        var generatedModule = result.GeneratedSources.Single(static sourceText =>
            sourceText.Contains("partial class FeatureModule"));

        Assert.That(generatedModule, Does.Not.Contain("ServiceContribution.ForTypes"));
        Assert.That(generatedModule, Does.Not.Contain("typeof(global::FeaturePack.InventoryService)"));
    }

    [Test]
    public void Module_ignore_suppresses_cross_assembly_owner_service_fallback()
    {
        var aotReference = CreateReference("AotGame", """
                                                       using LayerBase.DI;
                                                       using LayerBase.Layers;

                                                       namespace AotGame;

                                                       public sealed class GameplayLayer : Layer
                                                       {
                                                       }

                                                       [OwnerLayer(typeof(GameplayLayer))]
                                                       public sealed partial class InventoryService : IService
                                                       {
                                                           public void ConfigureServices(IServiceCollection services) { }
                                                       }
                                                       """);

        const string source = """
                              using AotGame;
                              using LayerBase.DI;
                              using LayerBase.DI.Options;
                              using LayerBase.Modules;

                              namespace FeaturePack;

                              [AssemblyModule("feature")]
                              public sealed partial class FeatureModule
                              {
                              }

                              [ModuleIgnore]
                              [OwnerService(typeof(InventoryService))]
                              public sealed class InventoryContext : ILayerContext
                              {
                              }
                              """;

        var result = RunGenerators(source, [aotReference], new AssemblyModuleGenerator(), new LayerServiceGenerator());

        Assert.That(result.Diagnostics, Is.Empty,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        var generatedModule = result.GeneratedSources.Single(static sourceText =>
            sourceText.Contains("partial class FeatureModule"));

        Assert.That(generatedModule, Does.Not.Contain("ContextContribution.ForTypes"));
        Assert.That(generatedModule, Does.Not.Contain("typeof(global::FeaturePack.InventoryContext)"));
    }

    [Test]
    public void Cross_assembly_layer_tool_is_transferred_to_single_assembly_module()
    {
        var aotReference = CreateReference("AotGame", """
                                                       using LayerBase.Layers;

                                                       namespace AotGame;

                                                       public sealed class CommerceLayer : Layer
                                                       {
                                                       }
                                                       """);

        const string source = """
                              using System;
                              using AotGame;
                              using LayerBase.Layers;
                              using LayerBase.Modules;
                              using LayerBase.Scope;
                              using LayerBase.Tools;

                              namespace FeaturePack;

                              [AssemblyModule("fulfillment")]
                              public sealed partial class FulfillmentModule
                              {
                              }

                              public interface IShippingLabelTool
                              {
                              }

                              public sealed class FulfillmentScope : IScopeDefinition
                              {
                                  public const int ScopeId = 16;
                              }

                              [LayerTool("shipping.label", typeof(CommerceLayer), typeof(FulfillmentScope), Contract = typeof(IShippingLabelTool))]
                              [AttributeUsage(AttributeTargets.Class, Inherited = false)]
                              public sealed class ShippingToolAttribute : Attribute
                              {
                                  public string Key { get; set; } = "default";
                                  public bool Cache { get; set; } = true;
                              }

                              [ShippingTool(Key = "labels")]
                              public sealed class ShippingLabelTool : IShippingLabelTool
                              {
                              }
                              """;

        var result = RunGenerators(source, [aotReference], new AssemblyModuleGenerator());

        Assert.That(result.Diagnostics, Is.Empty,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        var errors = result.OutputCompilation.GetDiagnostics()
                           .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                           .ToImmutableArray();

        Assert.That(errors, Is.Empty,
            string.Join(Environment.NewLine, errors.Select(static error => error.ToString())));

        var generatedModule = result.GeneratedSources.Single(static sourceText =>
            sourceText.Contains("partial class FulfillmentModule"));

        Assert.That(generatedModule, Does.Contain("global::LayerBase.Modules.LayerToolContribution.ForTypes("));
        Assert.That(generatedModule, Does.Contain("typeof(global::FeaturePack.IShippingLabelTool)"));
        Assert.That(generatedModule, Does.Contain("typeof(global::FeaturePack.ShippingLabelTool)"));
        Assert.That(generatedModule, Does.Contain("typeof(global::AotGame.CommerceLayer)"));
        Assert.That(generatedModule, Does.Contain("typeof(global::FeaturePack.FulfillmentScope)"));
        Assert.That(generatedModule, Does.Contain("\"labels\""));
    }

    [Test]
    public void Same_assembly_layer_tool_is_not_transferred_to_assembly_module()
    {
        const string source = """
                              using System;
                              using LayerBase.Layers;
                              using LayerBase.Modules;
                              using LayerBase.Scope;
                              using LayerBase.Tools;

                              namespace FeaturePack;

                              [AssemblyModule("fulfillment")]
                              public sealed partial class FulfillmentModule
                              {
                              }

                              public sealed class CommerceLayer : Layer
                              {
                              }

                              public interface IShippingLabelTool
                              {
                              }

                              public sealed class FulfillmentScope : IScopeDefinition
                              {
                                  public const int ScopeId = 16;
                              }

                              [LayerTool("shipping.label", typeof(CommerceLayer), typeof(FulfillmentScope), Contract = typeof(IShippingLabelTool))]
                              [AttributeUsage(AttributeTargets.Class, Inherited = false)]
                              public sealed class ShippingToolAttribute : Attribute
                              {
                                  public string Key { get; set; } = "default";
                              }

                              [ShippingTool]
                              public sealed class ShippingLabelTool : IShippingLabelTool
                              {
                              }
                              """;

        var result = RunGenerators(source, new AssemblyModuleGenerator());

        Assert.That(result.Diagnostics, Is.Empty,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        var generatedModule = result.GeneratedSources.Single(static sourceText =>
            sourceText.Contains("partial class FulfillmentModule"));

        Assert.That(generatedModule, Does.Not.Contain("LayerToolContribution.ForTypes"));
        Assert.That(generatedModule, Does.Not.Contain("typeof(global::FeaturePack.ShippingLabelTool)"));
    }

    [Test]
    public void Layer_service_generator_emits_local_layer_tool_provider_for_same_assembly_layer_tool()
    {
        const string source = """
                              using System;
                              using LayerBase.Layers;
                              using LayerBase.Scope;
                              using LayerBase.Tools;

                              namespace FeaturePack;

                              public sealed partial class CommerceLayer : Layer
                              {
                              }

                              public interface IShippingLabelTool
                              {
                              }

                              public sealed class FulfillmentScope : IScopeDefinition
                              {
                                  public const int ScopeId = 16;
                              }

                              [LayerTool("shipping.label", typeof(CommerceLayer), typeof(FulfillmentScope), Contract = typeof(IShippingLabelTool))]
                              [AttributeUsage(AttributeTargets.Class, Inherited = false)]
                              public sealed class ShippingToolAttribute : Attribute
                              {
                                  public string Key { get; set; } = "default";
                              }

                              [ShippingTool(Key = "labels")]
                              public sealed class ShippingLabelTool : IShippingLabelTool
                              {
                              }
                              """;

        var result = RunGenerators(source, new LayerServiceGenerator());

        Assert.That(result.Diagnostics, Is.Empty,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        var errors = result.OutputCompilation.GetDiagnostics()
                           .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                           .ToImmutableArray();

        Assert.That(errors, Is.Empty,
            string.Join(Environment.NewLine, errors.Select(static error => error.ToString())));

        var generatedLayer = result.GeneratedSources.Single(static sourceText =>
            sourceText.Contains("partial class CommerceLayer"));

        Assert.That(generatedLayer, Does.Contain("global::LayerBase.Tools.IGeneratedLayerToolProvider"));
        Assert.That(generatedLayer, Does.Contain("global::LayerBase.Modules.LayerToolContribution.ForTypes("));
        Assert.That(generatedLayer, Does.Contain("typeof(global::FeaturePack.IShippingLabelTool)"));
        Assert.That(generatedLayer, Does.Contain("typeof(global::FeaturePack.ShippingLabelTool)"));
        Assert.That(generatedLayer, Does.Contain("typeof(global::FeaturePack.CommerceLayer)"));
        Assert.That(generatedLayer, Does.Contain("typeof(global::FeaturePack.FulfillmentScope)"));
        Assert.That(generatedLayer, Does.Contain("\"labels\""));
    }

    [Test]
    public void Layer_service_generator_emits_scope_provider_for_non_generic_scope_attribute()
    {
        const string source = """
                              using LayerBase.DI;
                              using LayerBase.Layers;
                              using LayerBase.Scope;

                              namespace FeaturePack;

                              public sealed partial class CommerceLayer : Layer
                              {
                              }

                              public sealed class FulfillmentScope : IScopeDefinition
                              {
                                  public const int ScopeId = 16;
                              }

                              [OwnerLayer(typeof(CommerceLayer))]
                              [Scope(typeof(FulfillmentScope))]
                              public sealed partial class FulfillmentService : IService
                              {
                              }
                              """;

        var result = RunGenerators(source, new LayerServiceGenerator());

        Assert.That(result.Diagnostics, Is.Empty,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        var generatedLayer = result.GeneratedSources.Single(static sourceText =>
            sourceText.Contains("partial class CommerceLayer"));

        Assert.That(generatedLayer, Does.Contain("global::LayerBase.Scope.IGeneratedScopeDefinitionProvider"));
        Assert.That(generatedLayer, Does.Contain("typeof(global::FeaturePack.FulfillmentScope)"));
    }

    [Test]
    public void Assembly_module_generator_does_not_emit_runtime_ownership_or_instance_creation()
    {
        var aotReference = CreateReference("AotGame", """
                                                       using LayerBase.Layers;

                                                       namespace AotGame;

                                                       public sealed class GameplayLayer : Layer
                                                       {
                                                       }
                                                       """);

        const string source = """
                              using AotGame;
                              using LayerBase.DI;
                              using LayerBase.Layers;
                              using LayerBase.Modules;

                              [AssemblyModule("gameplay")]
                              public sealed partial class GameplayModule
                              {
                              }

                              [OwnerLayer(typeof(GameplayLayer))]
                              public sealed partial class GameService : IService
                              {
                                  public void ConfigureServices(IServiceCollection services) { }
                              }
                              """;

        var result = RunGenerators(source, [aotReference], new AssemblyModuleGenerator(), new LayerServiceGenerator());

        var generatedSource = result.GeneratedSources.Single(static sourceText =>
            sourceText.Contains("partial class GameplayModule"));

        Assert.That(generatedSource, Does.Not.Contain(".Push("));
        Assert.That(generatedSource, Does.Not.Contain("GetAssemblies"));
        Assert.That(generatedSource, Does.Not.Contain("ScopeRuntime"));
        Assert.That(generatedSource, Does.Not.Contain("new global::GameService"));
    }

    private static GeneratorTestResult RunGenerators(string source, params IIncrementalGenerator[] generators)
    {
        return RunGenerators(source, [], generators);
    }

    private static GeneratorTestResult RunGenerators(
        string source,
        IEnumerable<MetadataReference> additionalReferences,
        params IIncrementalGenerator[] generators)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));
        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "AssemblyModuleGeneratorTests_" + Guid.NewGuid().ToString("N"),
            syntaxTrees: [syntaxTree],
            references: GetMetadataReferences().Concat(additionalReferences),
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

    private static MetadataReference CreateReference(string assemblyName, string source)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));
        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName,
            syntaxTrees: [syntaxTree],
            references: GetMetadataReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        Assert.That(result.Success, Is.True,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

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
