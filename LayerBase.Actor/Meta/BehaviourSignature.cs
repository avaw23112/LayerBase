namespace LayerBase.Actor;

internal readonly struct BehaviourMask : IEquatable<BehaviourMask>
{
    private readonly ulong[] _words;

    public BehaviourMask(ulong[] words)
    {
        if (words == null)
        {
            throw new ArgumentNullException(nameof(words));
        }

        _words = TrimTrailingZeroWords(words);
    }

    public ReadOnlySpan<ulong> Words => _words;

    public static BehaviourMask FromSortedEventIds(ReadOnlySpan<int> eventTypeIds)
    {
        if (eventTypeIds.Length == 0)
        {
            return new BehaviourMask(Array.Empty<ulong>());
        }

        int maxEventTypeId = eventTypeIds[eventTypeIds.Length - 1];
        if (maxEventTypeId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(eventTypeIds), "EventTypeId must be non-negative.");
        }

        ulong[] words = new ulong[maxEventTypeId / 64 + 1];
        foreach (int eventTypeId in eventTypeIds)
        {
            if (eventTypeId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(eventTypeIds), "EventTypeId must be non-negative.");
            }

            int wordIndex = eventTypeId / 64;
            int bitIndex = eventTypeId % 64;
            words[wordIndex] |= 1UL << bitIndex;
        }

        return new BehaviourMask(words);
    }

    public bool ContainsAll(BehaviourMask query)
    {
        ReadOnlySpan<ulong> selfWords = _words;
        ReadOnlySpan<ulong> queryWords = query._words;

        for (int i = 0; i < queryWords.Length; i++)
        {
            ulong queryWord = queryWords[i];
            ulong selfWord = i < selfWords.Length ? selfWords[i] : 0UL;

            if ((selfWord & queryWord) != queryWord)
            {
                return false;
            }
        }

        return true;
    }

    public bool ContainsAny(BehaviourMask query)
    {
        ReadOnlySpan<ulong> selfWords = _words;
        ReadOnlySpan<ulong> queryWords = query._words;
        int length = Math.Min(selfWords.Length, queryWords.Length);

        for (int i = 0; i < length; i++)
        {
            if ((selfWords[i] & queryWords[i]) != 0UL)
            {
                return true;
            }
        }

        return false;
    }

    public bool Equals(BehaviourMask other)
    {
        return _words.AsSpan().SequenceEqual(other._words);
    }

    public override bool Equals(object? obj)
    {
        return obj is BehaviourMask other && Equals(other);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (ulong word in _words)
        {
            hash.Add(word);
        }

        return hash.ToHashCode();
    }

    private static ulong[] TrimTrailingZeroWords(ulong[] words)
    {
        int length = words.Length;
        while (length > 0 && words[length - 1] == 0UL)
        {
            length--;
        }

        if (length == words.Length)
        {
            return words;
        }

        if (length == 0)
        {
            return Array.Empty<ulong>();
        }

        ulong[] trimmed = new ulong[length];
        Array.Copy(words, trimmed, length);
        return trimmed;
    }
}

internal readonly struct BehaviourSignature : IEquatable<BehaviourSignature>
{
    private readonly int[] _eventTypeIds;

    public BehaviourMask Mask { get; }
    public static BehaviourSignature Empty => new(Array.Empty<int>());

    public BehaviourSignature(int[] eventTypeIds)
    {
        if (eventTypeIds == null)
        {
            throw new ArgumentNullException(nameof(eventTypeIds));
        }

        if (eventTypeIds.Length > 1)
        {
            for (int i = 1; i < eventTypeIds.Length; i++)
            {
                if (eventTypeIds[i] <= eventTypeIds[i - 1])
                {
                    throw new ArgumentException("Event type ids must be sorted and unique.", nameof(eventTypeIds));
                }
            }
        }

        _eventTypeIds = eventTypeIds.Length == 0 ? Array.Empty<int>() : (int[])eventTypeIds.Clone();
        Mask = BehaviourMask.FromSortedEventIds(_eventTypeIds);
    }

    public ReadOnlySpan<int> EventTypeIds => _eventTypeIds;

    public bool ContainsAll(BehaviourSignature query)
    {
        return Mask.ContainsAll(query.Mask);
    }

    public bool ContainsAny(BehaviourSignature query)
    {
        return Mask.ContainsAny(query.Mask);
    }

    public bool Equals(BehaviourSignature other)
    {
        return Mask.Equals(other.Mask);
    }

    public override bool Equals(object? obj)
    {
        return obj is BehaviourSignature other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Mask.GetHashCode();
    }
}
