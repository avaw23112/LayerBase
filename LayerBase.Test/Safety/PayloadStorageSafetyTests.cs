using System.Collections.Concurrent;
using LayerBase;
using LayerBase.Core.Event;

namespace EventsTest.Safety;

[TestFixture]
public sealed class PayloadStorageSafetyTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public void PayloadStorage_MultipleProducers_DoNotCorruptSlots()
    {
        const int producerCount = 8;
        const int perProducer = 2048;
        const int runtimeId = 77;

        using var storage = new EventPayloadStorage();
        using var start = new Barrier(producerCount);
        var handles = new ConcurrentBag<(PayloadHandle Handle, int Value)>();

        var tasks = Enumerable.Range(0, producerCount)
                              .Select(producer => System.Threading.Tasks.Task.Run(() =>
                              {
                                  start.SignalAndWait();
                                  for (var i = 0; i < perProducer; i++)
                                  {
                                      var value = producer * perProducer + i;
                                      var handle = storage.Store(runtimeId, new PayloadRaceEvent(value));
                                      handles.Add((handle, value));
                                  }
                              }))
                              .ToArray();

        System.Threading.Tasks.Task.WaitAll(tasks);

        Assert.That(handles.Count, Is.EqualTo(producerCount * perProducer));
        Assert.That(handles.Select(item => (item.Handle.Index, item.Handle.Version)).Distinct().Count(),
            Is.EqualTo(handles.Count));

        foreach (var (handle, value) in handles)
        {
            Assert.That(storage.TryGet<PayloadRaceEvent>(runtimeId, handle, out var payload), Is.True);
            Assert.That(payload.Value, Is.EqualTo(value));
        }

        var releases = Partitioner.Create(handles.ToArray(), true)
                                  .GetPartitions(producerCount)
                                  .Select(partition => System.Threading.Tasks.Task.Run(() =>
                                  {
                                      using (partition)
                                      {
                                          while (partition.MoveNext())
                                              storage.Release(partition.Current.Handle);
                                      }
                                  }))
                                  .ToArray();

        System.Threading.Tasks.Task.WaitAll(releases);

        Assert.That(storage.CaptureDiagnostics().Outstanding, Is.EqualTo(0));
    }

    [Test]
    public void PayloadStorage_EachInstanceHasOwnStore()
    {
        var storage1 = new EventPayloadStorage();
        var storage2 = new EventPayloadStorage();

        var handle1 = storage1.Store(0, new PayloadRaceEvent(1));
        var handle2 = storage2.Store(0, new PayloadRaceEvent(2));

        Assert.That(storage1.TryGet<PayloadRaceEvent>(0, handle1, out var val1), Is.True);
        Assert.That(val1.Value, Is.EqualTo(1));

        Assert.That(storage2.TryGet<PayloadRaceEvent>(0, handle2, out var val2), Is.True);
        Assert.That(val2.Value, Is.EqualTo(2));

        Assert.That(storage1.CaptureDiagnostics().Outstanding, Is.EqualTo(1));
        Assert.That(storage2.CaptureDiagnostics().Outstanding, Is.EqualTo(1));
    }

    [Test]
    public void ScopeDisposeRace_ReleasesRejectedPayloads()
    {
        const int runtimeId = 78;

        using var storage = new EventPayloadStorage();
        var handle = storage.Store(runtimeId, new PayloadRaceEvent(1));

        Assert.That(storage.CaptureDiagnostics().Outstanding, Is.EqualTo(1));

        storage.Dispose();
        storage.Release(handle);

        Assert.That(storage.CaptureDiagnostics().Outstanding, Is.EqualTo(0));
    }

    private readonly struct PayloadRaceEvent
    {
        public PayloadRaceEvent(int value)
        {
            Value = value;
        }

        public int Value { get; }
    }
}
