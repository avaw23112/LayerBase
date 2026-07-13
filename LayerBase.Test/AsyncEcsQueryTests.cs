using Arch.Core;
using LayerBase.ECS;
using LayerBase.ECS.Projection;
using LayerBase.ECS.Projection.Flow;
using LayerBase.ECS.Runtime;
using LayerBase.Layers;
using NUnit.Framework;

namespace LayerBase.Test;

[TestFixture]
public sealed class AsyncEcsQueryTests
{
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

        Task stop = Task.Run(runtime.EcsScheduler.Stop);
        Assert.That(stop.Wait(TimeSpan.FromMilliseconds(100)), Is.False);

        gate.Set();

        Assert.That(stop.Wait(TimeSpan.FromSeconds(2)), Is.True);
        Assert.That(executed, Is.EqualTo(2));
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

    private static LayerRuntime CreateAsyncRuntime()
    {
        return LayerHub.CreateLayers()
                       .Push(new AsyncEcsTestLayer())
                       .SetEcsExecutionMode(EcsExecutionMode.Async)
                       .Build();
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
