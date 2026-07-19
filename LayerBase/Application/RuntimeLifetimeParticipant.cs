using LayerBase.Lifetime;
using LayerBase.Scope;

namespace LayerBase;

internal sealed class RuntimeLifetimeParticipant : ILifetimeParticipant
{
    private readonly string _name;
    private readonly Action _closeAdmission;
    private readonly Action _requestStop;
    private readonly DrainDelegate _drain;
    private readonly Action<TerminalCleanupRunner> _release;

    internal delegate LifetimeDrainResult DrainDelegate(ShutdownDeadline deadline);

    public RuntimeLifetimeParticipant(
        string name,
        Action closeAdmission,
        Action requestStop,
        DrainDelegate drain,
        Action<TerminalCleanupRunner> release)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _closeAdmission = closeAdmission ?? throw new ArgumentNullException(nameof(closeAdmission));
        _requestStop = requestStop ?? throw new ArgumentNullException(nameof(requestStop));
        _drain = drain ?? throw new ArgumentNullException(nameof(drain));
        _release = release ?? throw new ArgumentNullException(nameof(release));
    }

    public string LifetimeName => _name;

    public void CloseAdmission() => _closeAdmission();

    public void RequestStop() => _requestStop();

    public LifetimeDrainResult Drain(in ShutdownDeadline deadline) => _drain(deadline);

    public void Release(TerminalCleanupRunner cleanup) => _release(cleanup);
}
