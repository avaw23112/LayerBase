using System.Runtime.CompilerServices;

namespace LayerBase.Scope;

/// <summary>
/// Scope 执行上下文。使用 [ThreadStatic] 零分配跟踪当前 Scope。
/// 每次 Enter 返回 readonly struct token，Dispose 时恢复上下文。
/// </summary>
public static class ScopeExecution
{
    [ThreadStatic]
    private static ScopeExecutionFrame s_current;

    public static ScopeExecutionFrame Current => s_current;

    public static bool HasCurrent => s_current.ScopeId >= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ScopeExecutionToken Enter(ScopeRuntime runtime)
    {
        var previous = s_current;
        s_current = new ScopeExecutionFrame(runtime.ScopeId, runtime);
        return new ScopeExecutionToken(previous);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Restore(in ScopeExecutionFrame previous)
    {
        s_current = previous;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Clear()
    {
        s_current = ScopeExecutionFrame.None;
    }
}

/// <summary>
/// Scope Execution 上下文的快照令牌。Dispose 时恢复上下文。
/// 必须是 readonly struct 且避免装箱，才能零分配。
/// </summary>
public readonly struct ScopeExecutionToken : IDisposable
{
    private readonly ScopeExecutionFrame _previous;

    internal ScopeExecutionToken(ScopeExecutionFrame previous)
    {
        _previous = previous;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        ScopeExecution.Restore(_previous);
    }
}

/// <summary>
/// 当前 Scope Execution 帧信息。
/// </summary>
public readonly struct ScopeExecutionFrame
{
    internal static readonly ScopeExecutionFrame None = new(-1, null);

    internal ScopeExecutionFrame(int scopeId, ScopeRuntime? runtime)
    {
        ScopeId = scopeId;
        Runtime = runtime;
    }

    public int ScopeId { get; }

    public ScopeRuntime? Runtime { get; }
}
