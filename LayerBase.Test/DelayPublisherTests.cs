using LayerBase.Event.Delay;
using LayerBase.LayerHub;
using LayerBase.Layers;

namespace EventsTest;

public class DelayPublisherTests
{
	[SetUp]
	public void SetUp()
	{
		LayerHub.Reset();
	}

	[Test]
	public void Delay_latest_overwrites_within_layer()
	{
		var layer = new DummyLayer();
		LayerHub.CreateLayers().Push(layer).Build();

		layer.Delay(new DelayPayload(1), 1f);
		layer.Delay(new DelayPayload(2), 1f);

		var publisher = layer.SubscribeDelay<DelayPayload>();

		Assert.That(publisher.TryGet(out var value), Is.True);
		Assert.That(value.Id, Is.EqualTo(2));
	}

	[Test]
	public void Delay_expires_after_ttl_and_is_dropped()
	{
		var layer = new DummyLayer();
		LayerHub.CreateLayers().Push(layer).Build();

		layer.Delay(new DelayPayload(3), 0.05f);

		LayerHub.Pump(0.06f);

		var publisher = layer.SubscribeDelay<DelayPayload>();
		Assert.That(publisher.TryGet(out _), Is.False);
	}

	[Test]
	public void Direction_is_recorded_for_directional_delay()
	{
		var layer = new DummyLayer();
		LayerHub.CreateLayers().Push(layer).Build();

		var publisher = layer.SubscribeDelay<DelayPayload>();
		layer.BroadCastDelay(new DelayPayload(5), 1f);

		Assert.That(publisher.Direction, Is.EqualTo(DelayDirection.BroadCast));
		Assert.That(publisher.TryGet(out _), Is.True);
	}

	[Test]
	public void BroadCastDelay_propagates_to_upper_and_lower_layers()
	{
		var top = new DummyLayer();
		var middle = new DummyLayer();
		var bottom = new DummyLayer();
		LayerHub.CreateLayers().Push(top).Push(middle).Push(bottom).Build();

		middle.BroadCastDelay(new DelayPayload(30), 1f);

		AssertDelay(top, 30, DelayDirection.BroadCast);
		AssertDelay(middle, 30, DelayDirection.BroadCast);
		AssertDelay(bottom, 30, DelayDirection.BroadCast);
	}

	[Test]
	public void BubbleDelay_propagates_only_to_current_and_upper_layers()
	{
		var top = new DummyLayer();
		var middle = new DummyLayer();
		var bottom = new DummyLayer();
		LayerHub.CreateLayers().Push(top).Push(middle).Push(bottom).Build();

		middle.BubbleDelay(new DelayPayload(31), 1f);

		AssertDelay(top, 31, DelayDirection.Bubble);
		AssertDelay(middle, 31, DelayDirection.Bubble);
		AssertDelayMissing(bottom);
	}

	[Test]
	public void DropDelay_propagates_only_to_current_and_lower_layers()
	{
		var top = new DummyLayer();
		var middle = new DummyLayer();
		var bottom = new DummyLayer();
		LayerHub.CreateLayers().Push(top).Push(middle).Push(bottom).Build();

		middle.DropDelay(new DelayPayload(32), 1f);

		AssertDelayMissing(top);
		AssertDelay(middle, 32, DelayDirection.Drop);
		AssertDelay(bottom, 32, DelayDirection.Drop);
	}

	[Test]
	public void ContractLayer_discards_other_pending_events_with_same_contract()
	{
		var layer = new DummyLayer();
		LayerHub.CreateLayers().Push(layer).Build();

		layer.Delay(new DelayPayload(10), 1f, contractLayer: 7);
		layer.BubbleDelay(new DelayPayload(11), 1f, contractLayer: 7); // should clear previous

		var publisher = layer.SubscribeDelay<DelayPayload>();
		Assert.That(publisher.HasValue, Is.True);
		Assert.That(publisher.TryGet(out var value), Is.True);
		Assert.That(value.Id, Is.EqualTo(11));
		Assert.That(publisher.TryGet(out var secondRead), Is.True);
		Assert.That(secondRead.Id, Is.EqualTo(11));
	}

	[Test]
	public void TryTake_consumes_latest_value()
	{
		var layer = new DummyLayer();
		LayerHub.CreateLayers().Push(layer).Build();

		layer.Delay(new DelayPayload(20), 1f);

		var publisher = layer.SubscribeDelay<DelayPayload>();
		Assert.That(publisher.TryTake(out var value), Is.True);
		Assert.That(value.Id, Is.EqualTo(20));
		Assert.That(publisher.TryGet(out _), Is.False);
		Assert.That(publisher.HasValue, Is.False);
	}

	private sealed class DummyLayer : Layer
	{
	}

	private static void AssertDelay(DummyLayer layer, int expectedId, DelayDirection expectedDirection)
	{
		var publisher = layer.SubscribeDelay<DelayPayload>();
		Assert.That(publisher.Direction, Is.EqualTo(expectedDirection));
		Assert.That(publisher.TryGet(out var value), Is.True);
		Assert.That(value.Id, Is.EqualTo(expectedId));
	}

	private static void AssertDelayMissing(DummyLayer layer)
	{
		var publisher = layer.SubscribeDelay<DelayPayload>();
		Assert.That(publisher.TryGet(out _), Is.False);
		Assert.That(publisher.HasValue, Is.False);
	}

	private readonly struct DelayPayload
	{
		public DelayPayload(int id)
		{
			Id = id;
		}

		public int Id { get; }
	}
}
