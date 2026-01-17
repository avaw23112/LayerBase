using LayerBase.Core.EventCatalogue;

namespace EventsTest;

public class EventCatalogueTests
{
    [Test]
    public void Category_tokens_reflect_parent_child_relationships()
    {
        var root = EventCatalogue.Path("root");
        var childA = root.Combine("a");
        var childB = root.Combine("b");

        Assert.That(EventCatalogue.IsSameCategory(childA.GetToken(), childB.GetToken()), Is.True);
        Assert.That(EventCatalogue.IsBelongCategory(root.GetToken(), childA.GetToken()), Is.True);
        Assert.That(EventCatalogue.IsBelongCategory(childA.GetToken(), childB.GetToken()), Is.False);
    }
}
