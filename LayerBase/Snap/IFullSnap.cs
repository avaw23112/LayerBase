namespace LayerBase.Snap;

public interface IFullSnap
{
    void WriteFullSnap(ref SnapWriter writer);

    void ReadFullSnap(ref SnapReader reader);
}
