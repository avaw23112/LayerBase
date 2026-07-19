using LayerBase.Snap;
using LayerBase.Layers;
using LayerBase.Scope;
using System.Text.Json.Nodes;

namespace LayerBase.Test;

[TestFixture]
[Category("ProductionHardening")]
public sealed class FullSnapHardeningTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public async Task Unsupported_version_rejected()
    {
        var layer = new SnapLayer();
        using LayerRuntime runtime = LayerHub.CreateLayers()
            .Push(layer)
            .Build();

        layer.LayerValue = 11;
        SnapDocument document = await runtime.SerializeFullSnapAsync();
        document = new SnapDocument
        {
            FormatVersion = FullSnapLimits.Default.MaxFormatVersion + 1,
            Sections = document.Sections
        };

        var restoredLayer = new SnapLayer { LayerValue = -1 };
        SnapFormatException? ex = Assert.Throws<SnapFormatException>(
            () => LayerHub.CreateLayers()
                .Push(restoredLayer)
                .RestoreFrom(document)
                .Build());

        Assert.That(ex!.Message, Does.Contain("FormatVersion"));
        Assert.That(restoredLayer.LayerValue, Is.EqualTo(-1));
    }

    [Test]
    public async Task Unknown_scope_payload_rejected()
    {
        var layer = new SnapLayer();
        using LayerRuntime runtime = LayerHub.CreateLayers()
            .Push(layer)
            .Build();

        layer.LayerValue = 7;
        SnapDocument document = await runtime.SerializeFullSnapAsync();
        document.AddSection(new SnapSection
        {
            Key = "unknown-section",
            Version = 1,
            Data = new JsonObject()
        });

        var restoredLayer = new SnapLayer { LayerValue = -1 };
        SnapFormatException? ex = Assert.Throws<SnapFormatException>(
            () => LayerHub.CreateLayers()
                .Push(restoredLayer)
                .RestoreFrom(document)
                .Build());

        Assert.That(ex!.Message, Does.Contain("unknown-section"));
        Assert.That(restoredLayer.LayerValue, Is.EqualTo(-1));
    }

    [Test]
    public async Task Oversized_snapshot_rejected_before_mutation()
    {
        var layer = new SnapLayer();
        using LayerRuntime runtime = LayerHub.CreateLayers()
            .Push(layer)
            .Build();

        layer.LayerValue = 21;
        SnapDocument document = await runtime.SerializeFullSnapAsync();
        var limits = FullSnapLimits.Default with { MaxTotalBytes = 16 };

        var restoredLayer = new SnapLayer { LayerValue = -1 };
        SnapFormatException? ex = Assert.Throws<SnapFormatException>(
            () => LayerHub.CreateLayers()
                .Push(restoredLayer)
                .RestoreFrom(document, limits)
                .Build());

        Assert.That(ex!.Message, Does.Contain("MaxTotalBytes"));
        Assert.That(restoredLayer.LayerValue, Is.EqualTo(-1));
    }

    [Test]
    public async Task Deep_json_rejected()
    {
        var layer = new SnapLayer();
        using LayerRuntime runtime = LayerHub.CreateLayers()
            .Push(layer)
            .Build();

        layer.LayerValue = 5;
        SnapDocument document = await runtime.SerializeFullSnapAsync();
        JsonObject data = document.Sections["LayerBase.Test.SnapLayer_FullSnap"].Data;
        JsonObject cursor = data;
        for (int i = 0; i < 8; i++)
        {
            var child = new JsonObject();
            cursor["child"] = child;
            cursor = child;
        }

        var limits = FullSnapLimits.Default with { MaxJsonDepth = 4 };
        var restoredLayer = new SnapLayer { LayerValue = -1 };
        SnapFormatException? ex = Assert.Throws<SnapFormatException>(
            () => LayerHub.CreateLayers()
                .Push(restoredLayer)
                .RestoreFrom(document, limits)
                .Build());

        Assert.That(ex!.Message, Does.Contain("MaxJsonDepth"));
        Assert.That(restoredLayer.LayerValue, Is.EqualTo(-1));
    }

    [Test]
    public async Task Duplicate_scope_payload_rejected()
    {
        var layer = new SnapLayer();
        using LayerRuntime runtime = LayerHub.CreateLayers()
            .Push(layer)
            .Build();

        layer.LayerValue = 3;
        string json = await runtime.SerializeFullSnapJsonAsync();
        string key = "LayerBase.Test.SnapLayer_FullSnap";
        string duplicateJson = json.Replace(
            $"\"{key}\":",
            $"\"{key}\":{{\"Key\":\"{key}\",\"Version\":1,\"Data\":{{\"layerValue\":99}}}},\"{key}\":",
            StringComparison.Ordinal);

        SnapFormatException? ex = Assert.Throws<SnapFormatException>(
            () => JsonSnapCodec.DecodeFromString(duplicateJson, FullSnapLimits.Default));

        Assert.That(ex!.Message, Does.Contain("Duplicate"));
        Assert.That(layer.LayerValue, Is.EqualTo(3));
    }

    [Test]
    public void Snap_read_limits_reject_section_count()
    {
        string json = """
                      {"FormatVersion":1,"Sections":{"a":{"Key":"a","Version":1,"Data":{"v":1}},"b":{"Key":"b","Version":1,"Data":{"v":2}}}}
                      """;

        SnapFormatException? ex = Assert.Throws<SnapFormatException>(
            () => JsonSnapCodec.DecodeFromString(json, new SnapReadLimits { MaxSections = 1 }));

        Assert.That(ex!.Message, Does.Contain("MaxSections"));
    }

    [Test]
    public void Snap_read_limits_reject_single_section_size()
    {
        string json = """
                      {"FormatVersion":1,"Sections":{"a":{"Key":"a","Version":1,"Data":{"payload":"abcdefghijklmnopqrstuvwxyz"}}}}
                      """;

        SnapFormatException? ex = Assert.Throws<SnapFormatException>(
            () => JsonSnapCodec.DecodeFromString(json, new SnapReadLimits { MaxSectionBytes = 48 }));

        Assert.That(ex!.Message, Does.Contain("MaxSectionBytes"));
    }

    [Test]
    public void Snap_read_limits_reject_total_section_size()
    {
        string json = """
                      {"FormatVersion":1,"Sections":{"a":{"Key":"a","Version":1,"Data":{"payload":"abc"}},"b":{"Key":"b","Version":1,"Data":{"payload":"def"}}}}
                      """;

        SnapFormatException? ex = Assert.Throws<SnapFormatException>(
            () => JsonSnapCodec.DecodeFromString(json, new SnapReadLimits { MaxTotalSectionBytes = 80 }));

        Assert.That(ex!.Message, Does.Contain("MaxTotalSectionBytes"));
    }

    [Test]
    public void Snap_read_limits_reject_empty_key_null_data_and_invalid_version()
    {
        Assert.Throws<SnapFormatException>(
            () => JsonSnapCodec.DecodeFromString(
                """{"FormatVersion":1,"Sections":{"":{"Key":"","Version":1,"Data":{}}}}"""));

        Assert.Throws<SnapFormatException>(
            () => JsonSnapCodec.DecodeFromString(
                """{"FormatVersion":1,"Sections":{"a":{"Key":"a","Version":1,"Data":null}}}"""));

        Assert.Throws<SnapFormatException>(
            () => JsonSnapCodec.DecodeFromString(
                """{"FormatVersion":1,"Sections":{"a":{"Key":"a","Version":0,"Data":{}}}}"""));
    }

    [Test]
    public void JsonSnapCodec_default_options_are_not_shared_mutable_state()
    {
        JsonSnapCodec.DefaultOptions.WriteIndented = true;

        Assert.That(JsonSnapCodec.DefaultOptions.WriteIndented, Is.False);
    }

    [Test]
    public async Task Validation_failure_leaves_runtime_unchanged()
    {
        var layer = new SnapLayer();
        using LayerRuntime runtime = LayerHub.CreateLayers()
            .Push(layer)
            .Build();

        layer.LayerValue = 41;
        SnapDocument document = await runtime.SerializeFullSnapAsync();
        SnapSection existing = document.Sections["LayerBase.Test.SnapLayer_FullSnap"];
        document.Sections["LayerBase.Test.SnapLayer_FullSnap"] = new SnapSection
        {
            Key = existing.Key,
            Version = 999,
            Data = existing.Data
        };

        var restoredLayer = new SnapLayer { LayerValue = -1 };
        Assert.Throws<SnapFormatException>(
            () => LayerHub.CreateLayers()
                .Push(restoredLayer)
                .RestoreFrom(document)
                .Build());

        Assert.That(restoredLayer.LayerValue, Is.EqualTo(-1));
    }

    [Test]
    public async Task Apply_failure_marks_restore_faulted()
    {
        var layer = new FaultingReadSnapLayer();
        using LayerRuntime runtime = LayerHub.CreateLayers()
            .Push(layer)
            .Build();

        SnapDocument document = await runtime.SerializeFullSnapAsync();

        InvalidOperationException? ex = Assert.Throws<InvalidOperationException>(
            () => LayerHub.CreateLayers()
                .Push(new FaultingReadSnapLayer())
                .RestoreFrom(document)
                .Build());

        Assert.That(ex!.Message, Does.Contain("read failed"));
    }

    [Test]
    public async Task Diagnostics_report_real_counts()
    {
        var layer = new SnapLayer();
        using LayerRuntime runtime = LayerHub.CreateLayers()
            .Push(layer)
            .Build();

        layer.LayerValue = 13;
        SnapDocument document = await runtime.SerializeFullSnapAsync();
        using LayerRuntime restoredRuntime = LayerHub.CreateLayers()
            .Push(new SnapLayer())
            .RestoreFrom(document)
            .Build();

        ScopeDiagnosticsSnapshot scope = restoredRuntime.CaptureDiagnostics()
            .Scopes
            .Single(static snapshot => snapshot.ScopeId == ScopeDefinitionIds.Main);
        Assert.That(scope.Snap.NodeCount, Is.GreaterThan(0));
        Assert.That(scope.Snap.SerializeCount, Is.EqualTo(0));
        Assert.That(scope.Snap.DeserializeCount, Is.EqualTo(1));
        Assert.That(scope.Snap.FailureCount, Is.EqualTo(0));
        Assert.That(scope.Snap.LastBytes, Is.GreaterThan(0));
        Assert.That(scope.Snap.LastDurationTicks, Is.GreaterThan(0));
        Assert.That(scope.CompletionInboxCount, Is.GreaterThanOrEqualTo(0));
        Assert.That(scope.FaultInboxCount, Is.GreaterThanOrEqualTo(0));
        Assert.That(scope.FaultInboxDropped, Is.GreaterThanOrEqualTo(0));
        Assert.That(scope.FaultInboxMerged, Is.GreaterThanOrEqualTo(0));
        Assert.That(scope.FaultInboxHighWatermark, Is.GreaterThanOrEqualTo(0));
    }
}

public partial class FaultingReadSnapLayer : Layer, IFullSnap
{
    public void WriteFullSnap(ref SnapWriter writer)
    {
        writer.WriteInt32("value", 1);
    }

    public void ReadFullSnap(ref SnapReader reader)
    {
        throw new InvalidOperationException("read failed");
    }
}
