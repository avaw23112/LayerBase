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
		Assert.That(publisher.TryGet(out _), Is.False); // previous cleared
	}

	private sealed class DummyLayer : Layer
	{
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
