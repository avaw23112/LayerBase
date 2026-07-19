namespace LayerBase.Lifetime;

internal enum LifetimeState : byte
{
    Running = 0,
    Closing = 1,
    StopRequested = 2,
    Draining = 3,
    DrainTimedOut = 4,
    Releasing = 5,
    Released = 6,
    ReleasedWithErrors = 7
}
