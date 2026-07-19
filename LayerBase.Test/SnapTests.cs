using System.Text.Json.Nodes;
using LayerBase;
using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.Layers;
using LayerBase.Snap;

namespace LayerBase.Test;

[TestFixture]
public class SnapTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public async Task FullSnap_runtime_collects_generated_nodes_and_round_trips_state()
    {
        var layer = new SnapLayer();
        LayerRuntime runtime = LayerHub.CreateLayers()
            .Push(layer)
            .Build();

        SnapStateService service = layer.StateService;
        SnapStateManager manager = service.Manager;

        Assert.That(layer, Is.InstanceOf<IGeneratedFullSnapNode>());
        Assert.That(service, Is.InstanceOf<IGeneratedFullSnapNode>());
        Assert.That(manager, Is.InstanceOf<IGeneratedFullSnapNode>());

        layer.LayerValue = 11;
        service.ServiceValue = 22;
        manager.ManagerValue = 33;

        SnapDocument document = await runtime.SerializeFullSnapAsync();

        Assert.That(document.Sections.Keys, Does.Contain("LayerBase.Test.SnapLayer_FullSnap"));
        Assert.That(document.Sections.Keys, Does.Contain("LayerBase.Test.SnapStateService_FullSnap"));
        Assert.That(document.Sections.Keys, Does.Contain("LayerBase.Test.SnapStateManager_FullSnap"));
        Assert.That(document.Sections.Keys, Has.None.Contains("ActorWorld"));
        Assert.That(document.Sections.Keys, Has.None.Contains("EcsWorld"));
        Assert.That(document.Sections.Keys, Has.None.Contains("PlainService"));

        Assert.That(((IGeneratedFullSnapNode)layer).__SnapVersion, Is.EqualTo(1));
        Assert.That(((IGeneratedFullSnapNode)service).__SnapVersion, Is.EqualTo(1));
        Assert.That(((IGeneratedFullSnapNode)manager).__SnapVersion, Is.EqualTo(1));

        layer.LayerValue = -1;
        service.ServiceValue = -1;
        manager.ManagerValue = -1;

        await runtime.DeserializeFullSnapAsync(document);

        Assert.That(layer.LayerValue, Is.EqualTo(11));
        Assert.That(service.ServiceValue, Is.EqualTo(22));
        Assert.That(manager.ManagerValue, Is.EqualTo(33));
    }

    [Test]
    public async Task FullSnap_deserialize_throws_when_required_field_is_missing()
    {
        var layer = new SnapLayer { LayerValue = 7 };
        LayerRuntime runtime = LayerHub.CreateLayers()
            .Push(layer)
            .Build();

        SnapDocument document = await runtime.SerializeFullSnapAsync();
        document.Sections["LayerBase.Test.SnapLayer_FullSnap"].Data.Remove("layerValue");

        SnapFormatException? exception = Assert.ThrowsAsync<SnapFormatException>(
            async () => await runtime.DeserializeFullSnapAsync(document));
        Assert.That(exception!.Message, Does.Contain("layerValue"));
    }

    [Test]
    public async Task SnapArrayReader_round_trips_objects_and_reports_type_mismatch()
    {
        var layer = new InventorySnapLayer();
        LayerRuntime runtime = LayerHub.CreateLayers()
            .Push(layer)
            .Build();

        layer.InventoryService.Items.Add(new ItemStack(1, 2));
        layer.InventoryService.Items.Add(new ItemStack(3, 4));

        SnapDocument document = await runtime.SerializeFullSnapAsync();

        layer.InventoryService.Items.Clear();
        await runtime.DeserializeFullSnapAsync(document);

        Assert.That(layer.InventoryService.Items, Has.Count.EqualTo(2));
        Assert.That(layer.InventoryService.Items[0].ItemId, Is.EqualTo(1));
        Assert.That(layer.InventoryService.Items[0].Count, Is.EqualTo(2));
        Assert.That(layer.InventoryService.Items[1].ItemId, Is.EqualTo(3));
        Assert.That(layer.InventoryService.Items[1].Count, Is.EqualTo(4));

        JsonArray items = (JsonArray)document.Sections["LayerBase.Test.InventorySnapService_FullSnap"].Data["items"]!;
        JsonObject firstItem = (JsonObject)items[0]!;
        firstItem["count"] = "bad";

        SnapFormatException? exception = Assert.ThrowsAsync<SnapFormatException>(
            async () => await runtime.DeserializeFullSnapAsync(document));
        Assert.That(exception!.Message, Does.Contain("count"));
    }

    [Test]
    public async Task SnapArrayReader_reports_out_of_range_access()
    {
        var layer = new OutOfRangeSnapLayer();
        LayerRuntime runtime = LayerHub.CreateLayers()
            .Push(layer)
            .Build();

        SnapDocument document = await runtime.SerializeFullSnapAsync();

        SnapFormatException? exception = Assert.ThrowsAsync<SnapFormatException>(
            async () => await runtime.DeserializeFullSnapAsync(document));
        Assert.That(exception!.Message, Does.Contain("out of range"));
    }

    [Test]
    public void ClipSnap_handles_multiple_clip_types_and_reports_missing_ones()
    {
        var carrier = new MultiClipCarrier();

        carrier.Clip<MoveClip>().Deserialize(new MoveClip(4.5f, 6.5f));
        carrier.Clip<HealthClip>().Deserialize(new HealthClip(99));

        MoveClip move = carrier.Clip<MoveClip>().Serialize();
        HealthClip health = carrier.Clip<HealthClip>().Serialize();

        Assert.That(move.X, Is.EqualTo(4.5f));
        Assert.That(move.Y, Is.EqualTo(6.5f));
        Assert.That(health.Value, Is.EqualTo(99));

        Assert.That(carrier.TryClip<MoveClip>(out ClipSnapHandle<MoveClip> moveHandle), Is.True);
        Assert.That(moveHandle.Serialize().X, Is.EqualTo(4.5f));
        Assert.That(carrier.TryClip<ManaClip>(out _), Is.False);

        Assert.That(
            () => carrier.Clip<ManaClip>(),
            Throws.TypeOf<InvalidOperationException>().With.Message.Contains("ManaClip"));
    }
}

public partial class SnapLayer : Layer, IFullSnap
{
    [Mount] private SnapStateService _stateService = null!;
    [Mount] private PlainService _plainService = null!;

    public int LayerValue { get; set; }

    public SnapStateService StateService => _stateService;

    public void WriteFullSnap(ref SnapWriter writer)
    {
        writer.WriteInt32("layerValue", LayerValue);
    }

    public void ReadFullSnap(ref SnapReader reader)
    {
        LayerValue = reader.ReadInt32("layerValue");
    }
}

public partial class SnapStateService : IService, IFullSnap
{
    [Mount] private SnapStateManager _manager = null!;

    public int ServiceValue { get; set; }

    public SnapStateManager Manager => _manager;

    public void ConfigureServices(IServiceCollection services)
    {
    }

    public void WriteFullSnap(ref SnapWriter writer)
    {
        writer.WriteInt32("serviceValue", ServiceValue);
    }

    public void ReadFullSnap(ref SnapReader reader)
    {
        ServiceValue = reader.ReadInt32("serviceValue");
    }
}

public partial class SnapStateManager : ILayerContext, IFullSnap
{
    public int ManagerValue { get; set; }

    public void WriteFullSnap(ref SnapWriter writer)
    {
        writer.WriteInt32("managerValue", ManagerValue);
    }

    public void ReadFullSnap(ref SnapReader reader)
    {
        ManagerValue = reader.ReadInt32("managerValue");
    }
}

public partial class PlainService : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
    }
}

public partial class InventorySnapLayer : Layer
{
    [Mount] private InventorySnapService _inventoryService = null!;

    public InventorySnapService InventoryService => _inventoryService;
}

public partial class InventorySnapService : IService, IFullSnap
{
    public List<ItemStack> Items { get; } = new();

    public void ConfigureServices(IServiceCollection services)
    {
    }

    public void WriteFullSnap(ref SnapWriter writer)
    {
        SnapArrayWriter items = writer.WriteArray("items");

        for (int i = 0; i < Items.Count; i++)
        {
            SnapWriter item = items.AddObject();
            item.WriteInt32("itemId", Items[i].ItemId);
            item.WriteInt32("count", Items[i].Count);
        }
    }

    public void ReadFullSnap(ref SnapReader reader)
    {
        Items.Clear();
        SnapArrayReader items = reader.ReadArray("items");

        for (int i = 0; i < items.Count; i++)
        {
            SnapReader item = items.ReadObject(i);
            Items.Add(new ItemStack(
                item.ReadInt32("itemId"),
                item.ReadInt32("count")));
        }
    }
}

public readonly struct ItemStack
{
    public ItemStack(int itemId, int count)
    {
        ItemId = itemId;
        Count = count;
    }

    public int ItemId { get; }

    public int Count { get; }
}

public partial class OutOfRangeSnapLayer : Layer
{
    [Mount] private OutOfRangeSnapService _service = null!;
}

public partial class OutOfRangeSnapService : IService, IFullSnap
{
    public void ConfigureServices(IServiceCollection services)
    {
    }

    public void WriteFullSnap(ref SnapWriter writer)
    {
        SnapArrayWriter items = writer.WriteArray("items");
        SnapWriter item = items.AddObject();
        item.WriteInt32("value", 1);
    }

    public void ReadFullSnap(ref SnapReader reader)
    {
        SnapArrayReader items = reader.ReadArray("items");
        _ = items.ReadObject(1);
    }
}

public sealed class MultiClipCarrier : IClipSnap<MoveClip>, IClipSnap<HealthClip>
{
    private float _x;
    private float _y;
    private int _hp;

    MoveClip IClipSnap<MoveClip>.Serialize()
    {
        return new MoveClip(_x, _y);
    }

    void IClipSnap<MoveClip>.Deserialize(in MoveClip clip)
    {
        _x = clip.X;
        _y = clip.Y;
    }

    HealthClip IClipSnap<HealthClip>.Serialize()
    {
        return new HealthClip(_hp);
    }

    void IClipSnap<HealthClip>.Deserialize(in HealthClip clip)
    {
        _hp = clip.Value;
    }
}

public readonly struct MoveClip
{
    public MoveClip(float x, float y)
    {
        X = x;
        Y = y;
    }

    public float X { get; }

    public float Y { get; }
}

public readonly struct HealthClip
{
    public HealthClip(int value)
    {
        Value = value;
    }

    public int Value { get; }
}

public readonly struct ManaClip
{
    public ManaClip(int value)
    {
        Value = value;
    }

    public int Value { get; }
}
