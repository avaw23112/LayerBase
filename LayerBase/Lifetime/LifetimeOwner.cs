using LayerBase.Scope;

namespace LayerBase.Lifetime;

internal sealed class LifetimeOwner
{
    private readonly List<ILifetimeParticipant> _children = new();
    private LifetimeState _state;

    public LifetimeState State => _state;

    public int ChildCount => _children.Count;

    public T Own<T>(T participant)
        where T : class, ILifetimeParticipant
    {
        if (participant == null)
            throw new ArgumentNullException(nameof(participant));

        if (_state != LifetimeState.Running)
            throw new InvalidOperationException(
                "Cannot register lifetime participants after shutdown begins.");

        _children.Add(participant);
        return participant;
    }

    public ShutdownReport Shutdown(in ShutdownDeadline deadline)
    {
        if (_state >= LifetimeState.Draining)
        {
            return ContinueShutdown(in deadline);
        }

        return RunShutdown(in deadline);
    }

    private ShutdownReport RunShutdown(in ShutdownDeadline deadline)
    {
        _state = LifetimeState.Closing;

        for (int i = _children.Count - 1; i >= 0; i--)
        {
            _children[i].CloseAdmission();
        }

        _state = LifetimeState.StopRequested;

        for (int i = _children.Count - 1; i >= 0; i--)
        {
            _children[i].RequestStop();
        }

        _state = LifetimeState.Draining;

        for (int i = _children.Count - 1; i >= 0; i--)
        {
            if (deadline.IsExpired)
            {
                _state = LifetimeState.DrainTimedOut;
                return new ShutdownReport(LifetimeState.DrainTimedOut, false, null);
            }

            LifetimeDrainResult result = _children[i].Drain(in deadline);

            if (result == LifetimeDrainResult.TimedOut)
            {
                _state = LifetimeState.DrainTimedOut;
                return new ShutdownReport(LifetimeState.DrainTimedOut, false, null);
            }
        }

        return ReleaseAll();
    }

    private ShutdownReport ContinueShutdown(in ShutdownDeadline deadline)
    {
        if (_state == LifetimeState.Released ||
            _state == LifetimeState.ReleasedWithErrors)
        {
            return new ShutdownReport(_state, false, null);
        }

        if (_state < LifetimeState.Draining)
            return RunShutdown(in deadline);

        if (_state == LifetimeState.DrainTimedOut)
        {
            for (int i = _children.Count - 1; i >= 0; i--)
            {
                if (deadline.IsExpired)
                    return new ShutdownReport(LifetimeState.DrainTimedOut, false, null);

                LifetimeDrainResult result = _children[i].Drain(in deadline);

                if (result == LifetimeDrainResult.TimedOut)
                    return new ShutdownReport(LifetimeState.DrainTimedOut, false, null);
            }

            return ReleaseAll();
        }

        return RunShutdown(in deadline);
    }

    private ShutdownReport ReleaseAll()
    {
        _state = LifetimeState.Releasing;
        var cleanup = new TerminalCleanupRunner();

        for (int i = _children.Count - 1; i >= 0; i--)
        {
            int capturedIndex = i;
            cleanup.Run(
                _children[i].LifetimeName,
                () => _children[capturedIndex].Release(cleanup));
        }

        if (cleanup.HasErrors)
        {
            _state = LifetimeState.ReleasedWithErrors;
            return new ShutdownReport(
                LifetimeState.ReleasedWithErrors,
                true,
                cleanup.BuildException());
        }

        _state = LifetimeState.Released;
        return new ShutdownReport(LifetimeState.Released, false, null);
    }
}
