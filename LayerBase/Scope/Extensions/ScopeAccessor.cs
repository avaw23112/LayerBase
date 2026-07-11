using System.Runtime.CompilerServices;
using LayerBase.DI;

namespace LayerBase.Scope.Extensions;

/// <summary>
/// 非泛型 Scope 访问器。Post 时目标 Scope 由消息的 [ScopeEvent&lt;TScope&gt;] 推导。
/// TryPost(Call) 需要低层级 targetScopeId，通常由 Generator 生成的重载调用。
/// </summary>
public readonly struct ScopeAccessor
{
    private readonly LayerRuntime _runtime;

    internal ScopeAccessor(LayerRuntime runtime)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }

    internal LayerRuntime Runtime => _runtime;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryPost(int targetScopeId, ScopePostMessage message)
    {
        return _runtime.ScopeHost?.TryPost(targetScopeId, message) ?? false;
    }
}

/// <summary>
/// 泛型 Scope 访问器。显式指定目标 Scope 类型，用于 awaitable Call 和 Post。
/// </summary>
public readonly struct ScopeAccessor<TScope>
{
    private readonly LayerRuntime _runtime;
    private readonly int _targetScopeId;

    internal ScopeAccessor(LayerRuntime runtime, int targetScopeId)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _targetScopeId = targetScopeId;
    }

    internal LayerRuntime Runtime => _runtime;
    internal int TargetScopeId => _targetScopeId;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryPost(ScopePostMessage message)
    {
        return _runtime.ScopeHost?.TryPost(_targetScopeId, message) ?? false;
    }
}
