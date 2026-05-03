using LayerBase.Event.EventMetaData;

namespace LayerBase.Test;

public partial struct CoalescedTestEvent
{
    public int Id;
    public int Value;
}

public class CoalescedTestEventMetaData : EventMetaData<CoalescedTestEvent>
{
    public override int GetPostCoalesceKey(in CoalescedTestEvent value) => value.Id;
    
    public override bool TryMergePostEvent(ref CoalescedTestEvent current, in CoalescedTestEvent next)
    {
        if (next.Value == -1) return false;
        current.Value += next.Value;
        return true;
    }
}
