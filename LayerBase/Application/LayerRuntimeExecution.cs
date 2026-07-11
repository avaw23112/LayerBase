namespace LayerBase;

internal static class LayerRuntimeExecution
{
    private static readonly AsyncLocal<LayerRuntime?> s_current = new();

    public static LayerRuntime? CurrentRuntime => s_current.Value;

    public static IDisposable Enter(LayerRuntime runtime)
    {
        LayerRuntime? previous = s_current.Value;
        s_current.Value = runtime ?? throw new ArgumentNullException(nameof(runtime));
        return new PopToken(previous);
    }

    private sealed class PopToken : IDisposable
    {
        private readonly LayerRuntime? _previous;
        private bool _disposed;

        public PopToken(LayerRuntime? previous)
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
}
