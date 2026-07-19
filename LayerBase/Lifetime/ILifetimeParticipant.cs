namespace LayerBase.Lifetime;

internal interface ILifetimeParticipant
{
    string LifetimeName { get; }

    void CloseAdmission();

    void RequestStop();

    LifetimeDrainResult Drain(in Scope.ShutdownDeadline deadline);

    void Release(TerminalCleanupRunner cleanup);
}
