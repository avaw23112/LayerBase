using LayerBase;
using LayerBase.Core.Event;
using LayerBase.Event.EventMetaData;
using LayerBase.Layers;
using LayerBase.Modules;
using LayerBase.Scope;

namespace LayerBase.Test;

public partial struct UnificationTestEvent;

[TestFixture]
[Category("ProductionHardening")]
public sealed class EventTypeIdUnificationTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        EventMetaDataHandler.Clear();
    }

    [Test]
    public void Module_event_metadata_uses_generic_event_id()
    {
        using LayerRuntime runtime = LayerHub.CreateLayers()
            .Push(new TestLayer())
            .AddAssemblyModule(new TestModule())
            .Build();

        int eventId = EventTypeId<UnificationTestEvent>.Id;
        EventPostPolicy? policy = runtime.PolicyTable.GetPostPolicy(eventId);

        Assert.That(policy, Is.Not.Null);
        Assert.That(policy!.Value.Mode, Is.EqualTo(PostDeliveryMode.Latest));
    }

    public sealed class TestEventMetaData : EventMetaData<UnificationTestEvent>
    {
        public override EventPostPolicy? PostPolicy =>
            new EventPostPolicy(
                PostDeliveryMode.Latest,
                BackpressurePolicy.RejectNew,
                maxPending: 1);
    }

    public sealed class TestLayer : Layer
    {
    }

    private sealed class TestModule : IAssemblyModule
    {
        public AssemblyModuleId Id => new("event-id-unification");

        public AssemblyModuleManifest Manifest { get; } =
            new AssemblyModuleManifest(
                new AssemblyModuleId("event-id-unification"),
                Array.Empty<ServiceContribution>(),
                Array.Empty<ContextContribution>(),
                Array.Empty<LocalCallContribution>(),
                Array.Empty<EventHandlerContribution>(),
                Array.Empty<LayerToolContribution>(),
                new[]
                {
                    EventContribution.ForTypes(
                        typeof(UnificationTestEvent),
                        typeof(TestLayer),
                        typeof(MainScope),
                        static () => new TestEventMetaData())
                });
    }
}
