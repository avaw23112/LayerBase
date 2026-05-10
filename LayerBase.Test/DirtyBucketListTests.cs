using LayerBase.Actor;

namespace LayerBase.Test;

[TestFixture]
public sealed class DirtyBucketListTests
{
    [Test]
    public void Mark_should_not_add_duplicate_bucket()
    {
        var list = new DirtyBucketList(initialCapacity: 4);

        list.Mark(3);
        list.Mark(3);

        Assert.That(list.Count, Is.EqualTo(1));
        Assert.That(list.TryPeek(out int bucketIndex), Is.True);
        Assert.That(bucketIndex, Is.EqualTo(3));
    }

    [Test]
    public void Pop_should_allow_bucket_to_be_added_again()
    {
        var list = new DirtyBucketList(initialCapacity: 4);

        list.Mark(3);
        list.Pop();
        list.Mark(3);

        Assert.That(list.Count, Is.EqualTo(1));
    }

    [Test]
    public void MoveHeadToTail_should_keep_contains_mark()
    {
        var list = new DirtyBucketList(initialCapacity: 4);

        list.Mark(1);
        list.Mark(2);
        list.MoveHeadToTail();
        list.Mark(1);

        Assert.That(list.Count, Is.EqualTo(2));
        Assert.That(list.TryPeek(out int bucketIndex), Is.True);
        Assert.That(bucketIndex, Is.EqualTo(2));
    }
}
