using System;

namespace LayerBase.DI.Options;

/// <summary>
/// 声明被标记的上下文或事件处理器属于指定的 IService 域。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class OwnerServiceAttribute : Attribute
{
    public OwnerServiceAttribute(Type serviceType)
    {
        ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
    }

    public Type ServiceType { get; }
}