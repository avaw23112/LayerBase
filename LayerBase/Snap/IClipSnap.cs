namespace LayerBase.Snap;

public interface IClipSnap<TClip>
{
    TClip Serialize();

    void Deserialize(in TClip clip);
}
