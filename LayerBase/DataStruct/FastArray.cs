using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LayerBase.Core.DataStruct;

internal static class FastArray
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref T At<T>(T[] array, int index)
    {
        Debug.Assert(array != null);
        Debug.Assert((uint)index < (uint)array.Length);

#if NETCOREAPP || NET5_0_OR_GREATER
        return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), index);
#else
        return ref array[index];
#endif
    }
}