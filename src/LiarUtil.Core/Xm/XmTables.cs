namespace LiarUtil.Core.Xm;

internal static class XmConst
{
    internal const int SineWaveform = 0;
    internal const int RampDownWaveform = 1;
    internal const int SquareWaveform = 2;
    internal const int RandomWaveform = 3;
    internal const int RampUpWaveform = 4;

    internal const int EffectArpeggio = 0x00;
    internal const int EffectPortamentoUp = 0x01;
    internal const int EffectPortamentoDown = 0x02;
    internal const int EffectTonePortamento = 0x03;
    internal const int EffectVibrato = 0x04;
    internal const int EffectTonePortamentoVolumeSlide = 0x05;
    internal const int EffectVibratoVolumeSlide = 0x06;
    internal const int EffectTremolo = 0x07;
    internal const int EffectSetPanning = 0x08;
    internal const int EffectSetSampleOffset = 0x09;
    internal const int EffectVolumeSlide = 0x0A;
    internal const int EffectJumpToOrder = 0x0B;
    internal const int EffectSetVolume = 0x0C;
    internal const int EffectPatternBreak = 0x0D;
    internal const int EffectSetTempo = 0x0E;
    internal const int EffectSetBpm = 0x0F;
    internal const int EffectSetGlobalVolume = 0x10;
    internal const int EffectGlobalVolumeSlide = 0x11;
    internal const int EffectExtraFinePortamentoUp = 0x12;
    internal const int EffectExtraFinePortamentoDown = 0x13;
    internal const int EffectKeyOff = 0x14;
    internal const int EffectSetEnvelopePosition = 0x15;
    internal const int EffectFineVibrato = 0x16;
    internal const int EffectS3mPortamentoUp = 0x17;
    internal const int EffectS3mPortamentoDown = 0x18;
    internal const int EffectPanningSlide = 0x19;
    internal const int EffectS3mVibratoVolumeSlide = 0x1A;
    internal const int EffectMultiRetrigNote = 0x1B;
    internal const int EffectS3mTonePortamentoVolumeSlide = 0x1C;
    internal const int EffectTremor = 0x1D;
    internal const int EffectRowLoop = 0x1E;
    internal const int EffectS3mVolumeSlide = 0x1F;
    internal const int EffectS3mMultiRetrigNote = 0x20;
    internal const int EffectFinePortamentoUp = 0x21;
    internal const int EffectFinePortamentoDown = 0x22;
    internal const int EffectSetGlissandoControl = 0x23;
    internal const int EffectSetVibratoControl = 0x24;
    internal const int EffectSetFinetune = 0x25;
    internal const int EffectPatternLoop = 0x26;
    internal const int EffectSetTremoloControl = 0x27;
    internal const int EffectSetChannelPanning = 0x28;
    internal const int EffectRetriggerNote = 0x29;
    internal const int EffectFineVolumeSlideUp = 0x2A;
    internal const int EffectFineVolumeSlideDown = 0x2B;
    internal const int EffectCutNote = 0x2C;
    internal const int EffectDelayNote = 0x2D;
    internal const int EffectDelayPattern = 0x2E;
    internal const int EffectS3mTremolo = 0x2F;
    internal const int EffectS3mArpeggio = 0x30;
    internal const int EffectS3mTremor = 0x31;
    internal const int EffectNop = 0xFF;

    internal const int VolumeEffectSlideDown = 0x6;
    internal const int VolumeEffectSlideUp = 0x7;
    internal const int VolumeEffectFineSlideDown = 0x8;
    internal const int VolumeEffectFineSlideUp = 0x9;
    internal const int VolumeEffectVibratoSpeed = 0xA;
    internal const int VolumeEffectVibrato = 0xB;
    internal const int VolumeEffectSetPanning = 0xC;
    internal const int VolumeEffectPanningSlideLeft = 0xD;
    internal const int VolumeEffectPanningSlideRight = 0xE;
    internal const int VolumeEffectTonePortamento = 0xF;

    internal const int SampleNameLength = 24;
    internal const int InstrumentNameLength = 32;
    internal const int ModuleNameLength = 32;
    internal const int TrackerNameLength = 24;

    internal const int PatternOrderTableLength = 256;
    internal const int MaxNote = 96;
    internal const int MaxEnvelopePoints = 12;
    internal const int MaxRowsPerPattern = 256;
    internal const int RampingPoints = 255;
    internal const int MaxVolume = 64;
    internal const int MaxFadeoutVolume = 32768;
    internal const int MaxPanning = 256;
    internal const int MaxEnvelopeValue = 64;
    internal const int MinBpm = 32;
    internal const int MaxBpm = 255;
    internal const int MaxPatterns = 256;
    internal const int MaxInstruments = 255;
    internal const int MaxChannels = 255;

    internal const int NoteKeyOff = 128;
    internal const int NoteRetrigger = MaxNote + 1;
    internal const int NoteSwitch = MaxNote + 2;

    internal const float RampingVolumeRamp = 1f / 256f;
    internal const float Amplification = .25f;

    internal const int TickSubsamples = 1 << 13;
    internal const int MicrostepBits = 12;
    internal const int SampleMicrosteps = 1 << MicrostepBits;
    internal const int MaxSampleLength = int.MaxValue / SampleMicrosteps;

    internal const int EmptyPatternNumRows = 64;
    internal const int SampleHeaderSize = 40;

    internal const int SampleFlag16Bit = 0b0001_0000;
    internal const int SampleFlagPingPong = 0b0000_0010;
    internal const int SampleFlagForward = 0b0000_0001;

    internal const int EnvelopeFlagEnabled = 0b0000_0001;
    internal const int EnvelopeFlagSustain = 0b0000_0010;
    internal const int EnvelopeFlagLoop = 0b0000_0100;
}

internal enum XmFormat : byte
{
    Xmif = 0,
    Xm0104 = 1,
    Mod = 2,
    ModFlt8 = 3,
    S3m = 4,
}

internal enum PitchSlideBehaviour : byte
{
    Wraparound = 0,
    Clamp = 1,
    Cut = 2,
}

internal static class XmTables
{
    internal static readonly sbyte[] Sine = [
        0, 12, 24, 37, 48, 60, 71, 81,
        90, 98, 106, 112, 118, 122, 125, 127,
    ];

    internal static readonly byte[] MultiRetrigAdd = [
        0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 4, 8, 16, 0, 0,
    ];

    internal static readonly byte[] MultiRetrigMultiply = [
        1, 1, 1, 1, 1, 1, 2, 1, 1, 1, 1, 1, 1, 1, 3, 2,
    ];

    internal static readonly byte[] ModPeriodTable = [
        106, 100, 94, 89, 84, 79, 75, 70, 66, 63, 59,
    ];

    private static uint _randomState;

    internal static sbyte Waveform(int waveform, byte step)
    {
        waveform &= 127;
        switch (waveform)
        {
            case XmConst.SquareWaveform:
                return step < 0x80 ? sbyte.MinValue : sbyte.MaxValue;

            case XmConst.RampDownWaveform:
                return (sbyte)(-step - 1);

            case XmConst.RampUpWaveform:
                return (sbyte)step;

            case XmConst.RandomWaveform:
                return unchecked((sbyte)Random16());

            default:
            {
                var shifted = step >> 2;
                var index = (shifted & 0x10) != 0 ? 0xF - (shifted & 0xF) : shifted & 0xF;
                return (sbyte)(shifted < 0x20 ? -Sine[index] : Sine[index]);
            }
        }
    }

    private static ushort Random16()
    {
        _randomState = unchecked((_randomState * 0xD9F5) + 1);
        return unchecked((ushort)_randomState);
    }
}
