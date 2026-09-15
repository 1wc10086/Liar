namespace LiarUtil.Core.Sps;

internal readonly record struct EaacHeader(uint First, uint Second)
{
    public int Version => (int)((First >> 28) & 0x0F);

    public int Codec => (int)((First >> 24) & 0x0F);

    public int ChannelConfig => (int)((First >> 18) & 0x3F);

    public int SampleRate => (int)(First & 0x03FFFF);

    public int Type => (int)((Second >> 30) & 0x03);

    public int LoopFlag => (int)((Second >> 29) & 0x01);

    public int SampleCount => (int)(Second & 0x1FFFFFFF);

    public int Channels => ChannelConfig + 1;
}
