using System;
using Events.Core.Event;
using Events.LayerChain;

namespace Events.Core.UnmanagedList
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
		
		/// <summary>
		/// TODO：等待事件改造，如提供PostDrop,PostBubble方法。
		/// </summary>
		/// <exception cref="Exception"></exception>
		public void Pump()
		{
			while (m_pooledQueue.Count > 0)
			{
				if (m_pooledQueue.TryDequeue(out Event<Value> val))
				{
					switch (val.Dir)
					{
						case EventDir.BroadCast:Owner.BroadCast(val); break;
						case EventDir.Bubble:Owner.Bubble(val);break;
						case EventDir.Drop:Owner.Drop(val); break; 
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