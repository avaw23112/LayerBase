using System.Buffers;

namespace LayerBase.Core
{
	public enum EventQueueOverflowStrategy
	{
		/// <summary>
		/// 溢出报错
		/// </summary>
		ThrowException,

		/// <summary>
		/// 抛弃新进事件
		/// </summary>
		Throw,

		/// <summary>
		/// 丢弃最老元素
		/// </summary>
		OverWrite,

		/// <summary>
		/// 队列扩容
		/// </summary>
		Scaling
	}

	/// <summary>
	/// 分段双端队列，支持覆盖写和多种溢出策略。
	/// </summary>
	public sealed class PooledChunkedOverwriteQueue<T> : IDisposable where T : struct
	{
		private readonly ArrayPool<T> _pool;
		private readonly int _chunkSize;
		private int _maxCapacity;
		private readonly bool _clearOnDequeue;
		private readonly bool _clearOnReturn;
		private readonly EventQueueOverflowStrategy _overflowStrategy;

		private Segment? _headSeg;
		private Segment? _tailSeg;
		private int _headIndex;
		private int _tailIndex; // 指向尾段的“下一写入位置”
		private int _count;
		private bool _disposed;

		internal PooledChunkedOverwriteQueue(
			int chunkSize = 256,
			int maxCapacity = 0,
			ArrayPool<T>? pool = null,
			bool clearOnDequeue = false,
			bool clearOnReturn = false)
			: this(chunkSize, maxCapacity, pool, clearOnDequeue, clearOnReturn, EventQueueOverflowStrategy.OverWrite)
		{
		}

		internal PooledChunkedOverwriteQueue(
			int maxCapacity,
			EventQueueOverflowStrategy overflowStrategy)
			:this(256, maxCapacity, null, false, false, overflowStrategy)
		{
			
		}

		internal PooledChunkedOverwriteQueue(
			int chunkSize,
			int maxCapacity,
			ArrayPool<T>? pool,
			bool clearOnDequeue,
			bool clearOnReturn,
			EventQueueOverflowStrategy overflowStrategy)
		{
			if (chunkSize <= 0)
				throw new ArgumentOutOfRangeException(nameof(chunkSize));

			_pool = pool ?? ArrayPool<T>.Shared;
			_chunkSize = chunkSize;
			_maxCapacity = maxCapacity;
			_clearOnDequeue = clearOnDequeue;
			_clearOnReturn = clearOnReturn;
			_overflowStrategy = overflowStrategy;

			_headSeg = null;
			_tailSeg = null;
			_headIndex = 0;
			_tailIndex = 0;
			_count = 0;
			_disposed = false;
		}

		internal int Count => _count;
		internal bool IsEmpty => _count == 0;

		/// <summary>
		/// 追加到队尾（保持旧 API 名称），使用配置的溢出策略。
		/// </summary>
		internal void EnqueueOverwrite(in T item)
		{
			EnqueueBack(item, _overflowStrategy);
		}

		/// <summary>
		/// 追加到队头。
		/// </summary>
		internal void EnqueueFront(in T item)
		{
			EnqueueFront(item, _overflowStrategy);
		}

		internal bool TryDequeue(out T item)
		{
			ThrowIfDisposed();

			if (_count == 0)
			{
				item = default;
				return false;
			}

			var seg = _headSeg!;
			item = seg.Buffer[_headIndex];
			if (_clearOnDequeue)
				seg.Buffer[_headIndex] = default;

			_headIndex++;
			_count--;

			if (_count == 0)
			{
				ResetToEmpty();
			}
			else if (_headIndex >= _chunkSize)
			{
				AdvanceHeadSegment();
			}

			return true;
		}

		internal bool TryDequeueBack(out T item)
		{
			ThrowIfDisposed();

			if (_count == 0)
			{
				item = default;
				return false;
			}

			EnsureTailForRead();

			int readIndex = _tailIndex - 1;
			item = _tailSeg!.Buffer[readIndex];
			if (_clearOnDequeue)
				_tailSeg.Buffer[readIndex] = default;

			_tailIndex--;
			_count--;

			if (_count == 0)
			{
				ResetToEmpty();
			}
			else if (_tailIndex == 0 && _tailSeg!.Prev != null)
			{
				MoveTailToPreviousSegment();
			}

			return true;
		}

		internal bool TryPeek(out T item)
		{
			ThrowIfDisposed();

			if (_count == 0)
			{
				item = default;
				return false;
			}

			item = _headSeg!.Buffer[_headIndex];
			return true;
		}

		internal bool TryPeekBack(out T item)
		{
			ThrowIfDisposed();

			if (_count == 0)
			{
				item = default;
				return false;
			}

			EnsureTailForRead();
			item = _tailSeg!.Buffer[_tailIndex - 1];
			return true;
		}

		internal void Clear()
		{
			ThrowIfDisposed();
			ClearCore();
		}

		private void ClearCore()
		{
			var cur = _headSeg;
			while (cur != null)
			{
				var next = cur.Next;
				_pool.Return(cur.Buffer, _clearOnReturn);
				cur.Next = null;
				cur.Prev = null;
				cur = next;
			}

			ResetToEmpty();
		}

		internal void Prewarm(int capacity)
		{
			ThrowIfDisposed();

			if (capacity <= 0)
				return;

			if (_maxCapacity > 0 && capacity > _maxCapacity)
				capacity = _maxCapacity;

			int neededSegments = (capacity + _chunkSize - 1) / _chunkSize;
			int existingSegments = CountSegments();

			if (_headSeg == null && neededSegments > 0)
			{
				CreateInitialSegment();
				existingSegments = 1;
			}

			int toAdd = neededSegments - existingSegments;
			for (int i = 0; i < toAdd; i++)
			{
				AppendNewTailSegment();
			}
		}

		public void Dispose()
		{
			if (_disposed)
				return;

			ClearCore();
			_disposed = true;
		}

		private sealed class Segment
		{
			public T[] Buffer;
			public Segment? Next;
			public Segment? Prev;

			public Segment(T[] buffer)
			{
				Buffer = buffer;
			}
		}

		private void EnqueueBack(in T item, EventQueueOverflowStrategy strategy)
		{
			ThrowIfDisposed();

			if (!EnsureCapacityForEnqueue(strategy))
				return;

			EnsureTailSegmentHasSpace();

			_tailSeg!.Buffer[_tailIndex] = item;
			_tailIndex++;
			_count++;
		}

		private void EnqueueFront(in T item, EventQueueOverflowStrategy strategy)
		{
			ThrowIfDisposed();

			if (!EnsureCapacityForEnqueue(strategy))
				return;

			if (_count == 0)
			{
				EnsureTailSegmentHasSpace();
				_headSeg!.Buffer[0] = item;
				_headIndex = 0;
				_tailIndex = 1;
				_count = 1;
				return;
			}

			if (_headIndex == 0)
			{
				PrependNewHeadSegment();
			}

			_headIndex--;
			_headSeg!.Buffer[_headIndex] = item;
			_count++;
		}

		private bool EnsureCapacityForEnqueue(EventQueueOverflowStrategy strategy)
		{
			if (_maxCapacity <= 0 || _count < _maxCapacity)
				return true;

			switch (strategy)
			{
				case EventQueueOverflowStrategy.ThrowException:
					throw new InvalidOperationException("Event queue overflow");
				case EventQueueOverflowStrategy.Throw:
					return false;
				case EventQueueOverflowStrategy.OverWrite:
					DropOldest(1);
					return true;
				case EventQueueOverflowStrategy.Scaling:
					ScaleCapacity();
					return true;
				default:
					return true;
			}
		}

		private void ScaleCapacity()
		{
			if (_maxCapacity <= 0)
				return;

			int desired = _count + 1;
			int growth = Math.Max(_chunkSize, _maxCapacity);
			long next = (long)_maxCapacity + growth;
			if (next < desired)
				next = desired;

			_maxCapacity = (int)Math.Min(int.MaxValue, next);
		}

		private void EnsureTailSegmentHasSpace()
		{
			if (_tailSeg == null)
			{
				CreateInitialSegment();
				return;
			}

			if (_tailIndex >= _chunkSize)
			{
				AppendNewTailSegment();
			}
		}

		private void CreateInitialSegment()
		{
			var buffer = _pool.Rent(_chunkSize);
			var seg = new Segment(buffer);
			_headSeg = seg;
			_tailSeg = seg;
			_headIndex = 0;
			_tailIndex = 0;
		}

		private void AppendNewTailSegment()
		{
			var buffer = _pool.Rent(_chunkSize);
			var newSeg = new Segment(buffer)
			{
				Prev = _tailSeg
			};

			if (_tailSeg != null)
			{
				_tailSeg.Next = newSeg;
			}

			_tailSeg = newSeg;
			if (_headSeg == null)
				_headSeg = newSeg;

			_tailIndex = 0;
		}

		private void PrependNewHeadSegment()
		{
			var buffer = _pool.Rent(_chunkSize);
			var newHead = new Segment(buffer)
			{
				Next = _headSeg
			};

			if (_headSeg != null)
			{
				_headSeg.Prev = newHead;
			}
			else
			{
				_tailSeg = newHead;
			}

			_headSeg = newHead;
			_headIndex = _chunkSize;
		}

		private void AdvanceHeadSegment()
		{
			var oldHead = _headSeg!;
			_headSeg = oldHead.Next;
			_pool.Return(oldHead.Buffer, _clearOnReturn);

			if (_headSeg == null)
			{
				_tailSeg = null;
				_tailIndex = 0;
				_headIndex = 0;
			}
			else
			{
				_headSeg.Prev = null;
				_headIndex = 0;
			}
		}

		private void MoveTailToPreviousSegment()
		{
			var oldTail = _tailSeg!;
			var newTail = oldTail.Prev;
			if (newTail == null)
				return;

			newTail.Next = null;
			_tailSeg = newTail;
			_pool.Return(oldTail.Buffer, _clearOnReturn);

			if (_tailSeg == _headSeg)
			{
				_tailIndex = _headIndex + _count;
			}
			else
			{
				_tailIndex = _chunkSize;
			}
		}

		private void EnsureTailForRead()
		{
			if (_tailSeg != null && _tailIndex == 0 && _tailSeg.Prev != null)
			{
				MoveTailToPreviousSegment();
			}
		}

		private void DropOldest(int n)
		{
			if (n <= 0 || _count == 0)
				return;

			if (n > _count)
				n = _count;

			for (int i = 0; i < n; i++)
			{
				var seg = _headSeg!;
				if (_clearOnDequeue)
					seg.Buffer[_headIndex] = default;

				_headIndex++;
				_count--;

				if (_count == 0)
				{
					ResetToEmpty();
					return;
				}

				if (_headIndex >= _chunkSize)
				{
					AdvanceHeadSegment();
				}
			}
		}

		private void ResetToEmpty()
		{
			_headSeg = null;
			_tailSeg = null;
			_headIndex = 0;
			_tailIndex = 0;
			_count = 0;
		}

		private int CountSegments()
		{
			int count = 0;
			var cur = _headSeg;
			while (cur != null)
			{
				count++;
				cur = cur.Next;
			}
			return count;
		}

		private void ThrowIfDisposed()
		{
			if (_disposed)
				throw new ObjectDisposedException(nameof(PooledChunkedOverwriteQueue<T>));
		}
	}
}
