using System;
using System.Buffers;
using System.Threading;

namespace LayerBase.Core.EventStateTrace;

/// <summary>
/// 路径数据池化，避免每个事件都分配独立数组。
/// </summary>
internal sealed class EventPathPool
{
    private readonly ArrayPool<PathFrame> _framePool;
    private readonly ArrayPool<HandlerVisit> _handlerPool;
    private readonly bool _clearOnReturn;
    private int _versionSeed;

    public EventPathPool(bool clearOnReturn = true)
    {
        _framePool = ArrayPool<PathFrame>.Shared;
        _handlerPool = ArrayPool<HandlerVisit>.Shared;
        _clearOnReturn = clearOnReturn;
    }

    public EventPath Rent(int frameCapacity = 4, int handlerCapacity = 8)
    {
        frameCapacity = Math.Max(frameCapacity, 1);
        handlerCapacity = Math.Max(handlerCapacity, 1);

        return new EventPath
        {
            Frames = _framePool.Rent(frameCapacity),
            Handlers = _handlerPool.Rent(handlerCapacity),
            FrameCount = 0,
            HandlerCount = 0,
            Version = NextVersion()
        };
    }

    public PathFrame[] GrowFrames(PathFrame[] oldFrames, int requiredCount)
    {
        int newSize = Math.Max(requiredCount, oldFrames.Length * 2);
        var newArr = _framePool.Rent(newSize);
        Array.Copy(oldFrames, newArr, oldFrames.Length);
        _framePool.Return(oldFrames, _clearOnReturn);
        return newArr;
    }

    public HandlerVisit[] GrowHandlers(HandlerVisit[] oldHandlers, int requiredCount)
    {
        int newSize = Math.Max(requiredCount, oldHandlers.Length * 2);
        var newArr = _handlerPool.Rent(newSize);
        Array.Copy(oldHandlers, newArr, oldHandlers.Length);
        _handlerPool.Return(oldHandlers, _clearOnReturn);
        return newArr;
    }

    public void Return(in EventPath path)
    {
        if (path.Frames != null)
        {
            _framePool.Return(path.Frames, _clearOnReturn);
        }

        if (path.Handlers != null)
        {
            _handlerPool.Return(path.Handlers, _clearOnReturn);
        }
    }

    private ushort NextVersion()
    {
        int next = Interlocked.Increment(ref _versionSeed);
        ushort v = (ushort)next;
        if (v == 0)
        {
            v = (ushort)Interlocked.Increment(ref _versionSeed);
        }
        return v;
    }
}
