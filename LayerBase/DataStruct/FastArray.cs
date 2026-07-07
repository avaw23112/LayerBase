using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LayerBase.Core.DataStruct;

/// <summary>
/// 高性能数组访问辅助类。在不做边界检查的前提下通过指针运算快速访问数组元素。
/// </summary>
internal static class FastArray
{
    /// <summary>以不安全方式获取数组元素的引用，跳过运行时边界检查。</summary>
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