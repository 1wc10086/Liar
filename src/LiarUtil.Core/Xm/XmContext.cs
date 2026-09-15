namespace LiarUtil.Core.Xm;

internal sealed class XmChannel
{
    public XmInstrument? Instrument;

    public XmSample? Sample;

    public XmPatternSlot Current;

    public uint SamplePosition;

    public uint Step;

    public float ActualLeft;

    public float ActualRight;

    public float TargetLeft;

    public float TargetRight;

    public uint FrameCount;

    public float[] EndOfPreviousSample = new float[XmConst.RampingPoints];

    public ushort Period;

    public ushort TonePortamentoTargetPeriod;

    public ushort FadeoutVolume;

    public ushort AutovibratoTicks;

    public ushort VolumeEnvelopeFrameCount;

    public ushort PanningEnvelopeFrameCount;

    public int VolumeEnvelopeVolume;

    public int PanningEnvelopePanning;

    public int Volume;

    public int VolumeOffset;

    public int Panning;

    public int BasePanning;

    public int OrigNote;

    public sbyte Finetune;

    public int NextInstrument;

    public int VolumeSlideParam;

    public int FineVolumeSlideUpParam;

    public int FineVolumeSlideDownParam;

    public int GlobalVolumeSlideParam;

    public int PanningSlideParam;

    public int PortamentoUpParam;

    public int PortamentoDownParam;

    public int FinePortamentoUpParam;

    public int FinePortamentoDownParam;

    public int ExtraFinePortamentoUpParam;

    public int ExtraFinePortamentoDownParam;

    public int GlissandoControlParam;

    public int GlissandoControlError;

    public int TonePortamentoParam;

    public int MultiRetrigParam;

    public int MultiRetrigTicks;

    public int PatternLoopOrigin;

    public int PatternLoopCount;

    public int SampleOffsetParam;

    public bool SampleOffsetInvalid;

    public int TremoloParam;

    public int TremoloTicks;

    public int TremoloControlParam;

    public int VibratoParam;

    public int VibratoTicks;

    public int VibratoOffset;

    public bool ShouldResetVibrato;

    public int VibratoControlParam;

    public int AutovibratoOffset;

    public bool ShouldResetArpeggio;

    public int ArpNoteOffset;

    public int TremorParam;

    public int TremorTicks;

    public bool TremorOn;

    public int EffectParam;

    public bool Sustained;

    public void Reset()
    {
        Instrument = null;
        Sample = null;
        Current = default;
        SamplePosition = 0;
        Step = 0;
        ActualLeft = 0f;
        ActualRight = 0f;
        TargetLeft = 0f;
        TargetRight = 0f;
        FrameCount = 0;
        Array.Clear(EndOfPreviousSample);
        Period = 0;
        TonePortamentoTargetPeriod = 0;
        FadeoutVolume = 0;
        AutovibratoTicks = 0;
        VolumeEnvelopeFrameCount = 0;
        PanningEnvelopeFrameCount = 0;
        VolumeEnvelopeVolume = 0;
        PanningEnvelopePanning = 0;
        Volume = 0;
        VolumeOffset = 0;
        Panning = 0;
        BasePanning = 0;
        OrigNote = 0;
        Finetune = 0;
        NextInstrument = 0;
        VolumeSlideParam = 0;
        FineVolumeSlideUpParam = 0;
        FineVolumeSlideDownParam = 0;
        GlobalVolumeSlideParam = 0;
        PanningSlideParam = 0;
        PortamentoUpParam = 0;
        PortamentoDownParam = 0;
        FinePortamentoUpParam = 0;
        FinePortamentoDownParam = 0;
        ExtraFinePortamentoUpParam = 0;
        ExtraFinePortamentoDownParam = 0;
        GlissandoControlParam = 0;
        GlissandoControlError = 0;
        TonePortamentoParam = 0;
        MultiRetrigParam = 0;
        MultiRetrigTicks = 0;
        PatternLoopOrigin = 0;
        PatternLoopCount = 0;
        SampleOffsetParam = 0;
        SampleOffsetInvalid = false;
        TremoloParam = 0;
        TremoloTicks = 0;
        TremoloControlParam = 0;
        VibratoParam = 0;
        VibratoTicks = 0;
        VibratoOffset = 0;
        ShouldResetVibrato = false;
        VibratoControlParam = 0;
        AutovibratoOffset = 0;
        ShouldResetArpeggio = false;
        ArpNoteOffset = 0;
        TremorParam = 0;
        TremorTicks = 0;
        TremorOn = false;
        EffectParam = 0;
        Sustained = false;
    }
}

internal sealed partial class XmContext
{
    private const int MaxRenderedRows = 262144;

    private readonly XmModule _module;
    private readonly XmChannel[] _channels;
    private readonly XmPatternSlot[] _slots;

    private uint _remainingSamplesInTick;
    private int _currentTableIndex;
    private int _currentTick;
    private int _currentRow;
    private int _extraRowsDone;
    private int _extraRows;
    private int _globalVolume;
    private int _currentTempo;
    private int _currentBpm;
    private bool _patternBreak;
    private bool _positionJump;
    private int _jumpDest;
    private int _jumpRow;
    private int _renderedRows;
    private bool _loopJump;
    private bool _finished;

    internal XmContext(XmModule module, int sampleRate)
    {
        _module = module;
        SampleRate = sampleRate;
        _channels = new XmChannel[module.NumChannels];
        for (var i = 0; i < _channels.Length; i++)
        {
            _channels[i] = new XmChannel();
        }

        _slots = module.PatternSlots;
        Reset();
    }

    internal int SampleRate { get; }

    internal bool Finished => _finished;

    internal void Reset()
    {
        for (var i = 0; i < _channels.Length; i++)
        {
            var ch = _channels[i];
            ch.Reset();
            ch.BasePanning = _module.DefaultChannelPanning[i];
        }

        _remainingSamplesInTick = 0;
        _currentTableIndex = 0;
        _currentTick = 0;
        _currentRow = 0;
        _extraRowsDone = 0;
        _extraRows = 0;
        _globalVolume = _module.DefaultGlobalVolume;
        _currentTempo = _module.DefaultTempo;
        _currentBpm = _module.DefaultBpm;
        _patternBreak = false;
        _positionJump = false;
        _jumpDest = 0;
        _jumpRow = 0;
        _renderedRows = 0;
        _loopJump = false;
        _finished = false;
    }

    private void AdvanceTick()
    {
        var next = (long)_remainingSamplesInTick - XmConst.TickSubsamples;
        var underflow = next < 0;
        _remainingSamplesInTick = unchecked((uint)next);
        if (underflow)
        {
            Tick();
        }
    }

}
