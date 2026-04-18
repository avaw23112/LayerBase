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
        private readonly GlobalEventCenter _center;
        private readonly PooledChunkedOverwriteQueue<Event<Value>> _queue;
        private readonly int _layerIndex;

        public UnmanagedList(GlobalEventCenter center, int layerIndex)
        {
            _center = center;
            _queue = new PooledChunkedOverwriteQueue<Event<Value>>();
            _layerIndex = layerIndex;
        }

        public void Pump()
        {
            int count = _queue.Count;
            if (count <= 0) return;

            bool forwarded = false;
            int lastTargetLayer = -1;
            ulong lastMask = 0;

            while (count-- > 0)
            {
                if (!_queue.TryDequeue(out Event<Value> @event))
                {
                    throw new Exception("致命错误：内存队列读取失败");
                }

                var state = _center.DispatchLocal(_layerIndex, in @event);
                
                // 处理顺序传播
                if (state != EventHandledState.Handled && @event.TargetMask != 0)
                {
                    ulong nextMask = @event.TargetMask & ~(1UL << _layerIndex);
                    if (nextMask != 0)
                    {
                        @event.TargetMask = nextMask;
                        
                        if (nextMask == lastMask)
                        {
                            _center.EnqueueEventInternal(lastTargetLayer, in @event);
                        }
                        else
                        {
                            int nextLayer = _center.FindFirstBit(nextMask);
                            lastMask = nextMask;
                            lastTargetLayer = nextLayer;
                            _center.EnqueueEventInternal(nextLayer, in @event);
                        }
                        forwarded = true;
                    }
                }
            }

            // 极致优化：仅在循环完全结束后，执行一次低频唤醒
            if (forwarded)
            {
                _center.WakeLayer(lastTargetLayer);
            }
        }

        public void Post(in Event<Value> val)
        {
            _queue.EnqueueOverwrite(val);
        }

        public bool TryDequeue(out Event<Value> @event)
        {
            return _queue.TryDequeue(out @event);
        }
    }
}
