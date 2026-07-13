namespace LayerBase.Actor.RuntimeCommands;

internal enum ActorCommandKind : byte
{
    Post = 0,
    PostMany = 1,
    Disable = 2,
    Release = 3,
    Enable = 4,
    Destroy = 5,
    Detach = 6
}
