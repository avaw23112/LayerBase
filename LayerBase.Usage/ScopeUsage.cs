using LayerBase;
using LayerBase.Async;
using LayerBase.DI;
using LayerBase.Layers;
using LayerBase.Scope;

namespace LayerBase.Usage;

/// <summary>
/// Scope 分域 + async-await 示例：
/// - ScopeRef<TScope>.Post() — 单向事件（fire-and-forget）
/// - ScopeRef<TScope>.Call() — 返回 LBTask<TResult>，调用方应使用 async-await
/// - 跨 Scope Call 的 continuation 自动回到调用方 Scope 上下文
/// </summary>
public static class ScopeUsage
{
    public static async Task Run()
    {
        Console.WriteLine("=== Scope 分域 + LBTask 异步示例 ===\n");
        LayerHub.Reset();

        var dataLayer = new ScopeDataLayer();
        var mainLayer = new ScopeMainLayer();

        using var runtime = LayerHub.CreateLayers()
            .Push(dataLayer)
            .Push(mainLayer)
            .Build();

        var host = runtime.ScopeHost!;
        var dataRef = host.GetScopeRef<DataScope>();

        // 1. Post：单向事件投递
        Console.WriteLine("[Main] Post 事件到 DataScope...");
        dataRef.Post(new DataUpdateEvent { TickCount = 1 });
        dataRef.Post(new DataUpdateEvent { TickCount = 2 });
        runtime.Pump(0.1f);
        Console.WriteLine($"[DataScope] 收到 Post 事件: {dataLayer.ScopeDataService.ReceivedPostCount} / expect 2");

        // 2. Call：LBTask 异步调用（同步 pump + GetResult）
        Console.WriteLine("[Main] Call 查询 DataScope（同步等待）...");
        var task1 = dataRef.Call(new DataQuery { Key = "status" });
        runtime.Pump(0f);
        var r1 = task1.GetAwaiter().GetResult();
        Console.WriteLine($"[Main] Call 返回: Count={r1.QueryCount}, InDataScope={r1.InDataScope}");

        // 3. Call：真正的 async-await（Wait 阻塞演示）
        Console.WriteLine("[Main] Call 查询 DataScope（async-await）...");
        var task2 = dataRef.Call(new DataQuery { Key = "items" });
        runtime.Pump(0f);
        var r2 = await task2;
        Console.WriteLine($"[Main] await 返回: Count={r2.QueryCount}, InDataScope={r2.InDataScope}");

        Console.WriteLine($"[DataScope] 总查询数: {dataLayer.ScopeDataService.TotalQueries} / expect 2");
        Console.WriteLine();
    }
}

// ───────── Scope ─────────

// MainScope 由框架内建（ScopeDescriptors.Main，scopeId=0），无需用户定义。
// 未标记 [Scope<TScope>] 的 Service 自动归属 MainScope。
// MainScope 固定为 Inline + EngineDriven，直接由 LayerRuntime.Pump() 驱动。

[ScopeOptions]
public sealed partial class DataScope
{
}

// ───────── Scope Event ─────────

[ScopeEvent<DataScope>]
public struct DataUpdateEvent
{
    public int TickCount;
}

// ───────── Scope Call ─────────

[ScopeCall<DataScope, DataQueryResult>]
public readonly struct DataQuery
{
    public string Key { get; init; }
}

public readonly struct DataQueryResult
{
    public DataQueryResult(int queryCount, bool inDataScope)
    {
        QueryCount = queryCount;
        InDataScope = inDataScope;
    }

    public int QueryCount { get; }
    public bool InDataScope { get; }
}

// ───────── Service ─────────

[Scope<DataScope>]
public sealed partial class ScopeDataService : IService
{
    private int _queryCount;
    private int _postCount;

    public int TotalQueries => _queryCount;
    public int ReceivedPostCount => _postCount;

    public void ConfigureServices(IServiceCollection services) { }

    [ScopeEvent]
    private void OnDataUpdate(DataUpdateEvent message)
    {
        _postCount++;
    }

    [ScopeCall]
    private DataQueryResult OnDataQuery(DataQuery call)
    {
        _queryCount++;
        return new DataQueryResult(
            queryCount: _queryCount,
            inDataScope: ScopeExecution.Current.ScopeId == 1);
    }
}

// ───────── Layers ─────────

public partial class ScopeDataLayer : Layer
{
    public ScopeDataLayer()
    {
        ScopeDataService = new ScopeDataService();
        RegisterService(typeof(ScopeDataService), ScopeDataService);
    }

    public ScopeDataService ScopeDataService { get; }
}

public partial class ScopeMainLayer : Layer
{
    public ScopeMainLayer()
    {
        RegisterService(typeof(ScopeMainSvc), new ScopeMainSvc());
    }
}

public sealed partial class ScopeMainSvc : IService
{
    public void ConfigureServices(IServiceCollection services) { }
}
