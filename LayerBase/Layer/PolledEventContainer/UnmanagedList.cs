using LayerBase.Core.Event;
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
		private PooledChunkedOverwriteQueue<Event<Value>> m_pooledQueue ;
		private Layer Owner;

		public UnmanagedList(Layer owner)
		{
			int MaxQueueSize = EventMetaDataHandler.MaxBufferSize<Value>();
			EventQueueOverflowStrategy OverflowStrategy = EventMetaDataHandler.EventQueueOverflowStrategy<Value>();
			m_pooledQueue = new PooledChunkedOverwriteQueue<Event<Value>>(maxCapacity:MaxQueueSize,overflowStrategy:OverflowStrategy);
			this.Owner = owner;
		}
		
		public void Pump()
		{
			//处理事件前,先调用元数据处理进行合并
			EventMetaDataHandler.EventMergeStrategy<Value>(m_pooledQueue);
			
			//每次Pump只处理已经有事件,而不直接处理完毕,避免无限循环
			int count = m_pooledQueue.Count;
			while (count-- > 0)
			{
				if (m_pooledQueue.TryDequeue(out Event<Value> val))
				{
					//未满足该事件的定时策略，待定
					if (!EventMetaData<Value>.IsFrequencyGateOpen)
					{
						m_pooledQueue.EnqueueOverwrite(val);
						continue;
					}
					
					LayerDispatchStrategy strategy = LayerDispatchStrategy.None;
					GetStrategy(val, ref strategy);
					
					if (strategy == LayerDispatchStrategy.Throw)
					{
						Owner.NotifyEventProcessed(val);
						return;
					}
					if (strategy == LayerDispatchStrategy.Post)
					{
						m_pooledQueue.EnqueueOverwrite(val);
						continue;
					}
					if (strategy == LayerDispatchStrategy.None)
					{
						EventHandledState eventHandledState = Owner.Dispatch(in val);
						if (eventHandledState == EventHandledState.Handled)
						{
							Owner.NotifyEventProcessed(val);
						}
					}
					//Ignore :不处理，直接丢到下一层
					
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

		private void GetStrategy(Event<Value> val, ref LayerDispatchStrategy strategy)
		{
			Owner.m_eventStateTracer.TryGet(val.TraceToken, out var eventState);
			var layerDispatchStrategy = LayerMetaData.GetDispatchStrategy(
				this.GetType(), eventState.CatalogueToken);
			strategy = layerDispatchStrategy;
		}

		public void Post(in Event<Value> val)
		{
			m_pooledQueue.EnqueueOverwrite(val);
		}
	}
}
