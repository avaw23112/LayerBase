using System.Reflection;
using Arch.Core;
using LayerBase;
using LayerBase.Core.Event;
using LayerBase.Scope;

namespace EventsTest;

[TestFixture]
[Category("ProductionHardening")]
public sealed class RuntimeOwnershipBoundaryTests
{
    [Test]
    public void ScopeRuntime_has_no_LayerRuntime_reference()
    {
        AssertNoTypeReference(
            typeof(ScopeRuntime),
            typeof(LayerRuntime),
            "ScopeRuntime must communicate through bound callbacks instead of retaining LayerRuntime.");
    }

    [Test]
    public void ScopeRuntime_has_no_ScopeRuntimeHost_reference()
    {
        AssertNoTypeReference(
            typeof(ScopeRuntime),
            typeof(ScopeRuntimeHost),
            "ScopeRuntimeHost may coordinate ScopeRuntime, but ScopeRuntime must not point back to the host.");
    }

    [Test]
    public void World_has_no_LayerRuntime_reference()
    {
        AssertNoTypeReference(
            typeof(World),
            typeof(LayerRuntime),
            "Arch.Core.World must keep ECS/projection capabilities without a LayerRuntime back reference.");
    }

    [Test]
    public void ScopeRuntime_uses_only_bound_callbacks()
    {
        Type callbackType = typeof(ScopeRuntimeCallbacks);

        FieldInfo[] callbackFields = typeof(ScopeRuntime)
            .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(field => field.FieldType == callbackType)
            .ToArray();

        Assert.That(callbackFields.Length, Is.EqualTo(1),
            "ScopeRuntime must retain exactly one immutable ScopeRuntimeCallbacks field.");
    }

    [Test]
    public void System_route_callbacks_are_single_cast()
    {
        ScopeSystemCallHandler firstCall = static (
            ScopeRuntime scope,
            in ScopeCallEnvelope envelope,
            EventPayloadStorage payloadStorage) => false;
        ScopeSystemCallHandler secondCall = static (
            ScopeRuntime scope,
            in ScopeCallEnvelope envelope,
            EventPayloadStorage payloadStorage) => false;
        ScopeSystemEventHandler firstEvent = static (
            ScopeRuntime scope,
            in ScopeEventEnvelope envelope,
            EventPayloadStorage payloadStorage) => false;
        ScopeSystemEventHandler secondEvent = static (
            ScopeRuntime scope,
            in ScopeEventEnvelope envelope,
            EventPayloadStorage payloadStorage) => false;

        Assert.Throws<ArgumentException>(() => new ScopeRuntimeCallbacks(
            static (in ScopeFaultRecord fault) => { },
            static scopeId => { },
            static (layerIndex, source, eventName, exception) => { },
            static scopeId => { },
            static scopeId => { },
            firstCall + secondCall,
            null));

        Assert.Throws<ArgumentException>(() => new ScopeRuntimeCallbacks(
            static (in ScopeFaultRecord fault) => { },
            static scopeId => { },
            static (layerIndex, source, eventName, exception) => { },
            static scopeId => { },
            static scopeId => { },
            null,
            firstEvent + secondEvent));
    }

    private static void AssertNoTypeReference(Type subject, Type forbidden, string because)
    {
        string[] references = subject
            .GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .SelectMany(member => GetReferencedTypes(member)
                .Where(type => References(type, forbidden))
                .Select(type => $"{member.MemberType} {member.Name}: {type.FullName}"))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.That(references, Is.Empty, because + Environment.NewLine + string.Join(Environment.NewLine, references));
    }

    private static IEnumerable<Type> GetReferencedTypes(MemberInfo member)
    {
        switch (member)
        {
            case FieldInfo field:
                yield return field.FieldType;
                break;
            case PropertyInfo property:
                yield return property.PropertyType;
                break;
            case MethodInfo method:
                yield return method.ReturnType;
                foreach (ParameterInfo parameter in method.GetParameters())
                    yield return parameter.ParameterType;
                break;
            case ConstructorInfo constructor:
                foreach (ParameterInfo parameter in constructor.GetParameters())
                    yield return parameter.ParameterType;
                break;
            case EventInfo @event:
                if (@event.EventHandlerType != null)
                    yield return @event.EventHandlerType;
                break;
        }
    }

    private static bool References(Type candidate, Type forbidden)
    {
        if (candidate == forbidden)
            return true;

        if (candidate.HasElementType)
            return References(candidate.GetElementType()!, forbidden);

        if (candidate.IsGenericType)
            return candidate.GetGenericArguments().Any(argument => References(argument, forbidden));

        return false;
    }
}
