namespace LiarUtil.Core.Xm;

internal struct XmEnvelopePoint
{
    public int Frame;

    public int Value;
}

internal sealed class XmEnvelope
{
    public XmEnvelopePoint[] Points = new XmEnvelopePoint[XmConst.MaxEnvelopePoints];

    public int NumPoints;

    public int SustainPoint;

    public int LoopStartPoint;

    public int LoopEndPoint;

    public void ResetAll()
    {
        Points = new XmEnvelopePoint[XmConst.MaxEnvelopePoints];
        NumPoints = 0;
        SustainPoint = 0;
        LoopStartPoint = 0;
        LoopEndPoint = 0;
    }
}

internal sealed class XmInstrument
{
    public string Name = string.Empty;

    public XmEnvelope VolumeEnvelope = new();

    public XmEnvelope PanningEnvelope = new();

    public ushort[] SampleOfNotes = new ushort[XmConst.MaxNote];

    public int VolumeFadeout;

    public int VibratoType;

    public int VibratoSweep;

    public int VibratoDepth;

    public int VibratoRate;
}

internal sealed class XmSample
{
    public string Name = string.Empty;

    public int Index;

    public int Length;

    public int LoopLength;

    public bool PingPong;

    public int Volume;

    public int Panning = XmConst.MaxPanning / 2;

    public sbyte Finetune;

    public sbyte RelativeNote;

    public bool Is16Bit;
}

internal readonly record struct XmPattern(int RowsIndex, int NumRows);

internal struct XmPatternSlot
{
    public byte Note;

    public byte Instrument;

    public byte VolumeColumn;

    public byte EffectType;

    public byte EffectParam;

    public readonly bool IsEmpty => Note == 0 && Instrument == 0 && VolumeColumn == 0
        && EffectType == 0 && EffectParam == 0;
}

internal sealed class XmModule
{
    public string Name = string.Empty;

    public string TrackerName = string.Empty;

    public int SamplesDataLength;

    public int NumRows;

    public int Length;

    public int NumPatterns;

    public int NumSamples;

    public int NumChannels;

    public int NumInstruments;

    public byte[] PatternTable = new byte[XmConst.PatternOrderTableLength];

    public int RestartPosition;

    public int DefaultTempo;

    public int DefaultBpm;

    public int DefaultGlobalVolume = XmConst.MaxVolume;

    public byte[] DefaultChannelPanning = new byte[XmConst.MaxChannels];

    public bool AmigaFrequencies;

    public bool FastS3mVolumeSlides;

    public XmPattern[] Patterns = [];

    public XmPatternSlot[] PatternSlots = [];

    public XmInstrument[] Instruments = [];

    public XmSample[] Samples = [];

    public short[] SamplesData = [];
}

internal sealed class XmPrescan
{
    public XmFormat Format;

    public int NumRows;

    public int SamplesDataLength;

    public int NumPatterns;

    public int NumSamples;

    public int PotLength;

    public int NumChannels;

    public int NumInstruments;
}
