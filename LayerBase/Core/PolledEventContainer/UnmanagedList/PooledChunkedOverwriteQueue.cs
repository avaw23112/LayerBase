using System;
using System.Buffers;

namespace LayerBase.Core
{
	/// <summary>
	/// 分段 FIFO 队列（Chunked / Segmented Queue）：
	/// - FIFO：First In First Out，先进先出（最早入队的最先出队）。
	/// - 分段：用多个“固定大小的数组段”串起来存数据；满一段就追加一段，不需要整体搬家复制。
	/// - 覆盖写：当达到 MaxCapacity（最大容量）时，新入队会丢弃最老元素，保持容量不增长。
	/// </summary>
	/// <typeparam name="T">
	/// 元素类型；你这里的 Event 是 struct，所以用 where T : struct 以避免每个元素都走堆分配。
	/// 注意：struct 里也可能包含引用字段（比如 string），那就要考虑清槽位以避免引用被池数组“意外保留”。
	/// </typeparam>
	public sealed class PooledChunkedOverwriteQueue<T> : IDisposable where T : struct
	{
		// =========================
		// 配置项（构造参数决定）
		// =========================

		private readonly ArrayPool<T> _pool;                    // 数组池：负责租/还段数组，核心复用机制
		private readonly int _chunkSize;                        // 每段数组的“段大小”（一个段能装多少个 T）
		private readonly int _maxCapacity;                      // 最大容量（元素数上限）；<=0 表示不设上限（无限增长但仍分段）
		private readonly bool _clearOnDequeue;                  // 出队后是否清空读过的槽位（避免引用字段被保留）
		private readonly bool _clearOnReturn;                   // 归还段数组时是否清空整段（更安全但更慢）

		// =========================
		// 运行时状态（队列指针）
		// =========================

		private Segment _headSeg;                              // 头段：当前正在读的段
		private Segment _tailSeg;                              // 尾段：当前正在写的段
		private int _headIndex;                                 // 头段内读索引：下一次出队从 headSeg.Buffer[headIndex] 读
		private int _tailIndex;                                 // 尾段内写索引：下一次入队写到 tailSeg.Buffer[tailIndex]
		private int _count;                                     // 当前队列元素数量（用于 IsEmpty / 满判断）
		private bool _disposed;                                 // 是否已 Dispose；Dispose 后不允许继续使用

		/// <summary>
		/// 构造一个“分段 + 覆盖写 + 池化”的 FIFO 队列。
		/// </summary>
		/// <param name="chunkSize">
		/// 段大小：每段数组能容纳多少个元素；
		/// - 越大：段更少，指针跳转更少，缓存局部性更好；
		/// - 越小：更灵活，浪费更少，但段对象/指针更多。
		/// 建议：128/256/512 这类值常见（取决于事件量与缓存友好程度）。
		/// </param>
		/// <param name="maxCapacity">
		/// 最大容量（元素数上限）：
		/// - >0：启用覆盖写；满了就丢弃最老元素，再写入新元素（保持固定容量）。
		/// - <=0：不设上限；队列会按需追加新段（仍旧复用内存，但峰值会增长）。
		/// </param>
		/// <param name="pool">
		/// 数组池：默认 ArrayPool<T>.Shared（全局共享池）；
		/// 你也可以传自定义池（比如更可控的池实现）。
		/// </param>
		/// <param name="clearOnDequeue">
		/// 出队后是否把槽位写回 default：
		/// - true：更安全（struct 内含引用字段时很重要）；
		/// - false：更快（纯值类型 struct 最推荐）。
		/// </param>
		/// <param name="clearOnReturn">
		/// Return 段数组给池时是否清空整段：
		/// - true：最安全（防止引用泄漏/驻留）；
		/// - false：更快（纯值类型 struct 最推荐）。
		/// </param>
		public PooledChunkedOverwriteQueue(
			int chunkSize = 256,                                // chunkSize：默认 256（你可按事件吞吐调整）
			int maxCapacity = 0,                                // maxCapacity：默认 0 表示不设上限；若你要覆盖写就传一个正数
			ArrayPool<T>? pool = null,                          // pool：允许 null，表示使用共享池
			bool clearOnDequeue = false,                        // clearOnDequeue：默认 false，追求性能
			bool clearOnReturn = false                          // clearOnReturn：默认 false，追求性能
		)
		{
			if (chunkSize <= 0)                                 // 段大小必须 >0，否则无法存储
				throw new ArgumentOutOfRangeException(nameof(chunkSize));

			_pool = pool ?? ArrayPool<T>.Shared;                // pool 为空则用共享池
			_chunkSize = chunkSize;                             // 保存段大小
			_maxCapacity = maxCapacity;                         // 保存最大容量（<=0 代表无限）
			_clearOnDequeue = clearOnDequeue;                   // 保存“出队清槽位”开关
			_clearOnReturn = clearOnReturn;                     // 保存“归还清整段”开关

			_headSeg = null;                                    // 初始没有段（首次入队再租）
			_tailSeg = null;                                    // 初始没有段
			_headIndex = 0;                                     // 头读索引初始为 0
			_tailIndex = 0;                                     // 尾写索引初始为 0
			_count = 0;                                         // 初始元素数量为 0
			_disposed = false;                                  // 初始未释放
		}

		/// <summary>当前队列元素数量。</summary>
		public int Count => _count;                             // 直接返回 _count

		/// <summary>队列是否为空。</summary>
		public bool IsEmpty => _count == 0;                     // 空：count==0

		/// <summary>
		/// 入队（覆盖写版本）：
		/// - 如果设置了 maxCapacity 且已满：先丢弃最老元素（逻辑覆盖），再写入新元素。
		/// - 如果没满/没上限：直接写入尾段；尾段满了就追加新段。
		/// </summary>
		/// <param name="item">要入队的元素（你的 struct Event）。</param>
		public void EnqueueOverwrite(in T item)                 // in：只读引用传参，减少大 struct 拷贝
		{
			ThrowIfDisposed();                                   // 已释放则抛异常，避免池数组泄漏/错乱

			// 如果启用了最大容量（>0），且队列已满：执行“覆盖写语义”= 丢弃最老元素再入队
			if (_maxCapacity > 0 && _count >= _maxCapacity)     // 满判断：count>=maxCapacity 表示已经到上限
			{
				// 丢弃 1 个最老元素：
				// 这在语义上等价于“覆盖写最老数据”，只不过我们用移动 head 来实现，而不是直接写到 head 的位置。
				DropOldest(1);                                   // DropOldest：推进 head 指针并在必要时回收空段
			}

			EnsureTailSegmentHasSpace();                          // 确保尾段存在且有可写空间（尾段满了就追加新段）

			_tailSeg!.Buffer[_tailIndex] = item;                  // 写入：把 item 放到尾段数组的 tailIndex 位置
			_tailIndex++;                                         // 尾写索引前进：下一次写入去下一个槽位
			_count++;                                             // 元素数量 +1
		}

		/// <summary>
		/// 尝试出队（FIFO）：
		/// - 成功：返回 true，并把最老元素写入 out item。
		/// - 失败（队列空）：返回 false，item=default。
		/// </summary>
		/// <param name="item">出队得到的元素。</param>
		/// <returns>是否成功出队。</returns>
		public bool TryDequeue(out T item)                       // out：把结果写回调用者变量
		{
			ThrowIfDisposed();                                   // 防止释放后使用

			if (_count == 0)                                     // 若为空：出队失败
			{
				item = default;                                  // 输出 default（struct 默认值）
				return false;                                    // 返回 false
			}

			// 头段一定存在：因为 _count>0 说明至少有一条数据
			var seg = _headSeg!;                                 // 取出头段引用（! 表示我们确信不为 null）

			item = seg.Buffer[_headIndex];                       // 读取：从头段数组的 headIndex 取最老元素
			if (_clearOnDequeue)                                 // 若启用出队清槽位
				seg.Buffer[_headIndex] = default;                // 清槽位：避免引用字段被池数组“保留”

			_headIndex++;                                        // 头读索引前进：下一次出队读下一个槽位
			_count--;                                            // 元素数量 -1

			// 如果头段已读空（读索引到达 chunkSize），则回收头段数组，并移动到下一段
			if (_headIndex >= _chunkSize)                        // 判断：头段所有槽位都已经被读走
			{
				AdvanceHeadSegment();                             // 回收旧头段（Return），切到 next 段
			}

			// 如果队列变空：重置所有状态，使下一次入队从“空队列初始态”开始
			if (_count == 0)                                     // 读完最后一个元素后
			{
				// 注意：此时 _headSeg 可能已经被 AdvanceHeadSegment 回收为空，也可能还有尾段但无元素
				ResetToEmpty();                                   // 统一把 head/tail/索引清成空队列状态
			}

			return true;                                         // 成功出队
		}

		/// <summary>
		/// 查看队头（最老元素）但不出队。
		/// </summary>
		/// <param name="item">队头元素（若为空则 default）。</param>
		/// <returns>是否成功读取到元素。</returns>
		public bool TryPeek(out T item)
		{
			ThrowIfDisposed();                                   // 防止释放后使用

			if (_count == 0)                                     // 空队列：没有可看的队头
			{
				item = default;                                  // 输出 default
				return false;                                    // 失败
			}

			item = _headSeg!.Buffer[_headIndex];                 // 直接读取队头槽位（不移动索引）
			return true;                                         // 成功
		}

		/// <summary>
		/// 清空队列：不销毁内存；把所有段数组 Return 回池，状态回到“空队列”。
		/// </summary>
		public void Clear()
		{
			ThrowIfDisposed();                                   // 防止释放后使用

			// 从头段开始逐段回收
			var cur = _headSeg;                                  // cur：当前段指针
			while (cur != null)                                  // 遍历所有段直到 null
			{
				var next = cur.Next;                             // 先缓存 next：因为我们马上要回收 cur
				_pool.Return(cur.Buffer, _clearOnReturn);         // Return 段数组回池（不销毁，进入复用循环）
				cur = next;                                      // 移动到下一段
			}

			ResetToEmpty();                                      // 重置队列为空
		}

		/// <summary>
		/// 预热/预分配容量（可选）：
		/// - 目的：提前租好足够多的段，避免运行时第一次爆发时频繁 Rent。
		/// - 注意：这不是“扩容复制”，而是“追加段”；旧段不会被销毁，仍在池生命周期里循环。
		/// </summary>
		/// <param name="capacity">
		/// 希望至少能容纳多少个元素。
		/// 如果设置了 maxCapacity，则会按 min(capacity, maxCapacity) 进行预热。
		/// </param>
		public void Prewarm(int capacity)
		{
			ThrowIfDisposed();                                   // 防止释放后使用

			if (capacity <= 0)                                   // 预热容量 <=0：无意义，直接返回
				return;

			if (_maxCapacity > 0 && capacity > _maxCapacity)     // 若有上限，则预热不要超过上限
				capacity = _maxCapacity;

			// 计算需要多少段：ceil(capacity / chunkSize)
			int neededSegments = (capacity + _chunkSize - 1) / _chunkSize; // 向上取整：不足一段也要算一段

			// 当前已有多少段（按链表数）
			int existingSegments = CountSegments();              // 统计当前段数量

			int toAdd = neededSegments - existingSegments;       // 还需要追加多少段
			for (int i = 0; i < toAdd; i++)                      // 追加 toAdd 段
			{
				AppendNewTailSegment();                          // 追加一段到尾部（只租数组，不写入元素）
			}
		}

		/// <summary>
		/// Dispose：释放队列，把所有段数组 Return 回池。
		/// </summary>
		public void Dispose()
		{
			if (_disposed)                                       // 幂等（递归解释：幂等=调用多次效果相同）：重复 Dispose 直接返回
				return;

			_disposed = true;                                    // 标记已释放
			Clear();                                             // Clear 会 Return 所有段并重置状态
		}

		// =========================
		// 内部：段结构（链表）
		// =========================

		private sealed class Segment                              // Segment：表示“一段数组”以及链表指针
		{
			public T[] Buffer;                                   // Buffer：这一段的数组存储（来自 ArrayPool<T>.Rent）
			public Segment? Next;                                 // Next：下一段指针（单向链表足够）

			public Segment(T[] buffer)                            // 构造函数：把租来的数组包起来
			{
				Buffer = buffer;                                 // 保存数组引用
				Next = null;                                     // 初始 next 为空
			}
		}

		// =========================
		// 内部：辅助方法（保证尾段空间、回收头段、丢弃最老元素等）
		// =========================

		private void EnsureTailSegmentHasSpace()
		{
			// 情况 1：队列为空（还没有任何段）=> 创建第一段，head/tail 都指向它
			if (_tailSeg == null)                                // _tailSeg 为 null 说明没有段
			{
				var buffer = _pool.Rent(_chunkSize);             // 从池租一个数组段；长度可能 >= chunkSize（这由池策略决定）
				var seg = new Segment(buffer);                   // 创建段节点
				_headSeg = seg;                                  // head 指向该段
				_tailSeg = seg;                                  // tail 指向该段
				_headIndex = 0;                                  // 头读索引从 0 开始
				_tailIndex = 0;                                  // 尾写索引从 0 开始
				return;                                          // 已保证可写
			}

			// 情况 2：尾段写满了（tailIndex 到达 chunkSize）=> 追加新段作为新的尾段
			if (_tailIndex >= _chunkSize)                         // 尾段已写满
			{
				AppendNewTailSegment();                           // 追加新段（不复制旧数据）
			}
		}

		private void AppendNewTailSegment()
		{
			// 追加新段：只租数组并挂到链表尾部
			var buffer = _pool.Rent(_chunkSize);                  // 从池租新数组段
			var newSeg = new Segment(buffer);                     // 包装成段节点

			_tailSeg!.Next = newSeg;                              // 把旧尾段的 Next 指向新段
			_tailSeg = newSeg;                                    // 更新尾段指针到新段
			_tailIndex = 0;                                       // 新尾段的写索引从 0 开始
																  // 注意：_headSeg / _headIndex 不变，因为读端不受影响
		}

		private void AdvanceHeadSegment()
		{
			// 回收头段：当头段已经被完全读空（headIndex>=chunkSize）时调用
			var oldHead = _headSeg!;                               // oldHead：旧头段

			_headSeg = oldHead.Next;                               // head 指向下一段（可能为 null）
			_pool.Return(oldHead.Buffer, _clearOnReturn);           // Return 旧头段数组回池（不销毁，等待复用）
			_headIndex = 0;                                        // 新头段读索引从 0 开始

			// 如果头段被回收后发现没有段了：说明队列彻底空（但调用方通常会在 _count==0 时 Reset）
			if (_headSeg == null)                                  // 链表无段
			{
				_tailSeg = null;                                   // 尾段也置空
				_tailIndex = 0;                                    // 尾写索引复位
			}
		}

		private void DropOldest(int n)
		{
			// 丢弃最老 n 个元素：用于覆盖写语义（满了就丢弃老的）
			// 这里用“推进 head 指针”的方式丢弃，不需要移动/复制任何数据。

			if (n <= 0)                                            // n<=0：无事发生
				return;

			if (_count == 0)                                       // 队列空：无可丢弃
				return;

			if (n > _count)                                        // 丢弃数量不能超过现有数量
				n = _count;

			for (int i = 0; i < n; i++)                            // 循环丢弃 n 次（你也可以做批量优化，但先保证正确）
			{
				// 逻辑上“出队但不返回值”：这就是丢弃最老元素
				// 这里不调用 TryDequeue 是为了少一次 out 写入，但逻辑等价。
				var seg = _headSeg!;                               // 当前头段
				if (_clearOnDequeue)                               // 如果要求清槽位
					seg.Buffer[_headIndex] = default;              // 清掉被丢弃的槽位（避免引用字段被保留）

				_headIndex++;                                      // 头读索引前进一格（丢弃一个元素）
				_count--;                                          // 数量 -1

				if (_headIndex >= _chunkSize)                      // 如果当前头段被丢弃/读到段末尾
				{
					AdvanceHeadSegment();                           // 回收该段并切到下一段
					if (_count == 0)                               // 如果丢弃后变空
					{
						ResetToEmpty();                            // 重置状态
						break;                                     // 退出循环
					}
				}
			}
		}

		private void ResetToEmpty()
		{
			// 把状态重置到“空队列”：
			// 注意：这里不 Return 段数组；Return 在 Clear / AdvanceHeadSegment 中做。
			_headSeg = null;                                       // 头段置空
			_tailSeg = null;                                       // 尾段置空
			_headIndex = 0;                                        // 读索引复位
			_tailIndex = 0;                                        // 写索引复位
			_count = 0;                                            // 数量复位
		}

		private int CountSegments()
		{
			// 统计链表中当前段数量：用于 Prewarm 的计算
			int count = 0;                                         // 段计数器
			var cur = _headSeg;                                    // 从头段开始遍历
			while (cur != null)                                    // 遍历直到末尾
			{
				count++;                                           // 段数量 +1
				cur = cur.Next;                                    // 前进到下一段
			}
			return count;                                          // 返回段数量
		}

		private void ThrowIfDisposed()
		{
			if (_disposed)                                         // 已 Dispose：不允许继续使用
				throw new ObjectDisposedException(nameof(PooledChunkedOverwriteQueue<T>)); // 抛异常提示逻辑错误
		}
	}

	// =========================
	// 示例：struct Event + 用法
	// =========================

	public struct GameEvent                                       // 示例事件结构体（你换成自己的 Event struct）
	{
		public int Type;                                          // Type：事件类型（示例）
		public int A;                                             // A：参数（示例）
		public int B;                                             // B：参数（示例）
	}

	public static class Demo
	{
		public static void Example()
		{
			// 创建分段队列：
			// - chunkSize=256：每段能装 256 个事件
			// - maxCapacity=1024：总容量上限 1024；满了就丢弃最老事件（覆盖写语义）
			// - clearOnDequeue/clearOnReturn=false：纯值类型事件建议关掉以追求性能
			using var q = new PooledChunkedOverwriteQueue<GameEvent>(
				chunkSize: 256,                                   // 每段容量
				maxCapacity: 1024,                                // 总容量上限（启用覆盖写）
				pool: ArrayPool<GameEvent>.Shared,                // 使用共享池
				clearOnDequeue: false,                            // 出队不清槽位（纯值类型更快）
				clearOnReturn: false                              // Return 不清数组（纯值类型更快）
			);

			q.Prewarm(1024);                                      // 可选：预热到 1024 容量，减少运行时 Rent

			q.EnqueueOverwrite(new GameEvent { Type = 1, A = 10, B = 20 }); // 入队：写入新事件
			q.EnqueueOverwrite(new GameEvent { Type = 2, A = 11, B = 21 }); // 入队：再写入一个

			if (q.TryDequeue(out var evt))                        // 出队：按 FIFO 取出最老事件
			{
				_ = evt.Type;                                     // 使用一下 evt，避免示例警告
			}
		}
	}
}