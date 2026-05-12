#if NETCOREAPP || NET5_0_OR_GREATER
using System.Numerics;
#endif
using System.Runtime.CompilerServices;

namespace LayerBase.Core.DataStruct;

internal static class BitHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int TrailingZeroCount(ulong mask)
    {
#if NETCOREAPP || NET5_0_OR_GREATER
        return BitOperations.TrailingZeroCount(mask);
#else
        if (mask == 0) return 64;
        int count = 0;
        if ((mask & 0xFFFFFFFF) == 0)
        {
            mask >>= 32;
            count += 32;
        }

        if ((mask & 0xFFFF) == 0)
        {
            mask >>= 16;
            count += 16;
        }

        if ((mask & 0xFF) == 0)
        {
            mask >>= 8;
            count += 8;
        }

        if ((mask & 0xF) == 0)
        {
            mask >>= 4;
            count += 4;
        }

        if ((mask & 0x3) == 0)
        {
            mask >>= 2;
            count += 2;
        }

        if ((mask & 0x1) == 0)
        {
            count += 1;
        }

        return count;
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int NextPowerOfTwo(int value)
    {
        if (value <= 1) return 1;
        value--;
        value |= value >> 1;
        value |= value >> 2;
        value |= value >> 4;
        value |= value >> 8;
        value |= value >> 16;
        value++;
        return value;
    }
}