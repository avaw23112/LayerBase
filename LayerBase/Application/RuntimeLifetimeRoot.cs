using LayerBase.Lifetime;

namespace LayerBase;

internal sealed class RuntimeLifetimeRoot
{
    private readonly LifetimeOwner _owner = new();

    public LifetimeOwner Owner => _owner;

    public bool HasDrainTimedOut =>
        _owner.State is LifetimeState.DrainTimedOut;

    public bool IsReleased =>
        _owner.State is LifetimeState.Released or LifetimeState.ReleasedWithErrors;

    public ShutdownReport Shutdown(in Scope.ShutdownDeadline deadline)
    {
        return _owner.Shutdown(in deadline);
    }
}
