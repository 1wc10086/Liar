namespace LiarUtil.Core.Pax;

internal static class PaxFormat
{
    public const int MaxRecords = 100000;
    public const int CompositionHeader = 1;
    public const int LayerHeader = 2;
    public const int AnchorPoint = 3;
    public const int Position = 4;
    public const int Scale = 5;
    public const int Rotation = 6;
    public const int Opacity = 7;
    public const int KeyframeMarker = 8;
    public const int EndOfLayer = 9;
    public const int LoopRepeat = 10;
    public const int LoopPingPong = 11;
    public const int EndOfPopFx = 9999999;
}
