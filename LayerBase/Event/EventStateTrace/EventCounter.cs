using System;
using System.Collections.Generic;
using LayerBase.Core.EventCatalogue;

namespace LayerBase.Core.EventStateTrace;

internal sealed class EventCounter
{
    private readonly Dictionary<EventCategoryToken, int> _countByCategory = new();

    public int Increment(in EventCategoryToken category)
    {
        if (category.IsEmpty)
        {
            return -1;
        }

        int current = 0;
        _countByCategory.TryGetValue(category, out current);
        current += 1;
        _countByCategory[category] = current;
        return current;
    }

    public int Decrement(in EventCategoryToken category)
    {
        if (category.IsEmpty)
        {
            return -1;
        }

        if (!_countByCategory.TryGetValue(category, out var current) || current <= 0)
        {
            throw new InvalidOperationException("Event count mismatch; decrement failed.");
        }

        current -= 1;
        if (current == 0)
        {
            _countByCategory.Remove(category);
            return 0;
        }

        _countByCategory[category] = current;
        return current;
    }
}

