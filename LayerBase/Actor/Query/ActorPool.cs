namespace LayerBase.Actor;

internal sealed class ActorPool<TActor>
    where TActor : class, IActor
{
    private readonly Stack<TActor> _items = new();
    private int _maxRetained = 1024;
    private int _createdTotal;
    private int _rentTotal;
    private int _returnTotal;
    private int _droppedOnReturn;

    public TActor Rent()
    {
        TActor actor;
        if (_items.Count > 0)
        {
            actor = _items.Pop();
        }
        else
        {
            actor = Activator.CreateInstance<TActor>();
            _createdTotal++;
        }

        _rentTotal++;

        if (actor is IPooledActor pooled)
        {
            pooled.OnRent();
        }

        return actor;
    }

    public void Return(TActor actor)
    {
        if (actor == null)
        {
            return;
        }

        if (actor is IPooledActor pooled)
        {
            pooled.OnReturn();
        }

        _returnTotal++;

        if (_items.Count >= _maxRetained)
        {
            _droppedOnReturn++;
            return;
        }

        _items.Push(actor);
    }

    public void Prewarm(int count)
    {
        if (count <= 0)
        {
            return;
        }

        while (_items.Count < count && _items.Count < _maxRetained)
        {
            TActor actor = Activator.CreateInstance<TActor>();
            _createdTotal++;

            if (actor is IPooledActor pooled)
            {
                pooled.OnReturn();
            }

            _items.Push(actor);
        }
    }

    public void SetLimit(int maxCount)
    {
        _maxRetained = Math.Max(0, maxCount);

        while (_items.Count > _maxRetained)
        {
            _items.Pop();
            _droppedOnReturn++;
        }
    }

    public ActorPoolStats GetStats()
    {
        return new ActorPoolStats(
            _createdTotal,
            _rentTotal,
            _returnTotal,
            _items.Count,
            _droppedOnReturn,
            _maxRetained);
    }

    public void Clear()
    {
        _items.Clear();
    }
}