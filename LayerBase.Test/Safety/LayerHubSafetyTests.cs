using System.Reflection;
using LayerBase;

namespace EventsTest.Safety;

[TestFixture]
public sealed class LayerHubSafetyTests
{
    [Test]
    public void LayerHub_DoesNotExposeRuntimeDispatchForwarders()
    {
        var forbiddenNames = new HashSet<string>
        {
            "Send",
            "Post",
            "TryPost",
            "PostLatest",
            "PostCoalesced",
            "MarkDirty",
            "CallAsync"
        };

        var methods = typeof(LayerHub)
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .Where(method => forbiddenNames.Contains(method.Name))
            .Select(method => method.Name)
            .ToArray();

        Assert.That(methods, Is.Empty);
    }

    [Test]
    public void LayerRuntime_ExposesOwnedRuntimeDispatchApis()
    {
        var runtimeMethods = typeof(LayerRuntime)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(method => method.Name)
            .ToHashSet();

        Assert.That(runtimeMethods, Does.Contain("Send"));
        Assert.That(runtimeMethods, Does.Contain("Post"));
        Assert.That(runtimeMethods, Does.Contain("TryPost"));
        Assert.That(runtimeMethods, Does.Contain("PostLatest"));
        Assert.That(runtimeMethods, Does.Contain("PostCoalesced"));
        Assert.That(runtimeMethods, Does.Contain("CallAsync"));
    }
}
