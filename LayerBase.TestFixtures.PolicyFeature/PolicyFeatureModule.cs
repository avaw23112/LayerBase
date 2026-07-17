using LayerBase.Core.Event;
using LayerBase.Event.EventMetaData;
using LayerBase.Layers;
using LayerBase.Modules;
using LayerBase.Scope;
using LayerBase.TestFixtures.PolicyHost;

namespace LayerBase.TestFixtures.PolicyFeature;

[AssemblyModule("production-hardening-policy-feature")]
public sealed partial class ProductionHardeningPolicyModule
{
}

public partial struct ScopedDamageEvent
{
    public int TargetId;
    public int Amount;
}

[OwnerLayer(typeof(PolicyLayer))]
[Scope<PolicyScope>]
public sealed class ScopedDamageEventMetaData
    : EventMetaData<ScopedDamageEvent>
{
    public override EventPostPolicy? PostPolicy =>
        new EventPostPolicy(
            PostDeliveryMode.Coalesced,
            BackpressurePolicy.RejectNew,
            maxPending: 0,
            MergeFailurePolicy.Reject);

    public override int GetPostCoalesceKey(in ScopedDamageEvent value)
    {
        return value.TargetId;
    }

    public override bool TryMergePostEvent(
        ref ScopedDamageEvent current,
        in ScopedDamageEvent next)
    {
        current.Amount += next.Amount;
        return true;
    }
}
