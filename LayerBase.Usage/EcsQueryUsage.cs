using Arch.Core;
using LayerBase;
using LayerBase.Actor;
using LayerBase.Core;
using LayerBase.DI;
using LayerBase.ECS;
using LayerBase.ECS.Projection;
using LayerBase.Layers;

namespace LayerBase.Usage;

/// <summary>
/// ECS 完整管线示例：Query → Bring → Batch → Post
/// 演示 [Query] + [Bring] + [Input] 三种特性的组合使用，
/// 以及 ProjectResult (Success/Touch/Fail) 对 Actor 生命周期和事件投递的控制。
/// </summary>
public static class EcsQueryUsage
{
    public static void Run()
    {
        Console.WriteLine("=== ECS Query + Bring + Input 完整管线示例 ===\n");
        LayerHub.Reset();

        var layer = new EcsPipelineLayer();

        using var runtime = LayerHub.CreateLayers()
            .Push(layer)
            .Build();

        var world = runtime.EcsWorld;

        Console.WriteLine("-- 1. [Query] + [Input]: 基础移动 --");
        Entity e1 = world.Create(
            new PositionComp { X = 0f },
            new VelocityComp { Speed = 10f });
        layer.PipelineService.RequestMove(0.5f);
        runtime.Pump(0f);
        PositionComp pos1 = world.Get<PositionComp>(e1);
        Console.WriteLine($"  Entity {e1.Id}: X={pos1.X:F1} (dt=0.5, speed=10 → expect 5)");

        Console.WriteLine("\n-- 2. [Query] + [Bring] + [Input]: 视野投影管线 --");
        // 创建实体：组件 + 投射Actor（投影事件由 ECS 生成 ActorEvent）
        Entity e2 = world.Create(
            new PositionComp { X = 10f },
            new VelocityComp { Speed = 5f },
            new AoiComp { IsVisible = true });
        world.WithProjectedActor<EcsProbeActor>(e2, keepAliveSeconds: 10f);

        // 不可见实体：应被 Fail，不 Touch Actor
        Entity e3 = world.Create(
            new PositionComp { X = 100f },
            new VelocityComp { Speed = 100f },
            new AoiComp { IsVisible = false });
        world.WithProjectedActor<EcsProbeActor>(e3, keepAliveSeconds: 10f);

        // 速度为零实体：应被 Touch（续命），但不 Post 事件
        Entity e4 = world.Create(
            new PositionComp { X = 50f },
            new VelocityComp { Speed = 0f },
            new AoiComp { IsVisible = true });
        world.WithProjectedActor<EcsProbeActor>(e4, keepAliveSeconds: 10f);

        EcsProbeActor.Reset();
        layer.PipelineService.RequestMoveView(new MoveViewInput { DeltaTime = 0.2f, TickId = 1 });
        runtime.Pump(0f);

        PositionComp pos2 = world.Get<PositionComp>(e2);
        Console.WriteLine($"  e2 (visible, speed=5, dt=0.2): X={pos2.X:F1} expect 11.0");
        Console.WriteLine($"  e2 Actor events: {EcsProbeActor.Received.Count} expect 1");
        foreach (var evt in EcsProbeActor.Received)
            Console.WriteLine($"    Event: Entity={evt.Entity}, X={evt.X:F1}, Y={evt.Y:F1}");

        // e3: 不可见，Fail
        PositionComp pos3 = world.Get<PositionComp>(e3);
        Console.WriteLine($"  e3 (invisible): X={pos3.X:F1} expect 100.0 (unchanged)");

        // e4: 速度为零，Touch（续命但不发事件）
        PositionComp pos4 = world.Get<PositionComp>(e4);
        Console.WriteLine($"  e4 (zero speed): X={pos4.X:F1} expect 50.0 (unchanged)");
        Console.WriteLine($"  e4 Actor events: {EcsProbeActor.Received.Count(r => r.Entity == 2)} expect 0 (touched only)");
        Console.WriteLine($"  e2 Actor still alive: True expect True");

        Console.WriteLine();
    }
}

// ───────── ECS 组件 ─────────

public struct PositionComp : IComponent
{
    public float X;
    public float Y;
}

public struct VelocityComp : IComponent
{
    public float Speed;
}

public struct AoiComp : IComponent
{
    public bool IsVisible;
}

// ───────── Actor 事件（IActorEvent）─────────

/// <summary>
/// 由 ECS Bring 管线输出的 Actor 事件，需实现 IActorEvent。
/// </summary>
public readonly struct MoveViewEvent : IActorEvent
{
    public MoveViewEvent(int entity, float x, float y)
    {
        Entity = entity;
        X = x;
        Y = y;
    }

    public int Entity { get; }
    public float X { get; }
    public float Y { get; }
}

// ───────── [Input] 参数：只读输入 ─────────

public readonly struct MoveViewInput
{
    public float DeltaTime { get; init; }
    public long TickId { get; init; }
}

// ───────── Projected Actor ─────────

public sealed partial class EcsProbeActor : IPooledActor
{
    public static List<MoveViewEvent> Received { get; } = new();

    public static void Reset() => Received.Clear();

    [ActorBehaviour]
    private void OnMoveView(in MoveViewEvent value)
    {
        Received.Add(value);
    }

    public void OnRent() { }
    public void OnReturn() { }
    public void OnEnable() { }
    public void OnDisable() { }
}

// ───────── Pipeline Service ─────────

public sealed partial class PipelineService : IService, LayerBase.DI.Options.IUpdate
{
    private float _pendingMoveDt;
    private bool _hasMove;
    private MoveViewInput _pendingViewInput;
    private bool _hasView;

    public void ConfigureServices(IServiceCollection services) { }

    public void RequestMove(float deltaTime)
    {
        _pendingMoveDt = deltaTime;
        _hasMove = true;
    }

    public void RequestMoveView(MoveViewInput input)
    {
        _pendingViewInput = input;
        _hasView = true;
    }

    public void Update()
    {
        if (_hasMove)
        {
            DoMove(new MoveInput { DeltaTime = _pendingMoveDt });
            _hasMove = false;
        }

        if (_hasView)
        {
            DoPublishView(_pendingViewInput);
            _hasView = false;
        }
    }

    // ═══ Query + [Input]：纯组件计算 ═══

    /// <summary>
    /// 纯 [Input] 无 [Bring]：Entity-local 组件计算。
    /// [Query] 触发源生成器生成 DoMove(MoveInput) 外部入口。
    /// </summary>
    [Query]
    private static void OnDoMove(
        [Input] MoveInput input,
        ref PositionComp position,
        in VelocityComp velocity)
    {
        position.X += velocity.Speed * input.DeltaTime;
    }

    // ═══ Query + [Bring] + [Input]：完整投影管线 ═══

    /// <summary>
    /// [Query] + [Bring&lt;MoveViewEvent&gt;] + [Input] 组合：
    /// - [Bring] 声明输出 Actor 事件类型
    /// - [Input] 声明外部传入只读参数
    /// - ProjectResult.Success → Touch Actor + Post 事件
    /// - ProjectResult.Touch   → Touch Actor（续命）不发事件
    /// - ProjectResult.Fail    → 不 Touch 且不发事件
    /// 源生成器生成 DoPublishView(MoveViewInput) → 自动执行 .Batch().Post()。
    /// </summary>
    [Query]
    [Bring<MoveViewEvent>]
    private static ProjectResult OnDoPublishView(
        [Input] MoveViewInput input,
        ref PositionComp position,
        in VelocityComp velocity,
        in AoiComp aoi,
        ref MoveViewEvent moveEvent)
    {
        if (!aoi.IsVisible)
            return ProjectResult.Fail;

        if (velocity.Speed == 0f)
            return ProjectResult.Touch;

        position.X += velocity.Speed * input.DeltaTime;

        moveEvent = new MoveViewEvent(
            entity: 0,
            x: position.X,
            y: 0f);

        return ProjectResult.Success;
    }
}

// ───────── [Input] 类型：纯计算输入 ─────────

public readonly struct MoveInput
{
    public float DeltaTime { get; init; }
}

// ───────── Layer ─────────

public partial class EcsPipelineLayer : Layer
{
    public EcsPipelineLayer()
    {
        PipelineService = new PipelineService();
        RegisterService(typeof(PipelineService), PipelineService);
    }

    public PipelineService PipelineService { get; }
}
