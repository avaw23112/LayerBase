namespace LayerBase.Scope;

public static class ScopeOptionAutoRegister<TScope>
{
    public static void SetReplay(Action replay)
    {
        ScopeOptionRegistry.SetReplay(typeof(TScope), replay);
        replay();
    }
}
