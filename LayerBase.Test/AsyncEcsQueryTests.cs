using Arch.Core;
using System.Diagnostics;
using LayerBase.Actor;
using LayerBase.ECS;
using LayerBase.ECS.Projection;
using LayerBase.ECS.Projection.Flow;
using LayerBase.ECS.Runtime;
using LayerBase.Layers;
using LayerBase.Scope;
using NUnit.Framework;

namespace LayerBase.Test;

[TestFixture]
public sealed class AsyncEcsQueryTests
{
    private const int DeferredActorTypeId = 9701;

    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        JobProbeActor.Received.Clear();
        JobProbeActor.RentThreadIds.Clear();
        JobProbeActor.ReturnThreadIds.Clear();
        JobProbeActor.RentCount = 0;
        JobProbeActor.ReturnCount = 0;
    }

    [TearDown]
    public void TearDown()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Build_DefaultEcsMode_IsSync()
    {
        LayerRuntime runtime = LayerHub.CreateLayers()
                                       .Push(new AsyncEcsTestLayer())
                                       .Build();

        Assert.That(runtime.EcsScheduler.Mode, Is.EqualTo(EcsExecutionMode.Sync));
    }

    [Test]
    public void Build_WithAsyncEcsMode_StartsAsyncScheduler()
    {
        LayerRuntime runtime = CreateAsyncRuntime();

        Assert.That(runtime.EcsScheduler.Mode, Is.EqualTo(EcsExecutionMode.Async));
    }

    [Test]
    public void AsyncDirectQuery_executes_immediately_at_call_site()
    {
        LayerRuntime runtime = CreateAsyncRuntime();

        Entity entity = runtime.EcsWorld.Create(
            new JobPositionComponent { X = 1f, Y = 2f },
            new JobVelocityComponent { X = 3f, Y = 4f });

        var job = new MoveJob();

        runtime.EcsWorld
               .Query<JobPositionComponent, JobVelocityComponent>()
               .ForEach(ref job);

        JobPositionComponent after = runtime.EcsWorld.Get<JobPositionComponent>(entity);
        Assert.That(after.X, Is.EqualTo(4f));
        Assert.That(after.Y, Is.EqualTo(6f));
    }

    [Test]
    public void AsyncBring_PostsActorEventOnlyAfterMainPump()
    {
        LayerRuntime runtime = CreateAsyncRuntime();

        Entity entity = runtime.EcsWorld.Create(
            new JobPositionComponent { X = 1f, Y = 2f },
            new JobVelocityComponent { X = 3f, Y = 4f },
            new JobAoiComponent { IsVisible = true });
        runtime.EcsWorld.WithProjectedActor<JobProbeActor>(entity, keepAliveSeconds: 0.5f);

        using var gate = new ManualResetEventSlim(false);
        var job = new BlockingBringJob(gate);

        runtime.EcsWorld
               .Query<JobPositionComponent, JobVelocityComponent, JobAoiComponent>()
               .Bring<JobMoveViewEvent>()
               .ForEach(ref job)
               .Batch()
               .Post();

        JobPositionComponent before = runtime.EcsWorld.Get<JobPositionComponent>(entity);
        Assert.That(before.X, Is.EqualTo(1f));
        Assert.That(before.Y, Is.EqualTo(2f));
        Assert.That(JobProbeActor.Received, Is.Empty);

        gate.Set();
        runtime.WaitEcsIdleForTest(TimeSpan.FromSeconds(2));

        Assert.That(JobProbeActor.Received, Is.Empty);

        runtime.Pump(0.016f);

        JobPositionComponent after = runtime.EcsWorld.Get<JobPositionComponent>(entity);
        Assert.That(after.X, Is.EqualTo(4f));
        Assert.That(after.Y, Is.EqualTo(6f));
        Assert.That(JobProbeActor.Received, Has.Count.EqualTo(1));
        Assert.That(JobProbeActor.Received[0].X, Is.EqualTo(4f));
        Assert.That(JobProbeActor.Received[0].Y, Is.EqualTo(6f));
    }

    [Test]
    public void AsyncScheduler_Stop_must_drain_accepted_batches_before_joining_worker()
    {
        LayerRuntime runtime = CreateAsyncRuntime();
        using var started = new ManualResetEventSlim(false);
        using var gate = new ManualResetEventSlim(false);
        var executed = 0;

        runtime.EcsWorkScheduler.Schedule(new BlockingCountWorkItem(started, gate, () => Interlocked.Increment(ref executed)));
        runtime.FlushEcsSubmissionsForTest();
        runtime.EcsWorkScheduler.Schedule(new CountWorkItem(() => Interlocked.Increment(ref executed)));
        runtime.FlushEcsSubmissionsForTest();

        Assert.That(started.Wait(TimeSpan.FromSeconds(2)), Is.True);

        var stopWatch = Stopwatch.StartNew();
        Task release = Task.Run(() =>
        {
            Thread.Sleep(100);
            gate.Set();
        });

        runtime.EcsScheduler.Stop();
        Assert.That(release.Wait(TimeSpan.FromSeconds(2)), Is.True);
        Assert.That(stopWatch.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(80));
        Assert.That(executed, Is.EqualTo(2));
    }

    [Test]
    public void Non_owner_schedule_must_fail_before_batch_mutation()
    {
        LayerRuntime runtime = CreateAsyncRuntime();
        using World world = World.Create();
        var scheduler = new AsyncEcsScheduler(runtime, world, EcsRuntimeOptions.Default);
        var item = new CancelCountingWorkItem();

        Exception? error = null;
        Task schedule = Task.Run(() =>
        {
            error = Assert.Throws<InvalidOperationException>(() => scheduler.Schedule(item));
        });

        Assert.That(schedule.Wait(TimeSpan.FromSeconds(2)), Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(item.CancelCount, Is.EqualTo(1));
        Assert.That(scheduler.CurrentSubmissionCountForTest, Is.EqualTo(0));

        scheduler.Dispose();
    }

    [Test]
    public void Non_owner_control_api_must_fail_before_stop_mutation()
    {
        LayerRuntime runtime = CreateAsyncRuntime();
        using World world = World.Create();
        var scheduler = new AsyncEcsScheduler(runtime, world, EcsRuntimeOptions.Default);
        var item = new CancelCountingWorkItem();
        scheduler.Schedule(item);

        Exception? error = null;
        Task stop = Task.Run(() =>
        {
            error = Assert.Throws<InvalidOperationException>(scheduler.Stop);
        });

        Assert.That(stop.Wait(TimeSpan.FromSeconds(2)), Is.True);
        Assert.That(error, Is.Not.Null);
        Assert.That(scheduler.CurrentSubmissionCountForTest, Is.EqualTo(1));
        Assert.That(item.CancelCount, Is.EqualTo(0));

        scheduler.Dispose();
    }

    [Test]
    public void AsyncScheduler_Stop_must_return_queued_pooled_work_when_worker_was_not_started()
    {
        LayerRuntime runtime = CreateAsyncRuntime();
        using World world = World.Create();
        var scheduler = new AsyncEcsScheduler(runtime, world, EcsRuntimeOptions.Default);
        var item = new ReturnCountingWorkItem();

        scheduler.Schedule(item);
        scheduler.FlushSubmissionsForTest();
        scheduler.Stop();

        Assert.That(item.ReturnCount, Is.EqualTo(1));
    }

    [Test]
    public void AsyncScheduler_Stop_must_cancel_queued_non_pooled_work_when_worker_was_not_started()
    {
        LayerRuntime runtime = CreateAsyncRuntime();
        using World world = World.Create();
        var scheduler = new AsyncEcsScheduler(runtime, world, EcsRuntimeOptions.Default);
        var item = new CancelCountingWorkItem();

        scheduler.Schedule(item);
        scheduler.FlushSubmissionsForTest();
        scheduler.Stop();

        Assert.That(item.CancelCount, Is.EqualTo(1));
        Assert.That(item.CancelReason, Is.TypeOf<OperationCanceledException>());
    }

    [Test]
    public void AsyncScheduler_Stop_must_reject_late_pooled_work_after_terminal_stop()
    {
        LayerRuntime runtime = CreateAsyncRuntime();
        using World world = World.Create();
        var scheduler = new AsyncEcsScheduler(runtime, world, EcsRuntimeOptions.Default);
        var item = new ReturnCountingWorkItem();

        scheduler.Start();
        scheduler.Stop();
        scheduler.Schedule(item);
        scheduler.FlushSubmissionsForTest();

        Assert.That(item.ReturnCount, Is.EqualTo(1));
    }

    [Test]
    public void ScopeRuntime_async_ecs_pump_must_submit_projected_actor_sweep_as_work()
    {
        string root = FindRepositoryRoot();
        string source = File.ReadAllText(Path.Combine(root, "LayerBase", "Scope", "ScopeRuntime.cs"));
        int pumpStart = source.IndexOf("private void PumpInternal", StringComparison.Ordinal);
        int pumpEnd = source.IndexOf("private void ScheduleProjectedActorSweep", StringComparison.Ordinal);
        Assert.That(pumpStart, Is.GreaterThanOrEqualTo(0));
        Assert.That(pumpEnd, Is.GreaterThan(pumpStart));
        string pumpInternal = source.Substring(pumpStart, pumpEnd - pumpStart);

        Assert.That(source, Does.Contain("ScheduleProjectedActorSweep"));
        Assert.That(source, Does.Contain("PooledEcsWorkItem<object?>.Rent"));
        Assert.That(pumpInternal, Does.Not.Contain("EcsWorld.SweepProjectedActors();"));
    }

    [Test]
    public void AsyncProjectedActor_sweep_must_return_actor_on_runtime_owner_thread()
    {
        int ownerThreadId = Environment.CurrentManagedThreadId;
        LayerRuntime runtime = CreateAsyncRuntime();

        Entity entity = runtime.EcsWorld.Create(
            new JobPositionComponent { X = 1f, Y = 2f },
            new JobVelocityComponent { X = 3f, Y = 4f },
            new JobAoiComponent { IsVisible = true });
        runtime.EcsWorld.WithProjectedActor<JobProbeActor>(entity, keepAliveSeconds: 0f);

        using var gate = new ManualResetEventSlim(true);
        var job = new BlockingBringJob(gate);
        runtime.EcsWorld
               .Query<JobPositionComponent, JobVelocityComponent, JobAoiComponent>()
               .Bring<JobMoveViewEvent>()
               .ForEach(ref job)
               .Batch()
               .Post();

        runtime.WaitEcsIdleForTest(TimeSpan.FromSeconds(2));
        runtime.Pump(0.016f);
        runtime.WaitEcsIdleForTest(TimeSpan.FromSeconds(2));
        runtime.Pump(0.016f);

        int[] returnThreadIds;
        lock (JobProbeActor.ReturnThreadIds)
        {
            returnThreadIds = JobProbeActor.ReturnThreadIds.ToArray();
        }

        Assert.That(returnThreadIds, Is.Not.Empty);
        Assert.That(returnThreadIds, Has.All.EqualTo(ownerThreadId));
    }

    [Test]
    public void AsyncProjectedActor_binding_must_not_use_false_actor_world_defer_stub()
    {
        string root = FindRepositoryRoot();
        string source = File.ReadAllText(Path.Combine(root, "LayerBase", "ECS", "Projection", "ProjectedActorBinding.cs"));
        int start = source.IndexOf("private static bool ShouldDeferActorWorldAccess(ActorWorld actorWorld)", StringComparison.Ordinal);

        Assert.That(start, Is.GreaterThanOrEqualTo(0));
        string method = source.Substring(start, source.IndexOf("\n    }\n}", start, StringComparison.Ordinal) - start);
        Assert.That(method, Does.Not.Contain("return false;"));
    }

    [Test]
    public void AsyncProjectedActor_ensure_must_rent_actor_on_runtime_owner_thread()
    {
        int ownerThreadId = Environment.CurrentManagedThreadId;
        LayerRuntime runtime = CreateAsyncRuntime();

        Entity entity = runtime.EcsWorld.Create(
            new JobPositionComponent { X = 1f, Y = 2f },
            new JobVelocityComponent { X = 3f, Y = 4f },
            new JobAoiComponent { IsVisible = true });
        runtime.EcsWorld.WithProjectedActor<JobProbeActor>(entity, keepAliveSeconds: 0.5f);

        Assert.That(runtime.EcsWorld.GetProjectionMeta(entity).ActorId.IsValid, Is.True);

        using var gate = new ManualResetEventSlim(true);
        var job = new BlockingBringJob(gate);
        runtime.EcsWorld
               .Query<JobPositionComponent, JobVelocityComponent, JobAoiComponent>()
               .Bring<JobMoveViewEvent>()
               .ForEach(ref job)
               .Batch()
               .Post();

        runtime.WaitEcsIdleForTest(TimeSpan.FromSeconds(2));
        runtime.Pump(0.016f);
        runtime.WaitEcsIdleForTest(TimeSpan.FromSeconds(2));
        runtime.Pump(0.016f);

        int[] rentThreadIds;
        lock (JobProbeActor.RentThreadIds)
        {
            rentThreadIds = JobProbeActor.RentThreadIds.ToArray();
        }

        Assert.That(rentThreadIds, Is.Not.Empty);
        Assert.That(rentThreadIds, Has.All.EqualTo(ownerThreadId));
    }

    [Test]
    public void Async_projection_ensure_must_create_actor_only_on_runtime_owner()
    {
        int ownerThreadId = Environment.CurrentManagedThreadId;
        LayerRuntime runtime = CreateAsyncRuntime();
        RegisterDeferredJobProbeActor();

        Entity entity = CreateDeferredProjectedEntity(runtime);

        using var gate = new ManualResetEventSlim(true);
        var job = new BlockingBringJob(gate);
        runtime.EcsWorld
               .Query<JobPositionComponent, JobVelocityComponent, JobAoiComponent>()
               .Bring<JobMoveViewEvent>()
               .ForEach(ref job)
               .Batch()
               .Post();

        runtime.WaitEcsIdleForTest(TimeSpan.FromSeconds(2));
        Assert.That(JobProbeActor.RentThreadIds, Is.Empty);

        runtime.Pump(0.016f);

        int[] rentThreadIds;
        lock (JobProbeActor.RentThreadIds)
        {
            rentThreadIds = JobProbeActor.RentThreadIds.ToArray();
        }

        Assert.That(runtime.EcsWorld.ProjectionIntentCountForTest, Is.EqualTo(1));
        Assert.That(runtime.EcsWorld.Get<ProjectedActorRef>(entity).ActorId.IsValid, Is.True);
        Assert.That(rentThreadIds, Is.Not.Empty);
        Assert.That(rentThreadIds, Has.All.EqualTo(ownerThreadId));
    }

    [Test]
    public void Async_projection_worker_must_never_enter_actor_world()
    {
        LayerRuntime runtime = CreateAsyncRuntime();
        RegisterDeferredJobProbeActor();
        _ = CreateDeferredProjectedEntity(runtime);

        using var gate = new ManualResetEventSlim(true);
        var job = new BlockingBringJob(gate);
        runtime.EcsWorld
               .Query<JobPositionComponent, JobVelocityComponent, JobAoiComponent>()
               .Bring<JobMoveViewEvent>()
               .ForEach(ref job)
               .Batch()
               .Post();

        runtime.WaitEcsIdleForTest(TimeSpan.FromSeconds(2));

        Assert.That(JobProbeActor.RentThreadIds, Is.Empty);
        Assert.That(runtime.EcsWorld.ProjectionIntentCountForTest, Is.EqualTo(1));
    }

    [Test]
    public void Projection_result_for_destroyed_entity_must_release_created_actor()
    {
        LayerRuntime runtime = CreateAsyncRuntime();
        RegisterDeferredJobProbeActor();
        Entity entity = CreateDeferredProjectedEntity(runtime);

        using var gate = new ManualResetEventSlim(true);
        var job = new BlockingBringJob(gate);
        runtime.EcsWorld
               .Query<JobPositionComponent, JobVelocityComponent, JobAoiComponent>()
               .Bring<JobMoveViewEvent>()
               .ForEach(ref job)
               .Batch()
               .Post();

        runtime.WaitEcsIdleForTest(TimeSpan.FromSeconds(2));
        runtime.EcsWorld.Destroy(entity);
        runtime.Pump(0.016f);

        Assert.That(JobProbeActor.RentCount, Is.EqualTo(0));
        Assert.That(runtime.EcsWorld.ActiveProjectedActorCountForTest, Is.EqualTo(0));
    }

    [Test]
    public void Pure_ecs_entity_must_not_generate_projection_intents()
    {
        LayerRuntime runtime = CreateAsyncRuntime();
        Entity entity = runtime.EcsWorld.Create(
            new JobPositionComponent { X = 1f, Y = 2f },
            new JobVelocityComponent { X = 3f, Y = 4f });

        var job = new MoveJob();
        runtime.EcsWorld
               .Query<JobPositionComponent, JobVelocityComponent>()
               .ForEach(ref job);

        JobPositionComponent after = runtime.EcsWorld.Get<JobPositionComponent>(entity);
        Assert.That(after.X, Is.EqualTo(4f));
        Assert.That(after.Y, Is.EqualTo(6f));
        Assert.That(runtime.EcsWorld.ProjectionIntentCountForTest, Is.EqualTo(0));
    }

    [Test]
    public void Repeated_ensure_must_generate_one_intent()
    {
        LayerRuntime runtime = CreateAsyncRuntime();
        RegisterDeferredJobProbeActor();
        Entity entity = CreateDeferredProjectedEntity(runtime);
        var queue = new EcsResultQueue(ringCapacity: 8, overflowCapacity: 0, batchCapacity: 1);

        Task.Run(() =>
        {
            EcsThreadGuard.EnterExecution(runtime.Id, queue);
            try
            {
                ref ProjectedActorRef actorRef = ref runtime.EcsWorld.Get<ProjectedActorRef>(entity);
                _ = ProjectedActorBinding.RefreshProjectedActorInterest(
                    runtime.EcsWorld,
                    runtime.Actors,
                    entity,
                    ref actorRef,
                    Stopwatch.GetTimestamp());
                _ = ProjectedActorBinding.RefreshProjectedActorInterest(
                    runtime.EcsWorld,
                    runtime.Actors,
                    entity,
                    ref actorRef,
                    Stopwatch.GetTimestamp());
            }
            finally
            {
                EcsThreadGuard.ExitExecution(runtime.Id);
            }
        }).Wait(TimeSpan.FromSeconds(2));

        ref ProjectedActorMeta meta = ref runtime.EcsWorld.GetProjectionMeta(entity);
        Assert.That(runtime.EcsWorld.ProjectionIntentCountForTest, Is.EqualTo(1));
        Assert.That(meta.EnsurePending, Is.True);

        queue.DrainToMainThread(runtime, maxCount: 0);

        Assert.That(meta.EnsurePending, Is.False);
        Assert.That(runtime.EcsWorld.Get<ProjectedActorRef>(entity).ActorId.IsValid, Is.True);
    }

    [Test]
    public void Rejected_projection_result_must_not_leave_pending_flag()
    {
        LayerRuntime runtime = CreateAsyncRuntime();
        RegisterDeferredJobProbeActor();
        Entity entity = CreateDeferredProjectedEntity(runtime);
        var queue = new EcsResultQueue(ringCapacity: 1, overflowCapacity: 0, batchCapacity: 1);
        Assert.That(queue.Enqueue(new NoopResultItem()), Is.True);

        Task.Run(() =>
        {
            EcsThreadGuard.EnterExecution(runtime.Id, queue);
            try
            {
                ref ProjectedActorRef actorRef = ref runtime.EcsWorld.Get<ProjectedActorRef>(entity);
                _ = ProjectedActorBinding.RefreshProjectedActorInterest(
                    runtime.EcsWorld,
                    runtime.Actors,
                    entity,
                    ref actorRef,
                    Stopwatch.GetTimestamp());
            }
            finally
            {
                EcsThreadGuard.ExitExecution(runtime.Id);
            }
        }).Wait(TimeSpan.FromSeconds(2));

        ref ProjectedActorMeta meta = ref runtime.EcsWorld.GetProjectionMeta(entity);
        Assert.That(meta.EnsurePending, Is.False);
        Assert.That(runtime.EcsWorld.ProjectionIntentCountForTest, Is.EqualTo(0));
    }

    [Test]
    public void Deferred_enable_rejection_must_not_report_success()
    {
        LayerRuntime runtime = CreateAsyncRuntime();
        RegisterDeferredJobProbeActor();
        long keepAliveTicks = ProjectedActorTime.SecondsToTicks(0.5f);
        ProjectedActorHandle handle = runtime.Actors.CreateProjectedActor<JobProbeActor>();
        Entity entity = runtime.EcsWorld.Create(new ProjectedActorRef(
            handle.ActorId,
            DeferredActorTypeId,
            keepAliveTicks,
            ProjectedActorReleasePolicy.ReturnToPool));
        ref ProjectedActorMeta meta = ref runtime.EcsWorld.GetProjectionMeta(entity);
        meta = ProjectedActorMeta.None;
        meta.MarkProjected(DeferredActorTypeId, keepAliveTicks, ProjectedActorReleasePolicy.ReturnToPool);
        meta.BindActor(handle.ActorId);
        runtime.ActorLifecycleInbox.Close();
        long beforeDeadline = runtime.EcsWorld.Get<ProjectedActorRef>(entity).ExpireAtTicks;
        bool alive = true;

        Task.Run(() =>
        {
            ref ProjectedActorRef actorRef = ref runtime.EcsWorld.Get<ProjectedActorRef>(entity);
            alive = ProjectedActorBinding.RefreshProjectedActorInterest(
                runtime.EcsWorld,
                runtime.Actors,
                entity,
                ref actorRef,
                Stopwatch.GetTimestamp());
        }).Wait(TimeSpan.FromSeconds(2));

        Assert.That(alive, Is.False);
        Assert.That(meta.EnablePending, Is.False);
        Assert.That(runtime.EcsWorld.Get<ProjectedActorRef>(entity).ExpireAtTicks, Is.EqualTo(beforeDeadline));
    }

    [Test]
    public void Actor_batch_must_enqueue_one_command()
    {
        LayerRuntime runtime = CreateAsyncRuntime();
        var gateway = new ScopeActorGateway(runtime, runtime.Actors, scopeId: 1);
        ActorId[] actorIds = { ActorId.Invalid, ActorId.Invalid };

        gateway.PostToMany(actorIds, new JobMoveViewEvent(1f, 2f));

        Assert.That(runtime.ActorEventInbox.Count, Is.EqualTo(1));
        Assert.That(runtime.ActorPayloads.Count, Is.EqualTo(1));
    }

    private static LayerRuntime CreateAsyncRuntime()
    {
        return LayerHub.CreateLayers()
                       .Push(new AsyncEcsTestLayer())
                       .SetEcsExecutionMode(EcsExecutionMode.Async)
                       .Build();
    }

    private static void RegisterDeferredJobProbeActor()
    {
        ProjectedActorTypeRegistry.RegisterGenerated(
            DeferredActorTypeId,
            typeof(JobProbeActor),
            static actorWorld => actorWorld.CreateProjectedActor<JobProbeActor>(),
            new ProjectedActorOptions(
                ProjectedActorRetirePolicy.ReturnToPool,
                ProjectedActorCreatePolicy.Lazy,
                ProjectedActorTime.SecondsToTicks(0.5f),
                ProjectedActorTime.SecondsToTicks(0.1f)));
    }

    private static Entity CreateDeferredProjectedEntity(LayerRuntime runtime)
    {
        long keepAliveTicks = ProjectedActorTime.SecondsToTicks(0.5f);
        Entity entity = runtime.EcsWorld.Create(
            new JobPositionComponent { X = 1f, Y = 2f },
            new JobVelocityComponent { X = 3f, Y = 4f },
            new JobAoiComponent { IsVisible = true },
            new ProjectedActorRef(
                DeferredActorTypeId,
                keepAliveTicks,
                ProjectedActorReleasePolicy.ReturnToPool));

        ref ProjectedActorMeta meta = ref runtime.EcsWorld.GetProjectionMeta(entity);
        var options = new ProjectedActorOptions(
            ProjectedActorRetirePolicy.ReturnToPool,
            ProjectedActorCreatePolicy.Lazy,
            keepAliveTicks,
            ProjectedActorTime.SecondsToTicks(0.1f));
        meta = ProjectedActorMeta.None;
        meta.MarkProjected(
            DeferredActorTypeId,
            keepAliveTicks,
            ProjectedActorReleasePolicy.ReturnToPool,
            in options);
        return entity;
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(TestContext.CurrentContext.TestDirectory);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "LayerBase.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }

    private readonly struct BlockingMoveJob :
        IQueryJob<JobPositionComponent, JobVelocityComponent>
    {
        private readonly ManualResetEventSlim _gate;

        public BlockingMoveJob(ManualResetEventSlim gate)
        {
            _gate = gate;
        }

        public void Execute(
            Entity entity,
            ref JobPositionComponent position,
            ref JobVelocityComponent velocity)
        {
            _gate.Wait();
            position.X += velocity.X;
            position.Y += velocity.Y;
        }
    }

    private readonly struct MoveJob :
        IQueryJob<JobPositionComponent, JobVelocityComponent>
    {
        public void Execute(
            Entity entity,
            ref JobPositionComponent position,
            ref JobVelocityComponent velocity)
        {
            position.X += velocity.X;
            position.Y += velocity.Y;
        }
    }

    private readonly struct BlockingBringJob :
        IProjectionJob3x1<JobPositionComponent, JobVelocityComponent, JobAoiComponent, JobMoveViewEvent>
    {
        private readonly ManualResetEventSlim _gate;

        public BlockingBringJob(ManualResetEventSlim gate)
        {
            _gate = gate;
        }

        public ProjectResult Execute(
            Entity entity,
            ref JobPositionComponent position,
            ref JobVelocityComponent velocity,
            ref JobAoiComponent aoi,
            ref JobMoveViewEvent moveEvent)
        {
            _gate.Wait();

            if (!aoi.IsVisible)
            {
                return ProjectResult.Fail;
            }

            position.X += velocity.X;
            position.Y += velocity.Y;
            moveEvent = new JobMoveViewEvent(position.X, position.Y);
            return ProjectResult.Success;
        }
    }

    private sealed class AsyncEcsTestLayer : Layer
    {
    }

    private sealed class NoopResultItem : IEcsResultItem
    {
        public string DebugName => nameof(NoopResultItem);

        public void Apply(LayerRuntime runtime)
        {
        }
    }

    private sealed class BlockingCountWorkItem : IEcsWorkItem
    {
        private readonly ManualResetEventSlim _started;
        private readonly ManualResetEventSlim _gate;
        private readonly Action _execute;

        public BlockingCountWorkItem(ManualResetEventSlim started, ManualResetEventSlim gate, Action execute)
        {
            _started = started;
            _gate = gate;
            _execute = execute;
        }

        public string DebugName => nameof(BlockingCountWorkItem);

        public void Execute(World world, EcsResultQueue results)
        {
            _started.Set();
            _gate.Wait();
            _execute();
        }

        public void Cancel(Exception reason)
        {
        }
    }

    private sealed class CountWorkItem : IEcsWorkItem
    {
        private readonly Action _execute;

        public CountWorkItem(Action execute)
        {
            _execute = execute;
        }

        public string DebugName => nameof(CountWorkItem);

        public void Execute(World world, EcsResultQueue results)
        {
            _execute();
        }

        public void Cancel(Exception reason)
        {
        }
    }

    private sealed class CancelCountingWorkItem : IEcsWorkItem
    {
        public int CancelCount { get; private set; }

        public Exception? CancelReason { get; private set; }

        public string DebugName => nameof(CancelCountingWorkItem);

        public void Execute(World world, EcsResultQueue results)
        {
            Assert.Fail("Queued work should not execute when the worker was never started.");
        }

        public void Cancel(Exception reason)
        {
            CancelCount++;
            CancelReason = reason;
        }
    }

    private sealed class ReturnCountingWorkItem : IEcsWorkItem, IPooledEcsWorkItem
    {
        public int ReturnCount { get; private set; }

        public string DebugName => nameof(ReturnCountingWorkItem);

        public void Execute(World world, EcsResultQueue results)
        {
            Assert.Fail("Queued work should not execute when the worker was never started.");
        }

        public void Cancel(Exception reason)
        {
            ReturnToPool();
        }

        public void ReturnToPool()
        {
            ReturnCount++;
        }
    }
}
