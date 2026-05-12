using Arch.Core;
using Arch.Core.Extensions;
using LayerBase.Actor;
using LayerBase.Core;
using LayerBase.ECS;

namespace LayerBase.Tests.ECS;

[TestFixture]
public sealed class BundleBlueprintTests
{
    [Test]
    public void Bundle_Config_ShouldAddExpectedComponents()
    {
        var builder =
            new EntityBlueprintBuilder();

        BlueprintUnitCache<TestMoveBundle>.Config(
            ref builder);

        EntityBlueprint blueprint = builder.Build();

        Assert.Multiple(
             () =>
            {
                Assert.That(
                    blueprint.ComponentTypes,
                    Does.Contain(Component<TestPositionComponent>.ComponentType));

                Assert.That(
                    blueprint.ComponentTypes,
                    Does.Contain(Component<TestVelocityComponent>.ComponentType));

                Assert.That(
                    blueprint.ComponentTypes,
                    Does.Contain(Component<TestMoveStateComponent>.ComponentType));
            });
    }

    [Test]
    public void Blueprint_Config_ShouldExpandNestedBundles()
    {
        EntityBlueprint blueprint =
            EntityBlueprintCache<TestEnemyBlueprint>.GetOrBuild();

        Assert.Multiple(
             () =>
            {
                Assert.That(
                    blueprint.ComponentTypes,
                    Does.Contain(Component<TestPositionComponent>.ComponentType));

                Assert.That(
                    blueprint.ComponentTypes,
                    Does.Contain(Component<TestVelocityComponent>.ComponentType));

                Assert.That(
                      blueprint.ComponentTypes,
                    Does.Contain(Component<TestMoveStateComponent>.ComponentType));

                Assert.That(
                      blueprint.ComponentTypes,
                    Does.Contain(Component<TestHealthComponent>.ComponentType));

                Assert.That(
                      blueprint.ComponentTypes,
                    Does.Contain(Component<TestAoiComponent>.ComponentType));
            });
    }

    [Test]
    public void BlueprintCache_ShouldBuildOnlyOnce()
    {
        TestCachedBlueprint.ConfigCallCount = 0;

        EntityBlueprint first =
            EntityBlueprintCache<TestCachedBlueprint>.GetOrBuild();

        EntityBlueprint second =
            EntityBlueprintCache<TestCachedBlueprint>.GetOrBuild();

        Assert.Multiple(
            () =>
            {
                Assert.That(
                    second.ComponentTypes,
                    Is.EqualTo(first.ComponentTypes));

                Assert.That(
                    TestCachedBlueprint.ConfigCallCount,
                    Is.EqualTo(1));
            });
    }

    [Test]
    public void CreateEntity_WithBlueprint_ShouldCreateExpectedComponents()
    {
        using World world =
            World.Create();

        Entity entity = world.CreateEntity().WithBlueprint<TestEnemyBlueprint>().Build();
        Assert.Multiple(
            () =>
            {
                Assert.That(
                    entity.Has<TestPositionComponent>(),
                    Is.True);

                Assert.That(
                    entity.Has<TestVelocityComponent>(),
                    Is.True);

                Assert.That(
                    entity.Has<TestMoveStateComponent>(),
                    Is.True);

                Assert.That(
                    entity.Has<TestHealthComponent>(),
                    Is.True);

                Assert.That(
                    entity.Has<TestAoiComponent>(),
                    Is.True);
            });
    }

    [Test]
    public void Entity_SetDeclaredComponent_ShouldSucceed()
    {
        using World world =
            World.Create();

        Entity entity = world.CreateEntity().WithBlueprint<TestEnemyBlueprint>().Build();

        entity.Set(
            new TestPositionComponent(
                x: 10f,
                y: 20f));

        ref TestPositionComponent position =
            ref entity.Get<TestPositionComponent>();
        Assert.That(
            position.X,
            Is.EqualTo(10f));

        Assert.That(
            position.Y,
            Is.EqualTo(20f));
    }

    [Test]
    public void Blueprint_WithProjectedActor_ShouldRecordProjectionMeta()
    {
        EntityBlueprint blueprint =
            EntityBlueprintCache<TestEnemyBlueprint>.GetOrBuild();
        Assert.That(
            blueprint.ActorProjection,
            Is.EqualTo(typeof(TestEnemyActor)));
    }

    [LayerBundle]
    public sealed class TestMoveBundle : IBundle
    {
        public void Config(
            ref EntityBlueprintBuilder builder)
        {
            // builder 参数作用：
            // 当前实体蓝图构建器。
            // 这里声明移动能力需要的 ECS 组件。

            builder.WithComponent<TestPositionComponent>();
            builder.WithComponent<TestVelocityComponent>();
            builder.WithComponent<TestMoveStateComponent>();
        }
    }

    [LayerBundle]
    public sealed class TestCombatBundle : IBundle
    {
        public void Config(
            ref EntityBlueprintBuilder builder)
        {
            // builder 参数作用：
            // 当前实体蓝图构建器。
            // 这里声明战斗能力需要的 ECS 组件。

            builder.WithComponent<TestHealthComponent>();
        }
    }

    [LayerBundle]
    public sealed class TestAoiBundle : IBundle
    {
        public void Config(
            ref EntityBlueprintBuilder builder)
        {
            // builder 参数作用：
            // 当前实体蓝图构建器。
            // 这里声明 AOI / 可见性能力需要的 ECS 组件。

            builder.WithComponent<TestAoiComponent>();
        }
    }

    [LayerBlueprint]
    public sealed class TestEnemyBlueprint : IEntityBlueprint
    {
        public void Config(
            ref EntityBlueprintBuilder builder)
        {
            // builder 参数作用：
            // 当前实体蓝图构建器。
            // 这里声明敌人实体的完整结构。

            builder.WithBundle<TestMoveBundle>();
            builder.WithBundle<TestCombatBundle>();
            builder.WithBundle<TestAoiBundle>();
            builder.WithProjectedActor<TestEnemyActor>();
        }
    }

    [LayerBlueprint]
    public sealed class TestCachedBlueprint : IEntityBlueprint
    {
        public static int ConfigCallCount;

        public void Config(
            ref EntityBlueprintBuilder builder)
        {
            // builder 参数作用：
            // 当前实体蓝图构建器。
            // 该测试通过计数验证 EntityBlueprintCache<TBlueprint> 只构建一次。

            ConfigCallCount++;

            builder.WithComponent<TestPositionComponent>();
        }
    }

    public struct TestPositionComponent : IComponent
    {
        public float X;
        public float Y;

        public TestPositionComponent(
            float x,
            float y)
        {
            // x 参数作用：
            // 测试位置组件的 X 坐标。

            // y 参数作用：
            // 测试位置组件的 Y 坐标。

            X = x;
            Y = y;
        }
    }

    public struct TestVelocityComponent : IComponent
    {
        public float X;
        public float Y;
    }

    public struct TestMoveStateComponent : IComponent
    {
        public int State;
    }

    public struct TestHealthComponent : IComponent
    {
        public int Value;
    }

    public struct TestAoiComponent : IComponent
    {
        public bool IsVisible;
    }

    public sealed class TestEnemyActor : IPooledActor
    {
        public long RecycleDeadlineTicks { get; set; }

        public void OnRent()
        {
        }
        public void OnReturn()
        {
        }
    }
}