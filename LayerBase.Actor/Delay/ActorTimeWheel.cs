namespace LayerBase.Actor;

internal readonly struct ActorTimeWheelOptions
{
    public static ActorTimeWheelOptions Default => new(16);

    public ActorTimeWheelOptions(int initialCapacity)
    {
        InitialCapacity = Math.Max(initialCapacity, 1);
    }

    public int InitialCapacity { get; }
}

internal sealed class ActorTimeWheel
{
    private sealed class Entry
    {
        public int Id;
        public float RemainingSeconds;
        public IActorDelayTask? Task;
    }

    private readonly List<Entry> _entries;
    private int _nextId = 1;

    public ActorTimeWheel(ActorTimeWheelOptions options)
    {
        _entries = new List<Entry>(options.InitialCapacity);
    }

    public bool HasPending => _entries.Count > 0;

    public DelayPostHandle Schedule(ActorDelayScheduler scheduler, IActorDelayTask task, float delaySeconds)
    {
        var entry = new Entry
        {
            Id = _nextId++,
            RemainingSeconds = Math.Max(0f, delaySeconds),
            Task = task
        };

        _entries.Add(entry);
        return new DelayPostHandle(scheduler, entry.Id);
    }

    public void Tick(float deltaTime)
    {
        float elapsed = Math.Max(0f, deltaTime);
        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            Entry entry = _entries[i];
            entry.RemainingSeconds -= elapsed;
            if (entry.RemainingSeconds > 0f)
            {
                continue;
            }

            _entries.RemoveAt(i);
            IActorDelayTask? task = entry.Task;
            entry.Task = null;
            task?.Execute();
        }
    }

    public void Cancel(int taskId)
    {
        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            Entry entry = _entries[i];
            if (entry.Id != taskId)
            {
                continue;
            }

            _entries.RemoveAt(i);
            IActorDelayTask? task = entry.Task;
            entry.Task = null;
            task?.Cancel();
            return;
        }
    }

    public void Clear()
    {
        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            IActorDelayTask? task = _entries[i].Task;
            _entries[i].Task = null;
            task?.Cancel();
        }

        _entries.Clear();
    }
}
