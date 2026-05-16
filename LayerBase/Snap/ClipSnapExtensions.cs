namespace LayerBase.Snap;

public static class ClipSnapExtensions
{
    public static ClipSnapHandle<TClip> Clip<TClip>(this object target)
    {
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        if (target is IClipSnap<TClip> clipSnap)
        {
            return new ClipSnapHandle<TClip>(clipSnap);
        }

        throw new InvalidOperationException(
            $"Object '{target.GetType().Name}' does not implement IClipSnap<{typeof(TClip).Name}>.");
    }

    public static bool TryClip<TClip>(this object target, out ClipSnapHandle<TClip> handle)
    {
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        if (target is IClipSnap<TClip> clipSnap)
        {
            handle = new ClipSnapHandle<TClip>(clipSnap);
            return true;
        }

        handle = default;
        return false;
    }
}

public readonly struct ClipSnapHandle<TClip>
{
    private readonly IClipSnap<TClip>? _snap;

    public ClipSnapHandle(IClipSnap<TClip> snap)
    {
        _snap = snap;
    }

    public TClip Serialize()
    {
        if (_snap == null)
        {
            throw new InvalidOperationException(
                $"ClipSnapHandle<{typeof(TClip).Name}> is not initialized.");
        }

        return _snap.Serialize();
    }

    public void Deserialize(in TClip clip)
    {
        if (_snap == null)
        {
            throw new InvalidOperationException(
                $"ClipSnapHandle<{typeof(TClip).Name}> is not initialized.");
        }

        _snap.Deserialize(in clip);
    }
}
