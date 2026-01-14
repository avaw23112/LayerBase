using System;

namespace LayerBase.Core.EventStateTrace;

/// <summary>
/// 固定长度的日志队列，满时覆盖最旧元素。
/// </summary>
public sealed class EventTraceLogQueue
{
    private readonly string[] _buffer;
    private int _head;
    private int _count;
    private readonly object _lock = new();

    public EventTraceLogQueue(int capacity = 256)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _buffer = new string[capacity];
        _head = 0;
        _count = 0;
    }

    public int Capacity => _buffer.Length;
    public int Count
    {
        get
        {
            lock (_lock) { return _count; }
        }
    }

    public void EnqueueOverwrite(string value)
    {
        if (value == null) value = string.Empty;

        lock (_lock)
        {
            int tail = (_head + _count) % _buffer.Length;
            _buffer[tail] = value;
            if (_count == _buffer.Length)
            {
                _head = (_head + 1) % _buffer.Length;
            }
            else
            {
                _count++;
            }
        }
    }

    public bool TryDequeue(out string value)
    {
        lock (_lock)
        {
            if (_count == 0)
            {
                value = string.Empty;
                return false;
            }

            value = _buffer[_head];
            _buffer[_head] = string.Empty;
            _head = (_head + 1) % _buffer.Length;
            _count--;
            return true;
        }
    }

    public string[] ToArray()
    {
        lock (_lock)
        {
            var result = new string[_count];
            for (int i = 0; i < _count; i++)
            {
                int idx = (_head + i) % _buffer.Length;
                result[i] = _buffer[idx];
            }
            return result;
        }
    }
}
