using System.Collections.Immutable;
using System.Threading;
using Arch.Core;
using LayerBase.Actor;
using LayerBase.ECS.Projection;
using LayerBase.ECS.Projection.Create;
using LayerBase.ECS.Projection.Flow;
using LayerBase.Generator;
using LayerBase.Layers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace LayerBase.Test;

public struct ProjectionPositionComponent
{
    public float X;
    public float Y;
}

public struct ProjectionVelocityComponent
{
    public float X;
    public float Y;
}

public struct ProjectionAoiComponent
{
    public bool IsVisible;
}

public struct ProjectionExtraComponent
{
    public int Value;
}

public struct ProjectionMoveViewEvent
{
    public float X;
    public float Y;

    public ProjectionMoveViewEvent(float x, float y)
    {
        X = x;
        Y = y;
    }
}

internal sealed partial class ProjectionProbeActor : IPooledActor
{
    public static List<ProjectionMoveViewEvent> Received { get; } = new();
    public static int RentCount { get; set; }
    public static int ReturnCount { get; set; }

    public long RecycleDeadlineTicks { get; set; }

    [ActorBehaviour]
    private void OnMove(in ProjectionMoveViewEvent value)
    {
        Received.Add(value);
    }

    public void OnRent()
    {
        RentCount++;
        RecycleDeadlineTicks = 0;
    }

    public void OnReturn()
    {
        ReturnCount++;
        RecycleDeadlineTicks = 0;
    }
}

internal sealed partial class ProjectionAltActor : IPooledActor
{
    public long RecycleDeadlineTicks { get; set; }

    [ActorBehaviour]
    private void OnMove(in ProjectionMoveViewEvent value)
    {
    }

    public void OnRent()
    {
        RecycleDeadlineTicks = 0;
    }

    public void OnReturn()
    {
        RecycleDeadlineTicks = 0;
    }
}

internal sealed class ProjectionLayer : Layer
{
}

[TestFixture]
public class ProjectedActorProjectionTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        ProjectionProbeActor.Received.Clear();
        ProjectionProbeActor.RentCount = 0;
        ProjectionProbeActor.ReturnCount = 0;
    }

    [TearDown]
    public void TearDown()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Projected_actor_type_registry_is_runtime_local()
    {
        LayerRuntime runtimeA = CreateRuntime();
        LayerRuntime runtimeB = CreateRuntime();

        runtimeA.ProjectedActorTypes.RegisterGenerated(
            7,
            typeof(ProjectionProbeActor),
            static actorWorld => actorWorld.CreateProjectedActor<ProjectionProbeActor>());
        runtimeB.ProjectedActorTypes.RegisterGenerated(
            7,
            typeof(ProjectionAltActor),
            static actorWorld => actorWorld.CreateProjectedActor<ProjectionAltActor>());

        Assert.That(runtimeA.ProjectedActorTypes.GetActorType(7), Is.EqualTo(typeof(ProjectionProbeActor)));
        Assert.That(runtimeB.ProjectedActorTypes.GetActorType(7), Is.EqualTo(typeof(ProjectionAltActor)));
    }

    [Test]
    public void Projection_post_creates_actor_updates_components_and_batches_mail()
    {
        LayerRuntime runtime = CreateRuntime();
        RegisterProjectionProbe(runtime, actorTypeId: 1);

        Entity entity = runtime.EcsWorld.Create(
            new ProjectionPositionComponent { X = 1f, Y = 2f },
            new ProjectionVelocityComponent { X = 3f, Y = 4f });
        runtime.EcsWorld.WithProjectedActor(entity, actorTypeId: 1, keepAliveSeconds: 0.5f);

        runtime.EcsWorld
            .Query<ProjectionPositionComponent, ProjectionVelocityComponent>()
            .Where(static (in Entity _, in ProjectionPositionComponent __, in ProjectionVelocityComponent velocity) =>
                velocity.X != 0f || velocity.Y != 0f)
            .Bring<ProjectionMoveViewEvent>()
            .ForEach(static (
                in Entity _,
                ref ProjectionPositionComponent position,
                ref ProjectionVelocityComponent velocity,
                ref ProjectionMoveViewEvent output) =>
            {
                position.X += velocity.X;
                position.Y += velocity.Y;
                output = new ProjectionMoveViewEvent(position.X, position.Y);
            })
            .Batch()
            .Post();

        runtime.Pump(0.016f);

        ProjectionPositionComponent position = runtime.EcsWorld.Get<ProjectionPositionComponent>(entity);
        ActorId actorId = runtime.EcsWorld.GetProjectionMeta(entity).ActorId;

        Assert.That(position.X, Is.EqualTo(4f));
        Assert.That(position.Y, Is.EqualTo(6f));
        Assert.That(actorId.IsValid, Is.True);
        Assert.That(runtime.Actors.IsAlive(actorId), Is.True);
        Assert.That(ProjectionProbeActor.RentCount, Is.EqualTo(1));
        Assert.That(ProjectionProbeActor.Received.Count, Is.EqualTo(1));
        Assert.That(ProjectionProbeActor.Received[0].X, Is.EqualTo(4f));
        Assert.That(ProjectionProbeActor.Received[0].Y, Is.EqualTo(6f));
    }

    [Test]
    public void Touch_projected_actor_creates_actor_without_post_and_sweeps_by_deadline()
    {
        LayerRuntime runtime = CreateRuntime();
        RegisterProjectionProbe(runtime, actorTypeId: 2);

        Entity entity = runtime.EcsWorld.Create(
            new ProjectionPositionComponent { X = 10f, Y = 20f },
            new ProjectionAoiComponent { IsVisible = true });
        runtime.EcsWorld.WithProjectedActor(entity, actorTypeId: 2, keepAliveSeconds: 0.01f);

        runtime.EcsWorld
            .Query<ProjectionPositionComponent, ProjectionAoiComponent>()
            .Where(static (in Entity _, in ProjectionPositionComponent __, in ProjectionAoiComponent aoi) => aoi.IsVisible)
            .TouchProjectedActor();

        ActorId actorId = runtime.EcsWorld.GetProjectionMeta(entity).ActorId;
        Assert.That(actorId.IsValid, Is.True);
        Assert.That(ProjectionProbeActor.Received, Is.Empty);

        Thread.Sleep(30);
        runtime.Pump(0.016f);

        Assert.That(runtime.EcsWorld.GetProjectionMeta(entity).ActorId.IsValid, Is.False);
        Assert.That(ProjectionProbeActor.ReturnCount, Is.EqualTo(1));
    }

    [Test]
    public void Projection_meta_survives_entity_row_move_and_archetype_move()
    {
        LayerRuntime runtime = CreateRuntime();
        RegisterProjectionProbe(runtime, actorTypeId: 3);

        Entity first = runtime.EcsWorld.Create(new ProjectionPositionComponent { X = 1f, Y = 1f });
        Entity second = runtime.EcsWorld.Create(new ProjectionPositionComponent { X = 2f, Y = 2f });
        runtime.EcsWorld.WithProjectedActor(second, actorTypeId: 3, keepAliveSeconds: 0.5f);

        runtime.EcsWorld
            .Query<ProjectionPositionComponent>()
            .Bring<ProjectionMoveViewEvent>()
            .ForEach(static (
                in Entity _,
                ref ProjectionPositionComponent position,
                ref ProjectionMoveViewEvent output) =>
            {
                output = new ProjectionMoveViewEvent(position.X, position.Y);
            })
            .Batch()
            .Post();

        ActorId beforeDestroy = runtime.EcsWorld.GetProjectionMeta(second).ActorId;
        runtime.EcsWorld.Destroy(first);
        Assert.That(runtime.EcsWorld.GetProjectionMeta(second).ActorId, Is.EqualTo(beforeDestroy));

        runtime.EcsWorld.Add(second, new ProjectionExtraComponent { Value = 9 });
        Assert.That(runtime.EcsWorld.GetProjectionMeta(second).ActorId, Is.EqualTo(beforeDestroy));
    }

    [Test]
    public void Projected_actor_type_generator_emits_registry_entries_for_layerbase_assembly()
    {
        const string source = """
using LayerBase.Actor;
using LayerBase.Test;

namespace LayerBase;

internal sealed partial class GeneratedProjectionActor : IPooledActor
{
    public long RecycleDeadlineTicks { get; set; }

    [ActorBehaviour]
    private void OnMove(in ProjectionMoveViewEvent value)
    {
    }

    public void OnRent()
    {
    }

    public void OnReturn()
    {
    }
}
""";

        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));
        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "LayerBase",
            syntaxTrees: new[] { syntaxTree },
            references: ActorGeneratorTests_GetMetadataReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new ISourceGenerator[]
            {
                new ActorBehaviourGenerator().AsSourceGenerator(),
                new ProjectedActorTypeGenerator().AsSourceGenerator()
            },
            parseOptions: new CSharpParseOptions(LanguageVersion.Preview));
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

        ImmutableArray<GeneratorRunResult> results = driver.GetRunResult().Results;
        string generated = results
            .SelectMany(static result => result.GeneratedSources)
            .Select(static sourceResult => sourceResult.SourceText.ToString())
            .First(static text => text.Contains("GeneratedProjectedActorTypes"));

        Assert.That(generated, Does.Contain("RegisterGenerated"));
        Assert.That(generated, Does.Contain("CreateProjectedActor<global::LayerBase.GeneratedProjectionActor>()"));
        Assert.That(generated, Does.Contain("actorType: typeof(global::LayerBase.GeneratedProjectionActor)"));
    }

    private static LayerRuntime CreateRuntime()
    {
        return LayerHub.CreateLayers()
            .Push(new ProjectionLayer())
            .Build();
    }

    private static void RegisterProjectionProbe(LayerRuntime runtime, int actorTypeId)
    {
        runtime.ProjectedActorTypes.RegisterGenerated(
            actorTypeId,
            typeof(ProjectionProbeActor),
            static actorWorld => actorWorld.CreateProjectedActor<ProjectionProbeActor>());
    }

    private static IEnumerable<MetadataReference> ActorGeneratorTests_GetMetadataReferences()
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

    [Test]
    public void Query0_TouchProjectedActor_Should_Visit_Entity()
    {
        // 逻辑说明：
        // 验证空组件 Query 能命中刚创建的 Entity。

        LayerRuntime runtime = CreateRuntime();
        RegisterProjectionProbe(runtime, actorTypeId: 10);

        Entity entity = runtime.EcsWorld.Create();
        runtime.EcsWorld.WithProjectedActor(entity, actorTypeId: 10, keepAliveSeconds: 0.5f);

        runtime.EcsWorld
            .Query()
            .TouchProjectedActor();

        ref ProjectedActorMeta meta =
            ref runtime.EcsWorld.GetProjectionMeta(entity);

        Assert.That(meta.ActorId.IsValid, Is.True);
    }

    [Test]
    public void Query0_Where_False_Should_Not_Create_ProjectedActor()
    {
        // 逻辑说明：
        // 验证 Query0 的 Where 可以阻止 Actor 创建。

        LayerRuntime runtime = CreateRuntime();
        RegisterProjectionProbe(runtime, actorTypeId: 10);

        Entity entity = runtime.EcsWorld.Create();
        runtime.EcsWorld.WithProjectedActor(entity, actorTypeId: 10, keepAliveSeconds: 0.5f);

        runtime.EcsWorld
            .Query()
            .Where(static (in Entity entity) => false)
            .TouchProjectedActor();

        ref ProjectedActorMeta meta =
            ref runtime.EcsWorld.GetProjectionMeta(entity);

        Assert.That(meta.ActorId.IsValid, Is.False);
    }

    [Test]
    public void CreateEntity0_WithProjectedActor_Should_Mark_Meta()
    {
        LayerRuntime runtime = CreateRuntime();
        RegisterProjectionProbe(runtime, actorTypeId: 10);

        Entity entity = runtime.EcsWorld.Create();
        runtime.EcsWorld.WithProjectedActor(entity, actorTypeId: 10, keepAliveSeconds: 0.5f);

        ref ProjectedActorMeta meta =
            ref runtime.EcsWorld.GetProjectionMeta(entity);

        Assert.That(meta.ActorTypeId, Is.GreaterThanOrEqualTo(0));
        Assert.That(meta.ActorId.IsValid, Is.False);
    }

    [Test]
    public void CreateEntity2_WithProjectedActor_Should_Mark_Meta()
    {
        LayerRuntime runtime = CreateRuntime();
        RegisterProjectionProbe(runtime, actorTypeId: 10);

        Entity entity = runtime.EcsWorld.Create(
            new ProjectionPositionComponent(),
            new ProjectionVelocityComponent());
        runtime.EcsWorld.WithProjectedActor(entity, actorTypeId: 10, keepAliveSeconds: 0.5f);

        ref ProjectedActorMeta meta =
            ref runtime.EcsWorld.GetProjectionMeta(entity);

        Assert.That(meta.ActorTypeId, Is.GreaterThanOrEqualTo(0));
        Assert.That(meta.ActorId.IsValid, Is.False);
    }
}
