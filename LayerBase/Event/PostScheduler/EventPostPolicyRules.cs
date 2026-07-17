using System;

namespace LayerBase.Core.Event;

internal static class EventPostPolicyRules
{
    public static void Validate(in EventPostPolicy policy, string parameterName)
    {
        if (policy.MaxPending < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                policy.MaxPending,
                "MaxPending cannot be negative.");
        }

        switch (policy.Mode)
        {
            case PostDeliveryMode.Normal:
                if (policy.MergeFailure != MergeFailurePolicy.Reject)
                {
                    throw new ArgumentException(
                        "MergeFailure is only valid for Coalesced mode.",
                        parameterName);
                }
                break;

            case PostDeliveryMode.Coalesced:
                break;

            case PostDeliveryMode.Latest:
            case PostDeliveryMode.DirtySignal:
                if (policy.MergeFailure != MergeFailurePolicy.Reject)
                {
                    throw new ArgumentException(
                        $"{policy.Mode} mode requires MergeFailure=Reject.",
                        parameterName);
                }
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    policy.Mode,
                    "Unknown post delivery mode.");
        }
    }
}
