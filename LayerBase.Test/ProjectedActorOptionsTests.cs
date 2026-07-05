using Arch.Core;
using LayerBase.Actor;
using LayerBase.ECS.Projection;
using LayerBase.ECS.Projection.Flow;
using LayerBase.Layers;
using NUnit.Framework;

namespace LayerBase.Test;

#region Test Types

[ProjectedActorOptions(
    retirePolicy: ProjectedActorRetirePolicy.Disable,
    createPolicy: ProjectedActorCreatePolicy.Lazy,
    keepAliveSeconds: 1.0f,
    touchIntervalSeconds: 0.2f)]
internal sealed partial class DisablePolicyProbeActor : IPooledActor
{
    public static int RentCount { get; set; }
    public static int ReturnCount { get; set; }
    public static int EnableCount { get; set; }
    public static int DisableCount { get; set; }

    public void OnRent()
    {
        RentCount++;
    }

    public void OnReturn()
    {
        ReturnCount++;
    }

    public void OnEnable()
    {
        EnableCount++;
    }

    public void OnDisable()
    {
        DisableCount++;
    }
}

[ProjectedActorOptions(
    retirePolicy: ProjectedActorRetirePolicy.ReturnToPool,
    createPolicy: ProjectedActorCreatePolicy.Lazy,
    keepAliveSeconds: 0.5f,
    touchIntervalSeconds: 0.1f)]
internal sealed partial class ReturnToPoolPolicyProbeActor : IPooledActor
{
    public static int RentCount { get; set; }
    public static int ReturnCount { get; set; }

    public void OnRent()
    {
        RentCount++;
    }

    public void OnReturn()
    {
        ReturnCount++;
    }

    public void OnEnable()
    {
    }

    public void OnDisable()
    {
    }
}

[ProjectedActorOptions(
    retirePolicy: ProjectedActorRetirePolicy.Disable,
    createPolicy: ProjectedActorCreatePolicy.Lazy,
    keepAliveSeconds: 1.5f,
    touchIntervalSeconds: 0.3f)]
internal sealed partial class ProjectionAttributeProbeActor : IPooledActor
{
    public void OnRent()
    {
    }

    public void OnReturn()
    {
    }

    public void OnEnable()
    {
    }

    public void OnDisable()
    {
    }
}

internal sealed partial class DefaultOptionsProbeActor : IPooledActor
{
    public static int RentCount { get; set; }
    public static int ReturnCount { get; set; }

    public void OnRent()
    {
        RentCount++;
    }

    public void OnReturn()
    {
        ReturnCount++;
    }

    public void OnEnable()
    {
    }

    public void OnDisable()
    {
    }
}

internal sealed class ProjectionTestLayer : Layer
{
}

#endregion

[TestFixture]
public class ProjectedActorOptionsTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        DisablePolicyProbeActor.RentCount = 0;
        DisablePolicyProbeActor.ReturnCount = 0;
        DisablePolicyProbeActor.EnableCount = 0;
        DisablePolicyProbeActor.DisableCount = 0;
        ReturnToPoolPolicyProbeActor.RentCount = 0;
        ReturnToPoolPolicyProbeActor.ReturnCount = 0;
        DefaultOptionsProbeActor.RentCount = 0;
        DefaultOptionsProbeActor.ReturnCount = 0;
    }

    [TearDown]
    public void TearDown()
    {
        LayerHub.Reset();
    }

    [Test]
    public void RegisterGenerated_CachesOptions_FromAttribute()
    {
        // 验证：第一次 RegisterGenerated 会反射读取 ActorOptionsAttribute 并缓存
        //       第二次 RegisterGenerated 不会重复反射（通过 _optionsInitializedById 判断）

        // Arrange
        int actorTypeId = 100;
        Type actorType = typeof(DisablePolicyProbeActor);

        // Act - 第一次注册
        ProjectedActorTypeRegistry.RegisterGenerated(
            actorTypeId,
            actorType,
            static actorWorld => actorWorld.CreateProjectedActor<DisablePolicyProbeActor>());

        // Assert - 验证 options 被正确缓存
        ProjectedActorOptions options = ProjectedActorTypeRegistry.GetOptions(actorTypeId);
        Assert.That(options.RetirePolicy, Is.EqualTo(ProjectedActorRetirePolicy.Disable));
        Assert.That(options.CreatePolicy, Is.EqualTo(ProjectedActorCreatePolicy.Lazy));
        Assert.That(options.KeepAliveTicks, Is.EqualTo(ProjectedActorTime.SecondsToTicks(1.0f)));
        Assert.That(options.TouchIntervalTicks, Is.EqualTo(ProjectedActorTime.SecondsToTicks(0.2f)));
    }

    [Test]
    public void RegisterGenerated_DoesNotReparseAttribute_OnSecondCall()
    {
        // 验证：第二次调用 RegisterGenerated 时不会重复解析特性

        // Arrange
        int actorTypeId = 101;
        Type actorType = typeof(DisablePolicyProbeActor);

        // Act - 注册两次
        ProjectedActorTypeRegistry.RegisterGenerated(
            actorTypeId,
            actorType,
            static actorWorld => actorWorld.CreateProjectedActor<DisablePolicyProbeActor>());

        ProjectedActorTypeRegistry.RegisterGenerated(
            actorTypeId,
            actorType,
            static actorWorld => actorWorld.CreateProjectedActor<DisablePolicyProbeActor>());

        // Assert - options 仍然正确（没有被覆盖或出错）
        ProjectedActorOptions options = ProjectedActorTypeRegistry.GetOptions(actorTypeId);
        Assert.That(options.RetirePolicy, Is.EqualTo(ProjectedActorRetirePolicy.Disable));
    }

    [Test]
    public void RegisterGenerated_WithOptions_BypassesReflection()
    {
        // 验证：带 options 参数的 RegisterGenerated overload 不使用反射

        // Arrange
        int actorTypeId = 102;
        Type actorType = typeof(DefaultOptionsProbeActor);
        ProjectedActorOptions customOptions = new ProjectedActorOptions(
            ProjectedActorRetirePolicy.DestroyImmediately,
            ProjectedActorCreatePolicy.OnMark,
            ProjectedActorTime.SecondsToTicks(2.0f),
            ProjectedActorTime.SecondsToTicks(0.5f));

        // Act - 使用带 options 的 overload
        ProjectedActorTypeRegistry.RegisterGenerated(
            actorTypeId,
            actorType,
            static actorWorld => actorWorld.CreateProjectedActor<DefaultOptionsProbeActor>(),
            in customOptions);

        // Assert
        ProjectedActorOptions options = ProjectedActorTypeRegistry.GetOptions(actorTypeId);
        Assert.That(options.RetirePolicy, Is.EqualTo(ProjectedActorRetirePolicy.DestroyImmediately));
        Assert.That(options.CreatePolicy, Is.EqualTo(ProjectedActorCreatePolicy.OnMark));
        Assert.That(options.KeepAliveTicks, Is.EqualTo(ProjectedActorTime.SecondsToTicks(2.0f)));
        Assert.That(options.TouchIntervalTicks, Is.EqualTo(ProjectedActorTime.SecondsToTicks(0.5f)));
    }

    [Test]
    public void RegisterGenerated_CachesOptions_FromProjectedActorOptionsAttribute()
    {
        int actorTypeId = 103;

        ProjectedActorTypeRegistry.RegisterGenerated(
            actorTypeId,
            typeof(ProjectionAttributeProbeActor),
            static actorWorld => actorWorld.CreateProjectedActor<ProjectionAttributeProbeActor>());

        ProjectedActorOptions options = ProjectedActorTypeRegistry.GetOptions(actorTypeId);
        Assert.That(options.RetirePolicy, Is.EqualTo(ProjectedActorRetirePolicy.Disable));
        Assert.That(options.CreatePolicy, Is.EqualTo(ProjectedActorCreatePolicy.Lazy));
        Assert.That(options.KeepAliveTicks, Is.EqualTo(ProjectedActorTime.SecondsToTicks(1.5f)));
        Assert.That(options.TouchIntervalTicks, Is.EqualTo(ProjectedActorTime.SecondsToTicks(0.3f)));
    }

    [Test]
    public void GetOptions_ReturnsDefault_ForUnregisteredTypeId()
    {
        // 验证：未注册的 actorTypeId 返回默认 options

        // Arrange
        int unregisteredTypeId = 9999;

        // Act
        ProjectedActorOptions options = ProjectedActorTypeRegistry.GetOptions(unregisteredTypeId);

        // Assert
        Assert.That(options.RetirePolicy, Is.EqualTo(ProjectedActorRetirePolicy.ReturnToPool));
        Assert.That(options.CreatePolicy, Is.EqualTo(ProjectedActorCreatePolicy.Lazy));
    }

    [Test]
    public void DisablePolicy_ShouldCallOnDisable_NotOnReturn()
    {
        // 验证：RetirePolicy.Disable 到期后调用 OnDisable，不调用 OnReturn

        // Arrange
        LayerRuntime runtime = CreateRuntime();
        RegisterActor<DisablePolicyProbeActor>(runtime, actorTypeId: 200);

        Entity entity = runtime.EcsWorld.Create(new ProjectedActorRef());
        runtime.EcsWorld.WithProjectedActor<DisablePolicyProbeActor>(entity);

        // Touch 创建 Actor
        runtime.EcsWorld
               .Query<ProjectedActorRef>()
               .TouchProjectedActor();

        Assert.That(DisablePolicyProbeActor.RentCount, Is.EqualTo(1));

        // Act - 等待超过 KeepAlive 时间（1.0f 秒）后 Sweep
        System.Threading.Thread.Sleep(1100);
        runtime.EcsWorld.SweepProjectedActors();

        // Assert - 应该调用 OnDisable，不调用 OnReturn
        Assert.That(DisablePolicyProbeActor.DisableCount, Is.EqualTo(1));
        Assert.That(DisablePolicyProbeActor.ReturnCount, Is.EqualTo(0));

        // 验证 ActorId 仍然有效（Disable 不清理 ActorId）
        ref ProjectedActorMeta meta = ref runtime.EcsWorld.GetProjectionMeta(entity);
        Assert.That(meta.ActorId.IsValid, Is.True);
    }

    [Test]
    public void ReturnToPoolPolicy_ShouldCallOnReturn()
    {
        // 验证：RetirePolicy.ReturnToPool 到期后调用 OnReturn

        // Arrange
        LayerRuntime runtime = CreateRuntime();
        RegisterActor<ReturnToPoolPolicyProbeActor>(runtime, actorTypeId: 201);

        Entity entity = runtime.EcsWorld.Create(new ProjectedActorRef());
        runtime.EcsWorld.WithProjectedActor<ReturnToPoolPolicyProbeActor>(entity);

        QueryDescription query = new QueryDescription().WithAny<ProjectedActorRef>();
        
        // Touch 创建 Actor
        runtime.EcsWorld
               .Query<ProjectedActorRef>()
               .TouchProjectedActor();

        Assert.That(ReturnToPoolPolicyProbeActor.RentCount, Is.EqualTo(1));

        // Act - 等待超过 KeepAlive 时间（0.5f 秒）后 Sweep
        System.Threading.Thread.Sleep(600);
        runtime.EcsWorld.SweepProjectedActors();

        // Assert - 应该调用 OnReturn
        Assert.That(ReturnToPoolPolicyProbeActor.ReturnCount, Is.EqualTo(1));

        // 验证 ActorId 被清理
        ref ProjectedActorMeta meta = ref runtime.EcsWorld.GetProjectionMeta(entity);
        Assert.That(meta.ActorId.IsValid, Is.False);
    }

    [Test]
    public void DisabledActor_ShouldCallOnEnable_WhenTouchedAgain()
    {
        // 验证：Disabled 状态的 Actor 再次 Touch 时调用 OnEnable

        // Arrange
        LayerRuntime runtime = CreateRuntime();
        RegisterActor<DisablePolicyProbeActor>(runtime, actorTypeId: 202);

        Entity entity = runtime.EcsWorld.Create(new ProjectedActorRef());
        runtime.EcsWorld.WithProjectedActor<DisablePolicyProbeActor>(entity);

        // Touch 创建 Actor
        runtime.EcsWorld
               .Query()
               .TouchProjectedActor();

        // 等待 Disable
        System.Threading.Thread.Sleep(1100);
        runtime.EcsWorld.SweepProjectedActors();

        Assert.That(DisablePolicyProbeActor.DisableCount, Is.EqualTo(1));

        // Act - 再次 Touch
        runtime.EcsWorld
               .Query()
               .TouchProjectedActor();

        // Assert - 应该调用 OnEnable
        Assert.That(DisablePolicyProbeActor.EnableCount, Is.EqualTo(1));
        Assert.That(DisablePolicyProbeActor.RentCount, Is.EqualTo(1)); // 不应该再次调用 OnRent
    }

    [Test]
    public void TouchThrottling_ShouldSkipRefresh_WithinInterval()
    {
        // 验证：TouchInterval 内重复 Touch 不刷新 ExpireAtTicks

        // Arrange
        LayerRuntime runtime = CreateRuntime();
        RegisterActor<DisablePolicyProbeActor>(runtime, actorTypeId: 203);

        Entity entity = runtime.EcsWorld.Create(new ProjectedActorRef());
        runtime.EcsWorld.WithProjectedActor<DisablePolicyProbeActor>(entity);

        // Touch 创建 Actor
        runtime.EcsWorld
               .Query()
               .TouchProjectedActor();

        // 记录当前 ExpireAtTicks
        ref ProjectedActorRef actorRef = ref runtime.EcsWorld.Get<ProjectedActorRef>(entity);
        long firstExpireAt = actorRef.ExpireAtTicks;

        // Act - 立即再次 Touch（在 TouchInterval 0.2f 秒内）
        System.Threading.Thread.Sleep(50); // 等待 50ms，小于 200ms
        runtime.EcsWorld
               .Query()
               .TouchProjectedActor();

        // Assert - ExpireAtTicks 不应该被刷新（因为被节流跳过）
        ref ProjectedActorRef actorRefAfter = ref runtime.EcsWorld.Get<ProjectedActorRef>(entity);
        Assert.That(actorRefAfter.ExpireAtTicks, Is.EqualTo(firstExpireAt));
    }

    [Test]
    public void Where_False_ShouldNotTouch_Actor()
    {
        // 验证：predicate 过滤失败不会 Touch

        // Arrange
        LayerRuntime runtime = CreateRuntime();
        RegisterActor<DisablePolicyProbeActor>(runtime, actorTypeId: 204);

        Entity entity = runtime.EcsWorld.Create(new ProjectedActorRef());
        runtime.EcsWorld.WithProjectedActor<DisablePolicyProbeActor>(entity);

        // Act - 使用 Where(false) 的 Query
        runtime.EcsWorld
               .Query<ProjectedActorRef>()
               .Where(static (in Entity _, in ProjectedActorRef __) => false)
               .TouchProjectedActor();

        // Assert - Actor 不应该被创建
        ref ProjectedActorMeta meta = ref runtime.EcsWorld.GetProjectionMeta(entity);
        Assert.That(meta.ActorId.IsValid, Is.False);
        Assert.That(DisablePolicyProbeActor.RentCount, Is.EqualTo(0));
    }

    private static LayerRuntime CreateRuntime()
    {
        return LayerHub.CreateLayers()
                       .Push(new ProjectionTestLayer())
                       .Build();
    }

    private static void RegisterActor<TActor>(LayerRuntime runtime, int actorTypeId)
        where TActor : class, IPooledActor, new()
    {
        ProjectedActorTypeRegistry.RegisterGenerated(
            actorTypeId,
            typeof(TActor),
            static actorWorld => actorWorld.CreateProjectedActor<TActor>());
    }
}
