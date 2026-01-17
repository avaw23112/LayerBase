using System;
using LayerBase.Core.Event;
using LayerBase.Core.EventStateTrace;
using LayerBase.Event.EventMetaData;
using LayerBase.Layers;
using LayerBase.Layers.LayerMetaData;

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
            int maxQueueSize = EventMetaDataHandler.MaxBufferSize<Value>();
            EventQueueOverflowStrategy overflowStrategy = EventMetaDataHandler.EventQueueOverflowStrategy<Value>();
            _queue = new PooledChunkedOverwriteQueue<Event<Value>>(maxCapacity: maxQueueSize, overflowStrategy: overflowStrategy);
            _owner = owner;
        }

        public void Pump()
        {
            // 处理前先按照元数据策略进行合并，减少重复事件。
            EventMetaDataHandler.EventMergeStrategy<Value>(_queue);

            int count = _queue.Count;
            while (count-- > 0)
            {
                if (!_queue.TryDequeue(out Event<Value> @event))
                {
                    throw new Exception("致命错误：内存队列读取失败。");
                }

                if (!EventMetaData<Value>.IsFrequencyGateOpen)
                {
                    _queue.EnqueueOverwrite(@event);
                    continue;
                }

                var strategy = ResolveStrategy(@event);
                if (strategy == LayerDispatchStrategy.Throw)
                {
                    _owner.NotifyEventProcessed(@event);
                    continue;
                }
                if (strategy == LayerDispatchStrategy.Post)
                {
                    _queue.EnqueueOverwrite(@event);
                    continue;
                }
                if (strategy == LayerDispatchStrategy.Ignore)
                {
                    Forward(@event);
                    _owner.NotifyEventProcessed(@event);
                    continue;
                }

                EventHandledState handledState = _owner.Dispatch(in @event);
                if (handledState != EventHandledState.Handled)
                {
                    Forward(@event);
                }

                _owner.NotifyEventProcessed(@event);
            }
        }

        private void Forward(in Event<Value> @event)
        {
            switch (@event.ForwardDir)
            {
                case EventForwardDir.BroadCast:
                    _owner.PostEventToDoubleSide(@event);
                    break;
                case EventForwardDir.Bubble:
                    _owner.PostEventToHigherLayer(@event);
                    break;
                case EventForwardDir.Drop:
                    _owner.PostEventToLowerLayer(@event);
                    break;
            }
        }

        private LayerDispatchStrategy ResolveStrategy(in Event<Value> @event)
        {
            var tracer = _owner.m_eventStateTracer;
            if (tracer != null && tracer.TryGet(@event.TraceToken, out var eventState))
            {
                return LayerMetaData.GetDispatchStrategy(_owner.GetType(), eventState.CatalogueToken);
            }

            return LayerDispatchStrategy.None;
        }

        public void Post(in Event<Value> val)
        {
            _queue.EnqueueOverwrite(val);
        }
    }
}
