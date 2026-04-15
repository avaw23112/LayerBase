using System;
using LayerBase.Core.Event;
using LayerBase.Layers;

namespace LayerBase.Core.UnmanagedList
{
    internal interface IUnmanagedList
    {
        void Pump();
    }

    internal class UnmanagedList<Value> : IUnmanagedList where Value : struct
    {
        private readonly PooledChunkedOverwriteQueue<Event<Value>> _queue;
        private readonly Layer _owner;

        public UnmanagedList(Layer owner)
        {
            _queue = new PooledChunkedOverwriteQueue<Event<Value>>();
            _owner = owner;
        }

        public void Pump()
        {
            int count = _queue.Count;
            while (count-- > 0)
            {
                if (!_queue.TryDequeue(out Event<Value> @event))
                {
                    throw new Exception("致命错误：内存队列读取失败");
                }

                EventHandledState handledState = _owner.Dispatch(in @event);
                _owner.NotifyQueuedEventProcessed(in @event, handledState);
            }
        }

        public void Post(in Event<Value> val)
        {
            _queue.EnqueueOverwrite(val);
        }
    }
}
