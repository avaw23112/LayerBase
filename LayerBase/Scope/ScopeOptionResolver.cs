namespace LayerBase.Scope;

public sealed class InvalidMainScopeOptionException : InvalidOperationException
{
    public const string ErrorCode = "InvalidMainScopeOption";

    public InvalidMainScopeOptionException(string message)
        : base(message)
    {
    }
}

internal readonly struct ResolvedScopeOption
{
    public ResolvedScopeOption(ScopeDescriptor descriptor, ScopeRuntimeOptions runtimeOptions)
    {
        Descriptor = descriptor;
        RuntimeOptions = runtimeOptions ?? throw new ArgumentNullException(nameof(runtimeOptions));
    }

    public ScopeDescriptor Descriptor { get; }

    public ScopeRuntimeOptions RuntimeOptions { get; }
}

internal static class ScopeOptionResolver
{
    public static ResolvedScopeOption Resolve(Type? scopeType, int scopeId, ScopeOptionTemplate template)
    {
        if (scopeType == typeof(MainScope) || scopeId == ScopeDescriptors.Main.ScopeId)
        {
            ValidateMainScopeTemplate(template);
            return new ResolvedScopeOption(
                ScopeDescriptors.Main,
                ScopeOptionCompiler.ToRuntimeOptions(template));
        }

        if (scopeType == null)
        {
            throw new ArgumentNullException(nameof(scopeType));
        }

        var descriptor = new ScopeDescriptor(
            scopeId,
            scopeType.Name,
            template.Threading,
            template.Clock,
            template.TickRateHz,
            template.StopPolicy);

        return new ResolvedScopeOption(
            descriptor,
            ScopeOptionCompiler.ToRuntimeOptions(template));
    }

    public static ResolvedScopeOption ResolveMain()
    {
        ScopeOptionTemplate template = ScopeOptionRegistry.TryGetTemplate(typeof(MainScope), out ScopeOptionTemplate registered)
            ? registered
            : ScopeOptionTemplate.Default;

        return Resolve(typeof(MainScope), ScopeDescriptors.Main.ScopeId, template);
    }

    public static ResolvedScopeOption Resolve(Type scopeType, int scopeId, ScopeDescriptor fallbackDescriptor)
    {
        ScopeOptionTemplate template = ScopeOptionRegistry.TryGetTemplate(scopeType, out ScopeOptionTemplate registered)
            ? registered
            : ScopeOptionTemplate.FromDescriptor(fallbackDescriptor);

        return Resolve(scopeType, scopeId, template);
    }

    public static ResolvedScopeOption ResolveDefault(Type scopeType, int scopeId)
    {
        ScopeOptionTemplate template = ScopeOptionRegistry.TryGetTemplate(scopeType, out ScopeOptionTemplate registered)
            ? registered
            : ScopeOptionTemplate.Default;

        return Resolve(scopeType, scopeId, template);
    }

    private static void ValidateMainScopeTemplate(ScopeOptionTemplate template)
    {
        if (template.Threading == ScopeThreadingMode.Inline &&
            template.Clock == ScopeClockMode.EngineDriven &&
            template.TickRateHz == 0)
        {
            return;
        }

        throw new InvalidMainScopeOptionException(
            "InvalidMainScopeOption: MainScope must use Inline threading, EngineDriven clock, and TickRateHz 0.");
    }
}
