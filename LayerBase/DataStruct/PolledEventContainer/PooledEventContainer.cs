using LayerBase.Core.Event;
using LayerBase.Core.UnmanagedList;
using LayerBase.Layers;

namespace LayerBase.Core.PolledEventContainer
{
    public class PooledEventContainer
    {
        private Dictionary<int, IUnmanagedList> m_unmanagedDic = new();
        private Layer Owner;

        public PooledEventContainer(Layer owner)
        {
            if (owner == null)
            {
                throw new Exception("致命错误.使用错误层级构建");
            }
            this.Owner = owner;
        }

        internal void Pump()
        {
            var valSet = m_unmanagedDic.Values;
            foreach (var val in valSet)
            {
                val.Pump();
            }
        }
        
        public void Post<Value>(Event<Value> @event) where Value : struct
        {
            int typeId = EventTypeId<Value>.Id;

            if (!m_unmanagedDic.TryGetValue(typeId,out IUnmanagedList list))
            {
                UnmanagedList<Value> listNewEventContainer = new UnmanagedList<Value>(Owner);
                m_unmanagedDic.Add(typeId,listNewEventContainer);
                list = listNewEventContainer;
            }
            UnmanagedList<Value> listEventContainer = list as UnmanagedList<Value>;
            if (listEventContainer == null)
            {
                throw new Exception($"typeId :{typeId} Type :{typeof(Value).Name} 对应了错误的UnmanagedList");
            }
            listEventContainer.Post(@event);
        }
    }
}