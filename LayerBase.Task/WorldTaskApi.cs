using System.Threading;

namespace LayerBase.Async;

public sealed class WorldTaskApi
{
    private readonly LayerBaseSynchronizationContext _context;

    public WorldTaskApi(LayerBaseSynchronizationContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public LBTask NextFrame(CancellationToken token = default)
    {
        return LBTask.NextFrame(_context, token);
    }

    public LBTask RunOnMainThread(Action action)
    {
        return LBTask.RunOnMainThread(action, _context);
    }

    public LBTask<T> RunOnMainThread<T>(Func<T> func)
    {
        return LBTask<T>.RunOnMainThread(func, _context);
    }

    public LBTask Delay(TimeSpan delay, CancellationToken token = default)
    {
        // Delay typically uses global timer but captures current context.
        // If we want it to capture THIS world context specifically:
        using var scope = _context.EnterScope();
        return LBTask.Delay(delay, token);
    }
}