using LayerBase.Core.Event;
using LayerBase.DI;
using LayerBase.Event.Delay;
using LayerBase.LayerHub;
using LayerBase.Layers;
using NUnit.Framework;

namespace EventsTest
{
    [TestFixture]
    public class DelayPublisherTests
    {
        private class DummyLayer : Layer { }

        [SetUp]
        public void SetUp() => LayerHub.Reset();

        [Test]
        public void DelayLocal_is_stored_and_can_be_retrieved()
        {
            var layer = new DummyLayer();
            LayerHub.CreateLayers().Push(layer).Build();

            var evt = new DelayTestEvent { Value = 42 };
            // 合法 API 调用
            layer.DelayLocal(evt, 1.0f);

            var publisher = layer.SubscribeDelay<DelayTestEvent>();
            Assert.That(publisher.HasValue, Is.True);
            Assert.That(publisher.Direction, Is.EqualTo(DelayDirection.Local));
            Assert.That(publisher.TryGet(out var retrieved), Is.True);
            Assert.That(retrieved.Value, Is.EqualTo(42));
        }

        [Test]
        public void Delay_expires_after_ttl_and_is_dropped()
        {
            var layer = new DummyLayer();
            LayerHub.CreateLayers().Push(layer).Build();

            layer.DelayLocal(new DelayTestEvent(), 0.05f);
            var publisher = layer.SubscribeDelay<DelayTestEvent>();

            Assert.That(publisher.HasValue, Is.True);

            // 合法推进
            layer.Pump(0.1f);
            
            Assert.That(publisher.HasValue, Is.False, "Event must expire via legal Pump cycle");
            Assert.That(publisher.TryGet(out _), Is.False);
        }

        [Test]
        public void TryTake_consumes_the_value()
        {
            var layer = new DummyLayer();
            LayerHub.CreateLayers().Push(layer).Build();

            layer.DelayGlobal(new DelayTestEvent { Value = 100 }, 1.0f);
            var publisher = layer.SubscribeDelay<DelayTestEvent>();

            Assert.That(publisher.TryTake(out var val), Is.True);
            Assert.That(val.Value, Is.EqualTo(100));
            Assert.That(publisher.HasValue, Is.False, "Value should be consumed via official TryTake");
        }

        [Test]
        public void ContractId_is_preserved()
        {
            var layer = new DummyLayer();
            LayerHub.CreateLayers().Push(layer).Build();

            layer.DelayLocal(new DelayTestEvent(), 1.0f, contractId: 999);
            var publisher = layer.SubscribeDelay<DelayTestEvent>();

            Assert.That(publisher.ContractId, Is.EqualTo(999));
        }

        public struct DelayTestEvent { public int Value; }
    }
}
