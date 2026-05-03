using System.Runtime.CompilerServices;

namespace LayerBase.Core.Event;

public readonly struct PostTypePlan
{
    public readonly int EventTypeId;
    public readonly PostDeliveryMode Mode;
    public readonly BackpressurePolicy Backpressure;
    public readonly int MaxPending;
    public readonly BackpressurePolicy DefaultBackpressure;
    public readonly MergeFailurePolicy MergeFailure;

    public PostTypePlan(
        int eventTypeId,
        PostDeliveryMode mode,
        BackpressurePolicy backpressure,
        int maxPending,
        BackpressurePolicy defaultBackpressure,
        MergeFailurePolicy mergeFailure = MergeFailurePolicy.Reject)
    {
        EventTypeId = eventTypeId;
        Mode = mode;
        Backpressure = backpressure;
        MaxPending = maxPending;
        DefaultBackpressure = defaultBackpressure;
        MergeFailure = mergeFailure;
    }

    public bool TrackPending => MaxPending > 0;
    public bool HasCustomBackpressure => Backpressure != DefaultBackpressure;
}

public sealed class PostBitmap
{

    private ulong[] _specialMask = Array.Empty<ulong>();
    private ulong[] _dirtyMask = Array.Empty<ulong>();
    private ulong[] _latestMask = Array.Empty<ulong>();
    private ulong[] _coalescedMask = Array.Empty<ulong>();
    private ulong[] _trackPendingMask = Array.Empty<ulong>();

    public void Build(ReadOnlySpan<PostTypePlan> plans)
    {
        var maxEventTypeId = 0;
        for (var i = 0; i < plans.Length; i++)
        {
            if (plans[i].EventTypeId > maxEventTypeId)
                maxEventTypeId = plans[i].EventTypeId;
        }

        var segmentCount = (maxEventTypeId >> 6) + 1;

        _specialMask = new ulong[segmentCount];
        _dirtyMask = new ulong[segmentCount];
        _latestMask = new ulong[segmentCount];
        _coalescedMask = new ulong[segmentCount];
        _trackPendingMask = new ulong[segmentCount];

        for (var i = 0; i < plans.Length; i++)
        {
            var plan = plans[i];
            var typeId = plan.EventTypeId;
            var segment = typeId >> 6;
            var bit = 1UL << (typeId & 63);

            if (plan.Mode == PostDeliveryMode.DirtySignal)
                _dirtyMask[segment] |= bit;

            if (plan.Mode == PostDeliveryMode.Latest)
                _latestMask[segment] |= bit;

            if (plan.Mode == PostDeliveryMode.Coalesced)
                _coalescedMask[segment] |= bit;

            if (plan.TrackPending)
                _trackPendingMask[segment] |= bit;

            if (plan.Mode != PostDeliveryMode.Normal ||
                plan.TrackPending ||
                plan.HasCustomBackpressure)
            {
                _specialMask[segment] |= bit;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsSpecial(int eventTypeId)
    {
        var segment = eventTypeId >> 6;
        if ((uint)segment >= (uint)_specialMask.Length)
            return false;

        var bit = 1UL << (eventTypeId & 63);
        return (_specialMask[segment] & bit) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsDirty(int eventTypeId)
    {
        var segment = eventTypeId >> 6;
        if ((uint)segment >= (uint)_dirtyMask.Length)
            return false;

        var bit = 1UL << (eventTypeId & 63);
        return (_dirtyMask[segment] & bit) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsLatest(int eventTypeId)
    {
        var segment = eventTypeId >> 6;
        if ((uint)segment >= (uint)_latestMask.Length)
            return false;

        var bit = 1UL << (eventTypeId & 63);
        return (_latestMask[segment] & bit) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsCoalesced(int eventTypeId)
    {
        var segment = eventTypeId >> 6;
        if ((uint)segment >= (uint)_coalescedMask.Length)
            return false;

        var bit = 1UL << (eventTypeId & 63);
        return (_coalescedMask[segment] & bit) != 0;
    }
}
