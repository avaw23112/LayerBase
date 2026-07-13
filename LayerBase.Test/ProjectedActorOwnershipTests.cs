using Arch.Core;
using LayerBase.Actor;
using LayerBase.Actor.RuntimeCommands;
using LayerBase.Core;
using LayerBase.DI;
using LayerBase.ECS.Projection;
using LayerBase.ECS.Projection.Flow;
using LayerBase.Scope;
using ServiceUpdate = LayerBase.DI.Options.IUpdate;

namespace LayerBase.Test;

[TestFixture]
public sealed class ProjectedActorOwnershipTests
{
    private const int ActorTypeId = 9501;

    [SetUp]
    public void SetUp()
    {
        ProjectedOwnerThreadActor.Reset();
        ProjectedActorTypeRegistry.RegisterGenerated(
            ActorTypeId,
            typeof(ProjectedOwnerThreadActor),
            static actorWorld => actorWorld.CreateProjectedActor<ProjectedOwnerThreadActor>(),
            new ProjectedActorOptions(
                ProjectedActorRetirePolicy.ReturnToPool,
                ProjectedActorCreatePolicy.Lazy,
                ProjectedActorTime.SecondsToTicks(0.01f),
                ProjectedActorTime.SecondsToTicks(0.001f)));
    }

    [Test]
    public void Shared_projected_actor_release_must_run_on_owner_thread_not_scope_worker()
    {
        int ownerThreadId = Thread.CurrentThread.ManagedThreadId;
        using var layerRuntime = new LayerRuntime(1230);
        using var service = new WorkerProjectedActorService();
        using var scope = new ScopeRuntime(
            new ScopeDescriptor(
                scopeId: 1230,
                name: "ProjectedActorWorkerScope",
                threading: ScopeThreadingMode.Worker,
                clock: ScopeClockMode.FixedRate,
                tickRateHz: 240,
                stopPolicy: ScopeStopPolicy.Drain),
            new IService[] { service },
            sharedActorWorld: layerRuntime.Actors,
            owningRuntime: layerRuntime);

        scope.Start();

        Assert.That(service.Created.Wait(TimeSpan.FromSeconds(2)), Is.True);
        Assert.That(DrainUntilReturned(layerRuntime, TimeSpan.FromSeconds(3)), Is.True);

        scope.Stop();

        Assert.That(service.WorkerThreadId, Is.Not.EqualTo(ownerThreadId));
        Assert.That(ProjectedOwnerThreadActor.ReturnThreadId, Is.EqualTo(ownerThreadId));
        Assert.That(ProjectedOwnerThreadActor.ReturnThreadId, Is.Not.EqualTo(service.WorkerThreadId));
    }

    [Test]
    public void Actor_lifecycle_inbox_must_overflow_instead_of_rejecting_when_fast_lane_is_full()
    {
        var inbox = new ActorLifecycleInbox(1);
        const int commandCount = 20;

        for (int i = 0; i < commandCount; i++)
        {
            var command = new ActorCommandEnvelope(
                i % 2 == 0 ? ActorCommandKind.Disable : ActorCommandKind.Release,
                default,
                i,
                0);
            Assert.That(inbox.TryEnqueue(command), Is.Not.EqualTo(ControlEnqueueResult.Closed));
        }

        Assert.That(inbox.Count, Is.EqualTo(commandCount));
        Assert.That(inbox.OverflowCount, Is.GreaterThan(0));

        for (int i = 0; i < commandCount; i++)
        {
            Assert.That(inbox.TryDequeue(out ActorCommandEnvelope command), Is.True);
            Assert.That(command.RouteId, Is.EqualTo(i));
        }

        Assert.That(inbox.TryDequeue(out _), Is.False);
    }

    [Test]
    public void Projected_actor_release_must_retain_binding_when_lifecycle_queue_is_closed()
    {
        using var actorWorld = new ActorWorld();
        using var world = World.Create();
        var sink = new ClosedProjectedActorLifecycleSink();
        world.BindScopeActors(actorWorld, sink);
        var actorId = new ActorId(1, 2, 3);
        Entity entity = world.Create(new ProjectedActorRef(actorId, ActorTypeId, 1, ProjectedActorReleasePolicy.ReturnToPool));
        ref ProjectedActorMeta meta = ref world.GetProjectionMeta(entity);
        meta.MarkProjected(ActorTypeId, 1, ProjectedActorReleasePolicy.ReturnToPool);
        meta.BindActor(actorId);
        meta.RetirePolicy = ProjectedActorRetirePolicy.ReturnToPool;
        world.AddActiveProjectedActor(entity, ref meta);
        ref ProjectedActorRef actorRef = ref world.Get<ProjectedActorRef>(entity);
        actorRef.ExpireAtTicks = 0;

        world.SweepProjectedActors(1);

        Assert.That(sink.ReleaseAttempts, Is.EqualTo(1));
        Assert.That(meta.ActorId, Is.EqualTo(actorId));
        Assert.That(meta.ActiveListIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(actorRef.ActorId, Is.EqualTo(actorId));
    }

    private static bool DrainUntilReturned(LayerRuntime runtime, TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            runtime.DrainActorCommands();
            if (ProjectedOwnerThreadActor.Returned.Wait(TimeSpan.FromMilliseconds(10)))
            {
                return true;
            }
        }

        return false;
    }

    private sealed class WorkerProjectedActorService : IService, ServiceUpdate, IDisposable
    {
        private bool _created;

        public ManualResetEventSlim Created { get; } = new(false);
        public int WorkerThreadId { get; private set; } = -1;

        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Update()
        {
            if (_created)
            {
                return;
            }

            _created = true;
            WorkerThreadId = Thread.CurrentThread.ManagedThreadId;
            ScopeRuntime runtime = ScopeExecution.Current.Runtime!;
            Entity entity = runtime.EcsWorld.Create(
                new ProjectedOwnerComponent(),
                new ProjectedActorRef());
            runtime.EcsWorld.WithProjectedActor(
                entity,
                ActorTypeId,
                ProjectedActorTime.SecondsToTicks(0.01f),
                ProjectedActorReleasePolicy.ReturnToPool);
            runtime.EcsWorld.Query<ProjectedOwnerComponent>().TouchProjectedActor();
            Created.Set();
        }

        public void Dispose()
        {
            Created.Dispose();
        }
    }

    private sealed class ClosedProjectedActorLifecycleSink : IProjectedActorLifecycleSink
    {
        public int ReleaseAttempts;

        public ControlEnqueueResult TryDisableProjectedActor(ActorId actorId)
        {
            return ControlEnqueueResult.Closed;
        }

        public ControlEnqueueResult TryReleaseProjectedActor(
            ActorId actorId,
            ProjectedActorReleasePolicy releasePolicy)
        {
            ReleaseAttempts++;
            return ControlEnqueueResult.Closed;
        }
    }
}

internal struct ProjectedOwnerComponent : IComponent
{
}

internal readonly struct ProjectedOwnerEvent : IActorEvent
{
}

internal sealed partial class ProjectedOwnerThreadActor : IPooledActor
{
    public static readonly ManualResetEventSlim Returned = new(false);

    public static int ReturnThreadId = -1;

    public static void Reset()
    {
        ReturnThreadId = -1;
        Returned.Reset();
    }

    [ActorBehaviour]
    private void OnProjectedOwnerEvent(in ProjectedOwnerEvent _)
    {
    }

    public void OnRent()
    {
    }

    public void OnReturn()
    {
        ReturnThreadId = Thread.CurrentThread.ManagedThreadId;
        Returned.Set();
    }

    public void OnEnable()
    {
    }

    public void OnDisable()
    {
    }
}
