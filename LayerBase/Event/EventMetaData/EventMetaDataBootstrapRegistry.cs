using System;
using System.Collections.Generic;

namespace LayerBase.Event.EventMetaData;

public static class EventMetaDataBootstrapRegistry
{
    private static readonly object s_lock = new();
    private static readonly List<Action> s_replays = new();

    public static void Register(Action replay)
    {
        if (replay == null)
            throw new ArgumentNullException(nameof(replay));

        lock (s_lock)
        {
            s_replays.Add(replay);
        }
    }

    internal static void ReplayAll()
    {
        Action[] snapshot;

        lock (s_lock)
        {
            snapshot = s_replays.ToArray();
        }

        for (int i = 0; i < snapshot.Length; i++)
        {
            snapshot[i]();
        }
    }
}
