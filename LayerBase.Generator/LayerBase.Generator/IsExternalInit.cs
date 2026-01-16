namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Shim for init-only setters support on targets that don't define IsExternalInit (e.g., netstandard2.0).
    /// </summary>
    internal static class IsExternalInit
    {
    }
}
