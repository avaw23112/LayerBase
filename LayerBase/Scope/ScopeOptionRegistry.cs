namespace LayerBase.Scope;

public static class ScopeOptionRegistry
{
    private static readonly object Gate = new();
    private static readonly Dictionary<Type, Action> ReplayActions = new();
    private static Dictionary<Type, ScopeOptionTemplate> s_templates = new();

    public static void Register<TScope>(ScopeOption<TScope> option)
    {
        ScopeOptionTemplate template = ScopeOptionCompiler.Compile(option);
        Register(typeof(TScope), template);
    }

    public static void Clear()
    {
        Action[] replays;
        lock (Gate)
        {
            s_templates = new Dictionary<Type, ScopeOptionTemplate>();
            replays = ReplayActions.Values.ToArray();
        }

        for (int i = 0; i < replays.Length; i++)
        {
            replays[i]();
        }
    }

    internal static void SetReplay(Type scopeType, Action replay)
    {
        if (scopeType == null)
        {
            throw new ArgumentNullException(nameof(scopeType));
        }

        if (replay == null)
        {
            throw new ArgumentNullException(nameof(replay));
        }

        lock (Gate)
        {
            ReplayActions[scopeType] = replay;
        }
    }

    internal static bool TryGetTemplate(Type scopeType, out ScopeOptionTemplate template)
    {
        if (scopeType == null)
        {
            throw new ArgumentNullException(nameof(scopeType));
        }

        lock (Gate)
        {
            return s_templates.TryGetValue(scopeType, out template);
        }
    }

    private static void Register(Type scopeType, ScopeOptionTemplate template)
    {
        lock (Gate)
        {
            var next = new Dictionary<Type, ScopeOptionTemplate>(s_templates)
            {
                [scopeType] = template
            };
            s_templates = next;
        }
    }
}
