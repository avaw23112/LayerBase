using Arch.Core;

namespace LayerBase.ECS.Runtime;

internal sealed class DelegateEcsWorkItem : IEcsWorkItem
{
    private readonly Action<World> _execute;

    public DelegateEcsWorkItem(string debugName, Action<World> execute)
    {
        DebugName = debugName;
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
    }

    public string DebugName { get; }

    public void Execute(World world, EcsResultQueue results)
    {
        _execute(world);
    }
}
