using LayerBase.Layers;
using LayerBase.Modules;
using LayerBase.Scope;

namespace Demo.Foundation;

[AssemblyModule]
public sealed partial class FoundationModule { }

public sealed partial class GameplayLayer : Layer { }

[ScopeOptions(
    threading: ScopeThreadingMode.Inline,
    clock: ScopeClockMode.EngineDriven)]
public sealed class CombatScope { }
