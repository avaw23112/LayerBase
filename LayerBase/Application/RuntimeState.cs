namespace LayerBase;

public enum RuntimeState : byte
{
    Created,
    Building,
    Built,
    Activating,
    Running,
    Stopping,
    Stopped,
    Disposing,
    Disposed,
    Faulted
}
