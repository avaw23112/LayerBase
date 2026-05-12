namespace LayerBase.Actor;

internal static class ActorSignatureUtility
{
    public static int[] Normalize(int[]? ids)
    {
        if (ids == null || ids.Length == 0)
        {
            return Array.Empty<int>();
        }

        int[] copy = new int[ids.Length];
        Array.Copy(ids, copy, ids.Length);
        Array.Sort(copy);

        int uniqueCount = 0;
        for (int i = 0; i < copy.Length; i++)
        {
            if (i == 0 || copy[i] != copy[i - 1])
            {
                copy[uniqueCount++] = copy[i];
            }
        }

        if (uniqueCount != copy.Length)
        {
            Array.Resize(ref copy, uniqueCount);
        }

        return copy;
    }

    public static int[] Merge(ReadOnlySpan<int> left, ReadOnlySpan<int> right)
    {
        if (left.Length == 0)
        {
            return Normalize(right.ToArray());
        }

        if (right.Length == 0)
        {
            return Normalize(left.ToArray());
        }

        int[] merged = new int[left.Length + right.Length];
        left.CopyTo(merged);
        right.CopyTo(merged.AsSpan(left.Length));
        return Normalize(merged);
    }

    public static bool ContainsAll(ReadOnlySpan<int> source, ReadOnlySpan<int> query)
    {
        if (query.Length == 0)
        {
            return true;
        }

        int sourceIndex = 0;
        int queryIndex = 0;

        while (sourceIndex < source.Length && queryIndex < query.Length)
        {
            int sourceValue = source[sourceIndex];
            int queryValue = query[queryIndex];

            if (sourceValue == queryValue)
            {
                sourceIndex++;
                queryIndex++;
                continue;
            }

            if (sourceValue < queryValue)
            {
                sourceIndex++;
                continue;
            }

            return false;
        }

        return queryIndex == query.Length;
    }

    public static bool ContainsAny(ReadOnlySpan<int> source, ReadOnlySpan<int> query)
    {
        if (source.Length == 0 || query.Length == 0)
        {
            return false;
        }

        int sourceIndex = 0;
        int queryIndex = 0;

        while (sourceIndex < source.Length && queryIndex < query.Length)
        {
            int sourceValue = source[sourceIndex];
            int queryValue = query[queryIndex];

            if (sourceValue == queryValue)
            {
                return true;
            }

            if (sourceValue < queryValue)
            {
                sourceIndex++;
            }
            else
            {
                queryIndex++;
            }
        }

        return false;
    }
}