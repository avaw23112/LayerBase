using System.Collections.Immutable;
using Arch.Core;
using LayerBase.Async;
using LayerBase.DI;
using LayerBase.ECS;
using LayerBase.Generator;
using LayerBase.Generator.Diagnostics;
using LayerBase.Layers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LayerBase.Test;

[TestFixture]
public sealed class QueryInputGeneratorTests
{
    [Test]
    public void Input_attribute_has_parameter_target()
    {
        AttributeUsageAttribute usage = typeof(InputAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), inherit: false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        Assert.That(usage.ValidOn, Is.EqualTo(AttributeTargets.Parameter));
        Assert.That(usage.AllowMultiple, Is.False);
        Assert.That(usage.Inherited, Is.False);
    }

    [Test]
    public void Generated_entry_remains_void_and_contains_input_parameters()
    {
        var result = RunQueryGenerator(QueryWithInputSource);

        AssertNoGeneratorDiagnostics(result);
        string generated = SingleGeneratedSource(result);

        Assert.That(generated, Does.Contain("public void Move("));
        Assert.That(generated, Does.Contain("in global::FrameInput frame"));
        Assert.That(generated, Does.Contain("global::MovementConfig config"));
        Assert.That(generated, Does.Not.Contain("public global::"));
    }

    [Test]
    public void Generated_job_captures_inputs_without_adding_them_to_query_shape()
    {
        var result = RunQueryGenerator(QueryWithInputSource);

        AssertNoGeneratorDiagnostics(result);
        string generated = SingleGeneratedSource(result);

        Assert.That(generated, Does.Contain("private readonly global::FrameInput _input0;"));
        Assert.That(generated, Does.Contain("private readonly global::MovementConfig _input1;"));
        Assert.That(generated, Does.Contain("public __MoveJob("));
        Assert.That(generated, Does.Contain("in global::FrameInput input0"));
        Assert.That(generated, Does.Contain("global::MovementConfig input1"));
        Assert.That(generated, Does.Contain("new __MoveJob("));
        Assert.That(generated, Does.Contain("in frame"));
        Assert.That(generated, Does.Contain("config"));
        Assert.That(generated, Does.Contain(".Query<global::PositionComponent, global::VelocityComponent>(this)"));
        Assert.That(generated, Does.Not.Contain("IQueryJob<global::PositionComponent, global::VelocityComponent, global::FrameInput"));
        Assert.That(generated, Does.Not.Contain("in global::FrameInput c"));
    }

    [Test]
    public void Generated_user_call_preserves_parameter_order_and_in_forwarding()
    {
        var result = RunQueryGenerator(QueryWithInputSource);

        AssertNoGeneratorDiagnostics(result);
        string generated = SingleGeneratedSource(result);

        Assert.That(generated, Does.Contain("_self.OnMove(ref c0, in c1, in _input0, _input1);"));
    }

    [Test]
    public void Bring_query_can_use_input_and_keeps_batch_post_chain()
    {
        var result = RunQueryGenerator(BringQueryWithInputSource);

        AssertNoGeneratorDiagnostics(result);
        string generated = SingleGeneratedSource(result);

        Assert.That(generated, Does.Contain("public void UpdateEnemyView("));
        Assert.That(generated, Does.Contain("in global::FrameInput frame"));
        Assert.That(generated, Does.Contain("private readonly global::FrameInput _input0;"));
        Assert.That(generated, Does.Contain(".Bring<global::MoveViewEvent>()"));
        Assert.That(generated, Does.Contain(".ForEach(ref job)"));
        Assert.That(generated, Does.Contain(".Batch()"));
        Assert.That(generated, Does.Contain(".Post();"));
        Assert.That(generated, Does.Contain("_self.OnUpdateEnemyView(ref c0, in c1, in _input0, in c2, ref e0);"));
        Assert.That(generated, Does.Not.Contain("IProjectionJob3x1<global::PositionComponent, global::VelocityComponent, global::FrameInput"));
    }

    [Test]
    public void Invalid_input_shapes_do_not_generate_entry_points()
    {
        var result = RunQueryGenerator(InvalidInputSource);

        Assert.That(result.GeneratedSources, Is.Empty);
    }

    [Test]
    public void Input_analyzer_allows_legal_input_without_component_diagnostics()
    {
        ImmutableArray<Diagnostic> diagnostics = RunECSAnalyzer(QueryWithInputSource);

        Assert.That(diagnostics.Select(static diagnostic => diagnostic.Id), Does.Not.Contain(DiagnosticIds.ECS010));
        Assert.That(diagnostics.Select(static diagnostic => diagnostic.Id), Does.Not.Contain(DiagnosticIds.ECS013));
    }

    [Test]
    public void Input_analyzer_reports_specific_invalid_diagnostics()
    {
        ImmutableArray<Diagnostic> diagnostics = RunECSAnalyzer(InvalidInputDiagnosticsSource);
        string[] ids = diagnostics.Select(static diagnostic => diagnostic.Id).ToArray();
        string diagnosticText = string.Join(Environment.NewLine, diagnostics.Select(static diagnostic => diagnostic.ToString()));

        Assert.That(ids, Does.Contain(DiagnosticIds.QueryInputRefNotSupported), diagnosticText);
        Assert.That(ids, Does.Contain(DiagnosticIds.QueryInputOutNotSupported), diagnosticText);
        Assert.That(ids, Does.Contain(DiagnosticIds.QueryInputByRefLikeNotSupported), diagnosticText);
        Assert.That(ids, Does.Contain(DiagnosticIds.QueryInputAfterBringNotSupported), diagnosticText);
        Assert.That(ids, Does.Contain(DiagnosticIds.QueryInputEntityNotSupported), diagnosticText);
        Assert.That(ids, Does.Contain(DiagnosticIds.QueryInputBringEventNotSupported), diagnosticText);
        Assert.That(ids, Does.Not.Contain(DiagnosticIds.ECS010), diagnosticText);
        Assert.That(ids, Does.Not.Contain(DiagnosticIds.ECS013), diagnosticText);
    }

    private const string QueryWithInputSource = """
                                                using Arch.Core;
                                                using LayerBase.Core;
                                                using LayerBase.DI;
                                                using LayerBase.ECS;

                                                public struct PositionComponent : IComponent { public float X; }
                                                public struct VelocityComponent : IComponent { public float X; }
                                                public readonly struct FrameInput { public readonly float DeltaTime; }
                                                public sealed class MovementConfig { public float Speed; }

                                                public sealed partial class MovementService : IService
                                                {
                                                    public void ConfigureServices(IServiceCollection services) { }

                                                    [Query]
                                                    private void OnMove(
                                                        ref PositionComponent position,
                                                        in VelocityComponent velocity,
                                                        [Input] in FrameInput frame,
                                                        [Input] MovementConfig config)
                                                    {
                                                    }
                                                }
                                                """;

    private const string BringQueryWithInputSource = """
                                                     using Arch.Core;
                                                     using LayerBase.Actor;
                                                     using LayerBase.Core;
                                                     using LayerBase.DI;
                                                     using LayerBase.ECS;

                                                     public struct PositionComponent : IComponent { public float X; }
                                                     public struct VelocityComponent : IComponent { public float X; }
                                                     public struct AoiComponent : IComponent { public bool IsVisible; }
                                                     public struct MoveViewEvent : IActorEvent { public float X; }
                                                     public readonly struct FrameInput { public readonly float DeltaTime; }

                                                     public sealed partial class EnemyViewService : IService
                                                     {
                                                         public void ConfigureServices(IServiceCollection services) { }

                                                         [Query]
                                                         [Bring<MoveViewEvent>]
                                                         private ProjectResult OnUpdateEnemyView(
                                                             ref PositionComponent position,
                                                             in VelocityComponent velocity,
                                                             [Input] in FrameInput frame,
                                                             in AoiComponent aoi,
                                                             ref MoveViewEvent moveEvent)
                                                         {
                                                             return ProjectResult.Success;
                                                         }
                                                     }
                                                     """;

    private const string InvalidInputSource = """
                                              using LayerBase.Core;
                                              using LayerBase.DI;
                                              using LayerBase.ECS;

                                              public struct PositionComponent : IComponent { }
                                              public readonly struct FrameInput { }

                                              public sealed partial class InvalidInputService : IService
                                              {
                                                  public void ConfigureServices(IServiceCollection services) { }

                                                  [Query]
                                                  private void OnInvalid(ref PositionComponent position, [Input] ref FrameInput frame)
                                                  {
                                                  }
                                              }
                                              """;

    private const string InvalidInputDiagnosticsSource = """
                                                         using System;
                                                         using Arch.Core;
                                                         using LayerBase.Core;
                                                         using LayerBase.DI;
                                                         using LayerBase.ECS;

                                                         public struct PositionComponent : IComponent { }
                                                         public readonly struct FrameInput { }
                                                         public struct MoveViewEvent : IActorEvent { }

                                                         public sealed partial class InvalidInputDiagnosticsService : IService
                                                         {
                                                             public void ConfigureServices(IServiceCollection services) { }

                                                             [Query]
                                                             private void OnInputRef(ref PositionComponent position, [Input] ref FrameInput frame)
                                                             {
                                                             }

                                                             [Query]
                                                             private void OnInputOut(ref PositionComponent position, [Input] out FrameInput frame)
                                                             {
                                                                 frame = default;
                                                             }

                                                             [Query]
                                                             private void OnInputSpan(ref PositionComponent position, [Input] ReadOnlySpan<int> frame)
                                                             {
                                                             }

                                                             [Query]
                                                             private void OnInputEntity([Input] Entity entity, ref PositionComponent position)
                                                             {
                                                             }

                                                             [Query]
                                                             [Bring<MoveViewEvent>]
                                                             private ProjectResult OnInputAfterBring(ref PositionComponent position, ref MoveViewEvent moveEvent, [Input] FrameInput frame)
                                                             {
                                                                 return ProjectResult.Success;
                                                             }

                                                             [Query]
                                                             [Bring<MoveViewEvent>]
                                                             private ProjectResult OnInputBringEvent(ref PositionComponent position, [Input] MoveViewEvent moveEvent)
                                                             {
                                                                 return ProjectResult.Success;
                                                             }
                                                         }
                                                         """;

    private static GeneratorTestResult RunQueryGenerator(string source)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));
        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "QueryInputGeneratorTests_" + Guid.NewGuid().ToString("N"),
            syntaxTrees: [syntaxTree],
            references: GetMetadataReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new[] { new QueryBringGenerator().AsSourceGenerator() },
            parseOptions: new CSharpParseOptions(LanguageVersion.Preview));

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outputCompilation, out _);
        GeneratorDriverRunResult runResult = driver.GetRunResult();
        ImmutableArray<Diagnostic> diagnostics = runResult.Results
            .SelectMany(static result => result.Diagnostics)
            .Where(static diagnostic => diagnostic.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning)
            .ToImmutableArray();

        ImmutableArray<string> generatedSources = runResult.Results
            .SelectMany(static result => result.GeneratedSources)
            .Select(static generated => generated.SourceText.ToString())
            .ToImmutableArray();

        return new GeneratorTestResult(diagnostics, outputCompilation, generatedSources);
    }

    private static ImmutableArray<Diagnostic> RunECSAnalyzer(string source)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));
        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "QueryInputAnalyzerTests_" + Guid.NewGuid().ToString("N"),
            syntaxTrees: [syntaxTree],
            references: GetMetadataReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return compilation
            .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new ECSAnalyzer()))
            .GetAnalyzerDiagnosticsAsync()
            .GetAwaiter()
            .GetResult()
            .Where(static diagnostic => diagnostic.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning)
            .ToImmutableArray();
    }

    private static void AssertNoGeneratorDiagnostics(GeneratorTestResult result)
    {
        Assert.That(
            result.Diagnostics,
            Is.Empty,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        Diagnostic[] compileErrors = result.OutputCompilation
            .GetDiagnostics()
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();

        Assert.That(
            compileErrors,
            Is.Empty,
            string.Join(Environment.NewLine, compileErrors.Select(static diagnostic => diagnostic.ToString())));
    }

    private static string SingleGeneratedSource(GeneratorTestResult result)
    {
        return result.GeneratedSources.Single(static source => source.Contains("_QueryBring.g.cs") ||
                                                              source.Contains("partial class"));
    }

    private static IEnumerable<MetadataReference> GetMetadataReferences()
    {
        var trustedPlatformAssemblies = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
        var paths = trustedPlatformAssemblies
            .Split(Path.PathSeparator)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        paths.Add(typeof(object).Assembly.Location);
        paths.Add(typeof(Enumerable).Assembly.Location);
        paths.Add(typeof(Entity).Assembly.Location);
        paths.Add(typeof(IService).Assembly.Location);
        paths.Add(typeof(Layer).Assembly.Location);
        paths.Add(typeof(LBTask).Assembly.Location);
        paths.Add(typeof(QueryBringGenerator).Assembly.Location);

        foreach (string path in paths)
            yield return MetadataReference.CreateFromFile(path);
    }

    private readonly record struct GeneratorTestResult(
        ImmutableArray<Diagnostic> Diagnostics,
        Compilation OutputCompilation,
        ImmutableArray<string> GeneratedSources);
}
