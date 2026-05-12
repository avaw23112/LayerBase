namespace LayerBase.Core.Event;

public interface IExpiredTimerSink<TPayload>
{
    bool TryAcceptExpired(in TPayload payload, TimerHandle handle);
}