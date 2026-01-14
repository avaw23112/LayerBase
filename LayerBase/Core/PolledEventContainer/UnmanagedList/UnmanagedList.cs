using System;
using LayerBase.Core.Event;
using LayerBase.LayerChain;

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
					Owner.DispatchEvent(in val);
					switch (val.ForwardDir)
					{
						case EventForwardDir.BroadCast: Owner.BroadCastEvent(val); break;
						case EventForwardDir.Bubble:    Owner.BubbleEvent(val);break;
						case EventForwardDir.Drop:      Owner.DropEvent(val); break; 
					}
				}
				else
				{
					throw new Exception("致命错误.内存管理队列错误.");
				}
			}
		}

		public void Post(Event<Value> val)
		{
			m_pooledQueue.EnqueueOverwrite(val);
		}
	}
}