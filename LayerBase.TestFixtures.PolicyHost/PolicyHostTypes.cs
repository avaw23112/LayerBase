using LayerBase.Layers;
using LayerBase.Scope;

namespace LayerBase.TestFixtures.PolicyHost;

public sealed class PolicyLayer : Layer
{
}

public sealed class PolicyScope : IScopeDefinition
{
    public const int ScopeId = 71;

    public ScopeOptions Options => ScopeOptions.Inline;
}
