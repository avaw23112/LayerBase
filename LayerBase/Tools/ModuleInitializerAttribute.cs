#if !NET5_0_OR_GREATER

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    internal sealed class ModuleInitializerAttribute : Attribute
    {
    }
}

#endif
