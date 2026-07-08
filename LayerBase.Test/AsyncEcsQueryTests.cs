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
    public void AsyncPlainQuery_DoesNotExecuteAtSubmitPointAndExecutesOnWorker()
    {
        LayerRuntime runtime = CreateAsyncRuntime();

        Entity entity = runtime.EcsWorld.Create(
            new JobPositionComponent { X = 1f, Y = 2f },
            new JobVelocityComponent { X = 3f, Y = 4f });

        using var gate = new ManualResetEventSlim(false);
        var job = new BlockingMoveJob(gate);

        runtime.EcsWorld
               .Query<JobPositionComponent, JobVelocityComponent>()
               .ForEach(ref job);

        JobPositionComponent before = runtime.EcsWorld.Get<JobPositionComponent>(entity);
        Assert.That(before.X, Is.EqualTo(1f));
        Assert.That(before.Y, Is.EqualTo(2f));

        gate.Set();
        runtime.WaitEcsIdleForTest(TimeSpan.FromSeconds(2));

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

    private static LayerRuntime CreateAsyncRuntime()
    {
        return LayerHub.CreateLayers()
                       .Push(new AsyncEcsTestLayer())
                       .SetEcsExecutionMode(EcsExecutionMode.Async)
                       .Build();
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
}
