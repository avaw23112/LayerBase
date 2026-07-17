using System;
using System.Collections.Concurrent;

namespace LayerBase.Event.EventMetaData;

internal sealed class EventExpectationQueue
{
    private readonly ConcurrentQueue<IInvocation> _pending = new();

    public bool HasPending => !_pending.IsEmpty;

    public void Enqueue<TEvent>(
        EventMetaData<TEvent> metaData,
        in TEvent value,
        Exception exception)
        where TEvent : struct
    {
        if (metaData == null)
            throw new ArgumentNullException(nameof(metaData));

        if (exception == null)
            throw new ArgumentNullException(nameof(exception));

        _pending.Enqueue(
            new Invocation<TEvent>(
                metaData,
                in value,
                exception));
    }

    public void Pump(Action<Exception>? reportObserverException)
    {
        while (_pending.TryDequeue(out IInvocation? invocation))
        {
            try
            {
                invocation.Invoke();
            }
            catch (Exception exception)
            {
                reportObserverException?.Invoke(exception);
            }
        }
    }

    public void Clear()
    {
        while (_pending.TryDequeue(out _))
        {
        }
    }

    private interface IInvocation
    {
        void Invoke();
    }

    private readonly struct Invocation<TEvent> : IInvocation
        where TEvent : struct
    {
        private readonly EventMetaData<TEvent> _metaData;
        private readonly TEvent _value;
        private readonly Exception _exception;

        public Invocation(
            EventMetaData<TEvent> metaData,
            in TEvent value,
            Exception exception)
        {
            _metaData = metaData;
            _value = value;
            _exception = exception;
        }

        public void Invoke()
        {
            _metaData.OnEventExpectation(
                _value,
                _exception);
        }
    }
}
