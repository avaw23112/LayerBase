using System.Collections.Concurrent;
using System.Threading;

namespace LayerBase.Core.Event;

/// <summary>
/// PostIngressQueue 一次 Drain 的结果。
/// </summary>
internal readonly struct PostIngressDrainResult
{
    /// <summary>
    /// drained:
    ///   本次从跨线程入口队列取出的事件数量。
    ///
    /// failed:
    ///   本次搬运后调用 PostScheduler.TryPost 失败的数量。
    /// </summary>
    public PostIngressDrainResult(int drained, int failed)
    {
        Drained = drained;
        Failed = failed;
    }

    /// <summary>
    /// 本次实际取出的事件数量。
    /// </summary>
    public int Drained { get; }

    /// <summary>
    /// 本次投递失败的事件数量。
    /// </summary>
    public int Failed { get; }
}

/// <summary>
/// 跨线程 Post 入口队列。
///
/// 作用：
/// 允许任意线程提交事件，但不让外部线程直接修改 PostScheduler 内部队列。
///
/// 注意：
/// 这是跨线程慢路径。
/// 主线程内的 LayerHub.Post / Runtime.Post / Runtime.TryPost 不经过这里。
/// </summary>
internal sealed class PostIngressQueue
{
    private int _capacity;
    private int _count;

    public PostIngressQueue(int capacity = 65536)
    {
        _capacity = capacity <= 0 ? 65536 : capacity;
    }

    /// <summary>
    /// 跨线程入口队列。
    ///
    /// ConcurrentQueue：
    /// .NET 提供的线程安全队列。
    /// 这里允许多个线程同时 Enqueue。
    /// Runtime.Pump 是唯一消费者。
    /// </summary>
    private readonly ConcurrentQueue<IIngressPostItem> _queue = new();

    /// <summary>
    /// 从任意线程提交一个事件。
    /// </summary>
    /// <typeparam name="T">
    /// 事件类型。
    /// 必须是 struct，以保持和 LayerBase 当前事件系统一致。
    /// </typeparam>
    /// <param name="value">
    /// 事件数据。
    /// 这里会复制一份到 IngressPostItem 中，避免保存外部可变引用。
    /// </param>
    /// <param name="policy">
    /// 可选 Post 策略。
    /// null 表示最终进入 PostScheduler 后使用默认策略。
    /// </param>
    public bool Enqueue<T>(in T value, EventPostPolicy? policy)
        where T : struct
    {
        while (true)
        {
            int current = Volatile.Read(ref _count);
            int capacity = Volatile.Read(ref _capacity);
            if (current >= capacity)
            {
                return false;
            }

            if (Interlocked.CompareExchange(ref _count, current + 1, current) == current)
            {
                break;
            }
        }

        _queue.Enqueue(new IngressPostItem<T>(value, policy));
        return true;
    }

    public void SetCapacity(int capacity)
    {
        Volatile.Write(ref _capacity, capacity <= 0 ? 65536 : capacity);
    }

    /// <summary>
    /// 把跨线程入口队列中的事件搬运到 PostScheduler。
    /// </summary>
    /// <param name="scheduler">
    /// 当前 Runtime 的 PostScheduler。
    /// 所有事件最终都通过它进入原有 Post 管线。
    /// </param>
    /// <param name="maxCount">
    /// 本次最多搬运多少个事件。
    /// 小于等于 0 表示不限制。
    /// </param>
    /// <returns>
    /// 本次 Drain 的结果，包含搬运数量和失败数量。
    /// </returns>
    public PostIngressDrainResult DrainTo(PostScheduler scheduler, int maxCount = 0)
    {
        if (scheduler == null)
        {
            throw new ArgumentNullException(nameof(scheduler));
        }

        var drained = 0;
        var failed = 0;

        while ((maxCount <= 0 || drained < maxCount) &&
               _queue.TryDequeue(out var item))
        {
            Interlocked.Decrement(ref _count);
            var result = item.PostTo(scheduler);
            if (!result.IsSuccess)
            {
                failed++;
            }

            drained++;
        }

        return new PostIngressDrainResult(drained, failed);
    }

    /// <summary>
    /// 清空入口队列。
    /// Runtime Dispose 或 Reset 时调用。
    /// </summary>
    public void Clear()
    {
        while (_queue.TryDequeue(out _))
        {
            Interlocked.Decrement(ref _count);
        }

        if (Volatile.Read(ref _count) < 0)
        {
            Interlocked.Exchange(ref _count, 0);
        }
    }
}

/// <summary>
/// 跨线程 Post 项的非泛型接口。
///
/// 作用：
/// PostIngressQueue 需要保存不同事件类型的投递项，
/// 所以用非泛型接口统一存储。
/// </summary>
internal interface IIngressPostItem
{
    /// <summary>
    /// 把事件重新投递到 PostScheduler。
    /// </summary>
    /// <param name="scheduler">
    /// 当前 Runtime 的 PostScheduler。
    /// </param>
    /// <returns>
    /// PostScheduler.TryPost 的结果。
    /// </returns>
    PostResult PostTo(PostScheduler scheduler);
}

/// <summary>
/// 泛型跨线程 Post 项。
/// </summary>
/// <typeparam name="T">
/// 事件类型。
/// </typeparam>
internal sealed class IngressPostItem<T> : IIngressPostItem
    where T : struct
{
    /// <summary>
    /// 事件数据的副本。
    /// </summary>
    private readonly T _value;

    /// <summary>
    /// 可选 Post 策略。
    /// null 表示使用事件默认策略。
    /// </summary>
    private readonly EventPostPolicy? _policy;

    /// <summary>
    /// 创建跨线程 Post 项。
    /// </summary>
    /// <param name="value">
    /// 事件数据。
    /// 构造时复制，避免跨线程持有外部引用。
    /// </param>
    /// <param name="policy">
    /// 可选 Post 策略。
    /// null 表示使用事件默认策略。
    /// </param>
    public IngressPostItem(T value, EventPostPolicy? policy)
    {
        _value = value;
        _policy = policy;
    }

    /// <summary>
    /// 在 Runtime.Pump 中重新进入原有 PostScheduler 管线。
    /// </summary>
    /// <param name="scheduler">
    /// 当前 Runtime 的 PostScheduler。
    /// </param>
    /// <returns>
    /// PostScheduler.TryPost 的结果。
    /// </returns>
    public PostResult PostTo(PostScheduler scheduler)
    {
        return scheduler.TryPost(_value, _policy);
    }
}
