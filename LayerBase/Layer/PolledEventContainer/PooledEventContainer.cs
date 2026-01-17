using LayerBase.Core.Event;
using LayerBase.Core.UnmanagedList;
using LayerBase.Layers;

namespace LayerBase.Core.PolledEventContainer
{
    /// <summary>
    /// 按事件类型维护的队列容器。
    /// </summary>
    internal class PooledEventContainer
    {
        private readonly Dictionary<int, IUnmanagedList> _queuesByType = new();
        private readonly Layer _owner;

        internal PooledEventContainer(Layer owner)
        {
            _owner = owner ?? throw new Exception("致命错误.使用错误层级构建");
        }

        internal void Pump()
        {
            foreach (var queue in _queuesByType.Values)
            {
                queue.Pump();
            }
        }
        
        internal void Post<Value>(Event<Value> @event) where Value : struct
        {
            int typeId = EventTypeId<Value>.Id;

            if (!_queuesByType.TryGetValue(typeId, out IUnmanagedList list))
            {
                var newQueue = new UnmanagedList<Value>(_owner);
                _queuesByType.Add(typeId, newQueue);
                list = newQueue;
            }

            if (list is not UnmanagedList<Value> typedQueue)
            {
                throw new Exception($"typeId:{typeId} Type:{typeof(Value).Name} 对应了错误的 UnmanagedList");
            }

            typedQueue.Post(@event);
        }
    }
}
