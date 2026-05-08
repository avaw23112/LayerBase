using System.Collections.Immutable;
using System.Reflection;
using LayerBase.Actor;
using LayerBase.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace LayerBase.Test;

[TestFixture]
public class ActorGeneratorTests
{
    [Test]
    public void Partial_actor_with_actor_behaviours_generates_IGeneratedActorMeta()
    {
        GeneratorRunResult result = RunGenerator("""
            using LayerBase.Actor;

            namespace Sample;

            public struct DamageEvent { }
            public struct DeadEvent { }

            public sealed partial class EnemyActor : IActor
            {
                [ActorBehaviour]
                private void OnDamage(in DamageEvent e)
                {
                }

                [ActorBehaviour]
                private void OnDead(in DeadEvent e)
                {
                }
            }
            """);

        Assert.That(GetGeneratorDiagnostics(result), Is.Empty);

        string generated = result.GeneratedSources.Single().SourceText.ToString();
        Assert.That(generated, Does.Contain("partial class EnemyActor : global::LayerBase.Actor.IGeneratedActorMeta"));
        Assert.That(generated, Does.Contain("private global::LayerBase.Actor.ActorContext __actorContext;"));
        Assert.That(generated, Does.Contain("builder.AddBehaviour<global::Sample.EnemyActor, global::Sample.DamageEvent>("));
        Assert.That(generated, Does.Contain("builder.AddBehaviour<global::Sample.EnemyActor, global::Sample.DeadEvent>("));
    }

    [Test]
    public void Generated_actor_code_compiles_without_errors()
    {
        (GeneratorRunResult result, Compilation outputCompilation) = RunGeneratorWithCompilation("""
            using LayerBase.Actor;

            namespace Sample;

            public struct DamageEvent { }

            public sealed partial class EnemyActor : IActor
            {
                [ActorBehaviour]
                private void OnDamage(in DamageEvent e)
                {
                }
            }
            """);

        Assert.That(GetGeneratorDiagnostics(result), Is.Empty);

        ImmutableArray<Diagnostic> errors = outputCompilation.GetDiagnostics()
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToImmutableArray();

        Assert.That(errors, Is.Empty, string.Join(Environment.NewLine, errors.Select(static error => error.ToString())));
    }

    [TestCase("LBACTOR001", """
        using LayerBase.Actor;

        public struct DamageEvent { }

        public sealed class EnemyActor : IActor
        {
            [ActorBehaviour]
            private void OnDamage(in DamageEvent e)
            {
            }
        }
        """)]
    [TestCase("LBACTOR002", """
        using LayerBase.Actor;

        public struct DamageEvent { }

        public sealed partial class EnemyActor
        {
            [ActorBehaviour]
            private void OnDamage(in DamageEvent e)
            {
            }
        }
        """)]
    [TestCase("LBACTOR003", """
        using LayerBase.Actor;

        public struct DamageEvent { }

        public sealed partial class EnemyActor : IActor
        {
            [ActorBehaviour]
            private static void OnDamage(in DamageEvent e)
            {
            }
        }
        """)]
    [TestCase("LBACTOR004", """
        using LayerBase.Actor;

        public struct DamageEvent { }

        public sealed partial class EnemyActor : IActor
        {
            [ActorBehaviour]
            private int OnDamage(in DamageEvent e)
            {
                return 0;
            }
        }
        """)]
    [TestCase("LBACTOR005", """
        using LayerBase.Actor;

        public struct DamageEvent { }

        public sealed partial class EnemyActor : IActor
        {
            [ActorBehaviour]
            private void OnDamage(in DamageEvent e, int extra)
            {
            }
        }
        """)]
    [TestCase("LBACTOR006", """
        using LayerBase.Actor;

        public struct DamageEvent { }

        public sealed partial class EnemyActor : IActor
        {
            [ActorBehaviour]
            private void OnDamage(DamageEvent e)
            {
            }
        }
        """)]
    [TestCase("LBACTOR007", """
        using LayerBase.Actor;

        public sealed class DamageEvent { }

        public sealed partial class EnemyActor : IActor
        {
            [ActorBehaviour]
            private void OnDamage(in DamageEvent e)
            {
            }
        }
        """)]
    [TestCase("LBACTOR008", """
        using LayerBase.Actor;

        public struct DamageEvent { }

        public sealed partial class EnemyActor : IActor
        {
            [ActorBehaviour]
            private void OnDamage(in DamageEvent e)
            {
            }

            [ActorBehaviour]
            private void OnDamageAgain(in DamageEvent e)
            {
            }
        }
        """)]
    [TestCase("LBACTOR009", """
        using LayerBase.Actor;
        using LayerBase.Core.Event;

        public struct DamageEvent { }

        public sealed partial class EnemyActor : IActor, IGeneratedActorMeta
        {
            [ActorBehaviour]
            private void OnDamage(in DamageEvent e)
            {
            }

            public void __BuildActorMeta(ActorTypeMetaBuilder builder)
            {
            }

            public ActorId GetId()
            {
                return default;
            }

            public void ActorInit(ActorContext context)
            {
            }

            public PostResult Post<TEvent>(in TEvent value) where TEvent : struct
            {
                return PostResult.Success;
            }

            public PostResult TryPost<TEvent>(in TEvent value) where TEvent : struct
            {
                return PostResult.Success;
            }
        }
        """)]
    public void Invalid_actor_behaviour_declarations_report_expected_diagnostic(string diagnosticId, string source)
    {
        GeneratorRunResult result = RunGenerator(source);
        ImmutableArray<Diagnostic> diagnostics = GetGeneratorDiagnostics(result);

        Assert.That(diagnostics.Select(static diagnostic => diagnostic.Id), Does.Contain(diagnosticId),
            string.Join(Environment.NewLine, diagnostics.Select(static diagnostic => diagnostic.ToString())));
    }

    private static GeneratorRunResult RunGenerator(string source)
    {
        return RunGeneratorWithCompilation(source).Result;
    }

    private static (GeneratorRunResult Result, Compilation OutputCompilation) RunGeneratorWithCompilation(string source)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));
        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "ActorGeneratorTests_" + Guid.NewGuid().ToString("N"),
            syntaxTrees: new[] { syntaxTree },
            references: GetMetadataReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ActorBehaviourGenerator().AsSourceGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outputCompilation, out _);

        ImmutableArray<GeneratorRunResult> results = driver.GetRunResult().Results;
        if (results.Length == 0)
        {
            throw new AssertionException("No generator results were produced.");
        }

        GeneratorRunResult result = results[0];

        return (result, outputCompilation);
    }

    private static ImmutableArray<Diagnostic> GetGeneratorDiagnostics(GeneratorRunResult result)
    {
        return result.Diagnostics
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToImmutableArray();
    }

    private static IEnumerable<MetadataReference> GetMetadataReferences()
    {
        string trustedPlatformAssemblies = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
        HashSet<string> paths = trustedPlatformAssemblies
            .Split(Path.PathSeparator)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        paths.Add(typeof(object).Assembly.Location);
        paths.Add(typeof(Enumerable).Assembly.Location);
        paths.Add(typeof(IActor).Assembly.Location);
        paths.Add(typeof(ActorBehaviourGenerator).Assembly.Location);

        foreach (string path in paths)
        {
            yield return MetadataReference.CreateFromFile(path);
        }
    }
}
