using System;

namespace LayerBase.Core.Event;

/// <summary>
/// handler 所属的故障类别。
/// 异常处理时通过它选择对应的 FaultSlot 数组。
/// </summary>
internal enum FaultKind
{
    /// <summary>
    /// SubscribeFlow 同步 handler。
    /// </summary>
    Sync,

    /// <summary>
    /// SubscribeFlow 异步 handler。
    /// </summary>
    Async,

    /// <summary>
    /// Subscribe 安全通知 handler。
    /// </summary>
    Subscribe
}

/// <summary>
/// 单个 handler 的故障诊断槽。
/// 它只在异常路径中使用。
/// </summary>
internal readonly struct FaultSlot
{
    /// <summary>
    /// 当前 handler 所属 Layer 的运行时下标。
    /// 异常上报时可通过它定位具体 Layer。
    /// </summary>
    public readonly int LayerIndex;

    /// <summary>
    /// 当前 handler 的故障状态对象。
    /// 异常发生后通过它执行 TryDisable。
    /// </summary>
    public readonly HandlerCircuit Circuit;

    /// <summary>
    /// 当前 handler 名称的符号 ID。
    /// 异常上报时通过 EventDiagnosticSymbols.Resolve 还原成字符串。
    /// </summary>
    public readonly int HandlerNameId;

    /// <summary>
    /// 创建一个故障诊断槽。
    /// </summary>
    /// <param name="layerIndex">
    /// 注册该 handler 的 Layer 下标。
    /// </param>
    /// <param name="circuit">
    /// 该 handler 对应的故障状态对象。
    /// </param>
    /// <param name="handlerNameId">
    /// 该 handler 名称对应的符号 ID。
    /// </param>
    public FaultSlot(int layerIndex, HandlerCircuit circuit, int handlerNameId)
    {
        LayerIndex = layerIndex;
        Circuit = circuit;
        HandlerNameId = handlerNameId;
    }
}

/// <summary>
/// 当前 EventBucket 的异常诊断快照。
/// 它和派发数组在同一次 Rebuild 中生成。
/// </summary>
/// <typeparam name="TEvent">
/// 当前事件类型。
/// </typeparam>
internal sealed class FaultTable<TEvent> where TEvent : struct
{
    /// <summary>
    /// SubscribeFlow 同步 handler 的故障槽数组。
    /// 下标与 _syncHandlers 对齐。
    /// </summary>
    public readonly FaultSlot[] SyncFaults;

    /// <summary>
    /// SubscribeFlow 异步 handler 的故障槽数组。
    /// 下标与 _asyncHandlers 对齐。
    /// </summary>
    public readonly FaultSlot[] AsyncFaults;

    /// <summary>
    /// Subscribe 安全通知 handler 的故障槽数组。
    /// 下标与 _subscribeHandlers 对齐。
    /// </summary>
    public readonly FaultSlot[] SubscribeFaults;

    /// <summary>
    /// 当前事件类型名称对应的符号 ID。
    /// 异常日志需要事件名时，才会通过该 ID 还原字符串。
    /// </summary>
    public readonly int EventNameId;

    /// <summary>
    /// 创建异常诊断快照。
    /// </summary>
    /// <param name="syncFaults">
    /// 与同步 Flow handler 数组对齐的故障槽数组。
    /// </param>
    /// <param name="asyncFaults">
    /// 与异步 Flow handler 数组对齐的故障槽数组。
    /// </param>
    /// <param name="subscribeFaults">
    /// 与安全 Subscribe handler 数组对齐的故障槽数组。
    /// </param>
    public FaultTable(
        FaultSlot[] syncFaults,
        FaultSlot[] asyncFaults,
        FaultSlot[] subscribeFaults)
    {
        SyncFaults = syncFaults;
        AsyncFaults = asyncFaults;
        SubscribeFaults = subscribeFaults;

        // 事件名称只作为诊断符号保存，不参与派发。
        EventNameId = EventTypeSymbol<TEvent>.NameId;
    }
}
