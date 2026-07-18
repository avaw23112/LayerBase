using LayerBase.DI.Options;

namespace LayerBase.Layers;

internal sealed class ScopeLayerLifecycleState
{
    public readonly List<IInitializable> Initializables = new();
    public readonly List<IPostBuild> PostBuilds = new();
    public readonly List<IRuntimeStart> RuntimeStarts = new();
    public readonly List<IUpdate> Updates = new();
    public readonly List<IFixedUpdate> FixedUpdates = new();
    public readonly List<IRuntimeStop> RuntimeStops = new();
    public readonly List<IDisposable> Disposables = new();

    public void RunInitialize()
    {
        for (int i = 0; i < Initializables.Count; i++)
            Initializables[i].Initialize();
    }

    public void RunPostBuild()
    {
        for (int i = 0; i < PostBuilds.Count; i++)
            PostBuilds[i].PostBuild();
    }

    public void RunRuntimeStart()
    {
        for (int i = 0; i < RuntimeStarts.Count; i++)
            RuntimeStarts[i].RuntimeStart();
    }

    public void PumpUpdate(float deltaTime)
    {
        for (int i = 0; i < Updates.Count; i++)
            Updates[i].Update();
    }

    public void PumpFixedUpdate(float fixedDeltaTime)
    {
        for (int i = 0; i < FixedUpdates.Count; i++)
            FixedUpdates[i].FixedUpdate(fixedDeltaTime);
    }

    public void RunRuntimeStop()
    {
        for (int i = RuntimeStops.Count - 1; i >= 0; i--)
            RuntimeStops[i].RuntimeStop();
    }

    public void RunDispose()
    {
        for (int i = Disposables.Count - 1; i >= 0; i--)
            Disposables[i].Dispose();
    }
}
