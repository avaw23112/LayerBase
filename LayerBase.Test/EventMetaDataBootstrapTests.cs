using LayerBase;
using LayerBase.Core.Event;
using LayerBase.DI;
using LayerBase.Event.EventMetaData;
using LayerBase.Layers;

namespace EventsTest;

public partial struct BootstrapLatestEvent
{
    public int Value;
}

public sealed class BootstrapLatestEventMetaData
    : EventMetaData<BootstrapLatestEvent>
{
    public override EventPostPolicy? PostPolicy =>
        new EventPostPolicy(
            PostDeliveryMode.Latest,
            BackpressurePolicy.RejectNew,
            maxPending: 0);
}

public sealed class BootstrapLayer : Layer
{
}

[TestFixture]
public sealed class EventMetaDataBootstrapTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        EventMetaDataHandler.Clear();
    }

    [Test]
    public void Global_Registry_meta_data_is_loaded_into_policy_table()
    {
        EventMetaDataRegistry.RegisterMetaData<BootstrapLatestEvent>(
            new BootstrapLatestEventMetaData());

        using var runtime = LayerHub.CreateLayers()
            .Push(new BootstrapLayer())
            .Build();

        var policyTable = runtime.PolicyTable;
        var policy = policyTable.GetPostPolicy(EventTypeId<BootstrapLatestEvent>.Id);

        Assert.That(policy.Mode, Is.EqualTo(PostDeliveryMode.Latest));
    }

    [Test]
    public void Global_registry_survives_layer_reset()
    {
        EventMetaDataRegistry.RegisterMetaData<BootstrapLatestEvent>(
            new BootstrapLatestEventMetaData());

        var runtimeA = LayerHub.CreateLayers()
            .Push(new BootstrapLayer())
            .Build();

        var policyA = runtimeA.PolicyTable.GetPostPolicy(EventTypeId<BootstrapLatestEvent>.Id);
        Assert.That(policyA.Mode, Is.EqualTo(PostDeliveryMode.Latest));

        runtimeA.Dispose();
        LayerHub.Reset();

        EventMetaDataRegistry.RegisterMetaData<BootstrapLatestEvent>(
            new BootstrapLatestEventMetaData());

        using var runtimeB = LayerHub.CreateLayers()
            .Push(new BootstrapLayer())
            .Build();

        var policyB = runtimeB.PolicyTable.GetPostPolicy(EventTypeId<BootstrapLatestEvent>.Id);
        Assert.That(policyB.Mode, Is.EqualTo(PostDeliveryMode.Latest));
    }
}
