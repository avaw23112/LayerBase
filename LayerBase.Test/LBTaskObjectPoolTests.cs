using LayerBase.Async;

namespace LayerBase.Test;

[TestFixture]
public sealed class LBTaskObjectPoolTests
{
    [Test]
    public void ObjectPool_drops_returned_items_after_retained_limit()
    {
        var pool = new ObjectPool<object>(() => new object(), maxRetained: 2);

        pool.Return(new object());
        pool.Return(new object());
        pool.Return(new object());
        pool.Return(new object());

        Assert.That(pool.MaxRetained, Is.EqualTo(2));
        Assert.That(pool.Count, Is.EqualTo(2));

        object first = pool.Rent();
        object second = pool.Rent();
        object third = pool.Rent();

        Assert.That(pool.Count, Is.EqualTo(0));
        Assert.That(first, Is.Not.Null);
        Assert.That(second, Is.Not.Null);
        Assert.That(third, Is.Not.Null);
    }
}
