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
                    // 移除当前层级的 Bit
                    ulong nextMask = @event.TargetMask & ~(1UL << _layerIndex);
                    if (nextMask != 0)
                    {
                        @event.TargetMask = nextMask;
                        // 找到下一个目标层级
                        int nextLayer = _center.FindFirstBit(nextMask);
                        _center.EnqueueEventInternal(nextLayer, in @event);
                    }
                }
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
