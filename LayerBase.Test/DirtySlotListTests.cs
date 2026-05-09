using LayerBase.Actor;

namespace LayerBase.Test;

[TestFixture]
public sealed class DirtySlotListTests
{
    [Test]
    public void AddIfNotExists_should_not_add_duplicate_slot()
    {
        var list = new DirtySlotList(initialCapacity: 4);

        list.AddIfNotExists(2);
        list.AddIfNotExists(2);

        Assert.That(list.Count, Is.EqualTo(1));
        Assert.That(list.TryPeek(out int slotIndex), Is.True);
        Assert.That(slotIndex, Is.EqualTo(2));
    }

    [Test]
    public void Pop_should_allow_slot_to_be_added_again()
    {
        var list = new DirtySlotList(initialCapacity: 4);

        list.AddIfNotExists(2);
        list.Pop();
        list.AddIfNotExists(2);

        Assert.That(list.Count, Is.EqualTo(1));
    }

    [Test]
    public void MoveHeadToTail_should_keep_contains_mark()
    {
        var list = new DirtySlotList(initialCapacity: 4);

        list.AddIfNotExists(1);
        list.AddIfNotExists(2);
        list.MoveHeadToTail();
        list.AddIfNotExists(1);

        Assert.That(list.Count, Is.EqualTo(2));
        Assert.That(list.TryPeek(out int slotIndex), Is.True);
        Assert.That(slotIndex, Is.EqualTo(2));
    }
}
