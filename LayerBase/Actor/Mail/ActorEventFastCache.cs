namespace LayerBase.Actor;

internal sealed class ActorEventFastCache<TEvent>
    where TEvent : struct
{
    private int[] _versions = Array.Empty<int>();
    private int[] _slotIndices = Array.Empty<int>();
    private int[] _generations = Array.Empty<int>();
    private EventMail<TEvent>[][] _mailArrays = Array.Empty<EventMail<TEvent>[]>();
    private DirtySlotList?[] _dirtySlotLists = Array.Empty<DirtySlotList?>();
    private int[] _bucketIndices = Array.Empty<int>();
    private ActorMailOptions[] _options = Array.Empty<ActorMailOptions>();
    private byte[] _states = Array.Empty<byte>();

    public void EnsureCapacity(int fastIndex)
    {
        if ((uint)fastIndex < (uint)_versions.Length)
        {
            return;
        }

        int newSize = _versions.Length == 0 ? 4 : _versions.Length;
        while (newSize <= fastIndex)
        {
            newSize <<= 1;
        }

        Array.Resize(ref _versions, newSize);
        Array.Resize(ref _slotIndices, newSize);
        Array.Resize(ref _generations, newSize);
        Array.Resize(ref _mailArrays, newSize);
        Array.Resize(ref _dirtySlotLists, newSize);
        Array.Resize(ref _bucketIndices, newSize);
        Array.Resize(ref _options, newSize);
        Array.Resize(ref _states, newSize);
    }

    public void Bind(
        int fastIndex,
        int version,
        int slotIndex,
        int generation,
        EventMail<TEvent>[] mailArray,
        DirtySlotList dirtySlots,
        int bucketIndex,
        ActorMailOptions options)
    {
        EnsureCapacity(fastIndex);

        _versions[fastIndex] = version;
        _slotIndices[fastIndex] = slotIndex;
        _generations[fastIndex] = generation;
        _mailArrays[fastIndex] = mailArray;
        _dirtySlotLists[fastIndex] = dirtySlots;
        _bucketIndices[fastIndex] = bucketIndex;
        _options[fastIndex] = options;
        _states[fastIndex] = 1;
    }

    public bool TryGet(
        int fastIndex,
        int version,
        int generation,
        out int slotIndex,
        out EventMail<TEvent>[] mailArray,
        out DirtySlotList dirtySlots,
        out int bucketIndex,
        out ActorMailOptions options)
    {
        if ((uint)fastIndex >= (uint)_states.Length
            || _states[fastIndex] == 0
            || _versions[fastIndex] != version
            || _generations[fastIndex] != generation)
        {
            slotIndex = -1;
            mailArray = null!;
            dirtySlots = null!;
            bucketIndex = -1;
            options = default;
            return false;
        }

        slotIndex = _slotIndices[fastIndex];
        mailArray = _mailArrays[fastIndex];
        dirtySlots = _dirtySlotLists[fastIndex]!;
        bucketIndex = _bucketIndices[fastIndex];
        options = _options[fastIndex];
        return true;
    }

    public void Invalidate(int fastIndex)
    {
        if ((uint)fastIndex >= (uint)_states.Length)
        {
            return;
        }

        _states[fastIndex] = 0;
        _mailArrays[fastIndex] = null!;
        _dirtySlotLists[fastIndex] = null;
        _bucketIndices[fastIndex] = -1;
        _options[fastIndex] = default;
    }

    public void InvalidateAll()
    {
        Array.Clear(_states, 0, _states.Length);
        Array.Clear(_mailArrays, 0, _mailArrays.Length);
        Array.Clear(_dirtySlotLists, 0, _dirtySlotLists.Length);
        Array.Fill(_bucketIndices, -1);
        Array.Clear(_options, 0, _options.Length);
    }
}
