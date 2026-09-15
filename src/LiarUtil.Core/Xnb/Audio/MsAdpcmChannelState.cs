namespace LiarUtil.Core.Xnb.Audio;

internal struct MsAdpcmChannelState
{
    public int Predictor;
    public int Delta;
    public int Previous;
    public int Older;
}
