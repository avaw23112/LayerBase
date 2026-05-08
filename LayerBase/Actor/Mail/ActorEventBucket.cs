namespace LayerBase.Actor;

internal sealed class ActorEventBucket<TEvent> : IActorEventBucket
    where TEvent : struct
{
    private IActorEventColumn<TEvent>[] _columns = Array.Empty<IActorEventColumn<TEvent>>();
    private int _cursor;

    public void AddColumn(IActorEventColumn<TEvent> column)
    {
        int oldLength = _columns.Length;
        Array.Resize(ref _columns, oldLength + 1);
        _columns[oldLength] = column;
    }

    public bool PumpOne(ref RuntimeFrameBudget budget)
    {
        if (_columns.Length == 0)
        {
            return false;
        }

        int checkedCount = 0;
        while (checkedCount < _columns.Length)
        {
            int index = _cursor;
            _cursor = index + 1 == _columns.Length ? 0 : index + 1;
            checkedCount++;

            if (_columns[index].PumpOne(ref budget))
            {
                return true;
            }
        }

        return false;
    }
}
