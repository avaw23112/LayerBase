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
		private PooledChunkedOverwriteQueue<Event<Value>> m_pooledQueue = new PooledChunkedOverwriteQueue<Event<Value>>();
		private Layer Owner;

		public UnmanagedList(Layer owner)
		{
			this.Owner = owner;
		}
		
		public void Pump()
		{
			while (m_pooledQueue.Count > 0)
			{
				if (m_pooledQueue.TryDequeue(out Event<Value> val))
				{
					Owner.Dispatch(in val);
					switch (val.ForwardDir)
					{
						case EventForwardDir.BroadCast: Owner.PostEventToDoubleSide(val); break;
						case EventForwardDir.Bubble:    Owner.PostEventToHigherLayer(val);break;
						case EventForwardDir.Drop:      Owner.PostEventToLowerLayer(val); break; 
					}
					Owner.NotifyEventProcessed(val);
				}
				else
				{
					throw new Exception("致命错误.内存管理队列错误.");
				}
			}
		}

		public void Post(in Event<Value> val)
		{
			m_pooledQueue.EnqueueOverwrite(val);
		}
	}
}
