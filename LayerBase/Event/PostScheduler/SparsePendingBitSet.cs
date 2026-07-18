using System.Runtime.CompilerServices;
using LayerBase.Core.DataStruct;

namespace LayerBase.Core.Event;

internal sealed class SparsePendingBitSet
{
    private ulong[] _pendingBits = Array.Empty<ulong>();
    private int[] _pendingWords = Array.Empty<int>();
    private byte[] _pendingWordFlags = Array.Empty<byte>();
    private int _pendingWordCount;

    private ulong[] _snapshotBits = Array.Empty<ulong>();
    private int[] _snapshotWords = Array.Empty<int>();
    private byte[] _snapshotWordFlags = Array.Empty<byte>();
    private int _snapshotWordCount;

    public bool HasPending => _pendingWordCount != 0;

    public int PendingWordCount => _pendingWordCount;

    public int SnapshotWordCount => _snapshotWordCount;

    public ReadOnlySpan<int> PendingWords =>
        _pendingWords.AsSpan(0, _pendingWordCount);

    public ReadOnlySpan<int> SnapshotWords =>
        _snapshotWords.AsSpan(0, _snapshotWordCount);

    public void EnsureBitCapacity(int bitCapacity)
    {
        if (bitCapacity < 0)
            throw new ArgumentOutOfRangeException(nameof(bitCapacity));

        int requiredWordCount = (bitCapacity + 63) >> 6;

        if (_pendingBits.Length >= requiredWordCount)
            return;

        int newLength = BitHelper.NextPowerOfTwo(
            Math.Max(requiredWordCount, 1));

        Array.Resize(ref _pendingBits, newLength);
        Array.Resize(ref _snapshotBits, newLength);
        Array.Resize(ref _pendingWordFlags, newLength);
        Array.Resize(ref _snapshotWordFlags, newLength);
        Array.Resize(ref _pendingWords, newLength);
        Array.Resize(ref _snapshotWords, newLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Set(int bitIndex)
    {
        if (bitIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(bitIndex));

        int wordIndex = bitIndex >> 6;

        if ((uint)wordIndex >= (uint)_pendingBits.Length)
            throw new InvalidOperationException(
                "SparsePendingBitSet capacity was not prebuilt.");

        ulong bit = 1UL << (bitIndex & 63);
        ref ulong word = ref _pendingBits[wordIndex];

        if ((word & bit) != 0)
            return false;

        if (word == 0)
        {
            if (_pendingWordFlags[wordIndex] != 0)
                throw new InvalidOperationException(
                    "Pending word flag is inconsistent.");

            _pendingWordFlags[wordIndex] = 1;
            _pendingWords[_pendingWordCount++] = wordIndex;
        }

        word |= bit;
        return true;
    }

    public void TakeSnapshot()
    {
        if (_snapshotWordCount != 0)
            throw new InvalidOperationException(
                "Previous sparse bitset snapshot was not cleared.");

        (_pendingBits, _snapshotBits) =
            (_snapshotBits, _pendingBits);

        (_pendingWords, _snapshotWords) =
            (_snapshotWords, _pendingWords);

        (_pendingWordFlags, _snapshotWordFlags) =
            (_snapshotWordFlags, _pendingWordFlags);

        _snapshotWordCount = _pendingWordCount;
        _pendingWordCount = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong GetSnapshotBits(int wordIndex)
    {
        return _snapshotBits[wordIndex];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong GetPendingBits(int wordIndex)
    {
        return _pendingBits[wordIndex];
    }

    public void ClearSnapshotWord(int wordIndex)
    {
        _snapshotBits[wordIndex] = 0;
        _snapshotWordFlags[wordIndex] = 0;
    }

    public void UpdateSnapshotBits(int wordIndex, ulong bits)
    {
        _snapshotBits[wordIndex] = bits;
    }

    public void ClearSnapshot()
    {
        for (int i = 0; i < _snapshotWordCount; i++)
        {
            int wordIndex = _snapshotWords[i];
            _snapshotBits[wordIndex] = 0;
            _snapshotWordFlags[wordIndex] = 0;
        }

        _snapshotWordCount = 0;
    }

    public void ClearPending()
    {
        for (int i = 0; i < _pendingWordCount; i++)
        {
            int wordIndex = _pendingWords[i];
            _pendingBits[wordIndex] = 0;
            _pendingWordFlags[wordIndex] = 0;
        }

        _pendingWordCount = 0;
    }
}
