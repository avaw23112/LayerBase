using System.Threading;

namespace LayerBase.Scope;

public static class ScopeExecution
{
    private static readonly AsyncLocal<ScopeExecutionFrameHolder?> s_current = new();

    public static ScopeExecutionFrame Current => s_current.Value?.Context ?? ScopeExecutionFrame.None;

    internal static IDisposable Enter(ScopeRuntime runtime)
    {
        ScopeExecutionFrameHolder? previous = s_current.Value;
        s_current.Value = new ScopeExecutionFrameHolder(new ScopeExecutionFrame(runtime.ScopeId, runtime));
        return new PopToken(previous);
    }

    private sealed class PopToken : IDisposable
    {
        private readonly ScopeExecutionFrameHolder? _previous;
        private bool _disposed;

        public PopToken(ScopeExecutionFrameHolder? previous)
        {
            _previous = previous;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            s_current.Value = _previous;
        }
    }

    private sealed class ScopeExecutionFrameHolder
    {
        public ScopeExecutionFrameHolder(ScopeExecutionFrame context)
        {
            Context = context;
        }

        public ScopeExecutionFrame Context { get; }
    }
}

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
