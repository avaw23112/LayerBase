using LayerBase.Async;

namespace EventsTest.Safety;

[TestFixture]
public sealed class LBTaskSafetyTests
{
    [Test]
    public void RepeatedTrySetResult_ReturnsFalse()
    {
        var source = new LBTaskCompletionSource<int>();

        Assert.That(source.TrySetResult(1), Is.True);
        Assert.That(source.TrySetResult(2), Is.False);
    }

    [Test]
    public void RepeatedTrySetException_ReturnsFalse()
    {
        var source = new LBTaskCompletionSource<int>();

        Assert.That(source.TrySetException(new InvalidOperationException("first")), Is.True);
        Assert.That(source.TrySetException(new InvalidOperationException("second")), Is.False);
    }

    [Test]
    public void RepeatedTrySetCanceled_ReturnsFalse()
    {
        var source = new LBTaskCompletionSource<int>();

        Assert.That(source.TrySetCanceled(), Is.True);
        Assert.That(source.TrySetCanceled(), Is.False);
    }

    [Test]
    public void OldCompletionSource_CannotCompleteReusedLBTaskSource()
    {
        var oldSource = new LBTaskCompletionSource<int>();
        var oldTask = oldSource.Task;

        Assert.That(oldSource.TrySetResult(1), Is.True);
        Assert.That(oldTask.GetAwaiter().GetResult(), Is.EqualTo(1));

        var newSource = new LBTaskCompletionSource<int>();
        var newTask = newSource.Task;

        Assert.That(oldSource.TrySetResult(2), Is.False);
        Assert.That(newTask.GetAwaiter().IsCompleted, Is.False);

        Assert.That(newSource.TrySetResult(3), Is.True);
        Assert.That(newTask.GetAwaiter().GetResult(), Is.EqualTo(3));
    }
}
