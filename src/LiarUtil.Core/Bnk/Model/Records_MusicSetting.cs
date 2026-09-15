namespace LiarUtil.Core.Bnk.Model;

internal sealed class MusicTransitionSettingItemFade
{
    public long Time = 0L;
    public long Curve = 0L;
    public long Offset = 0L;
}

internal sealed class MusicTransitionSettingItemSource
{
    public long Identifier = 0L;
    public TimePoint ExitSourceAt = default;
    public long ExitSourceAtCustomCueMatch = 0L;
    public bool PlayPostExit = false;
    public MusicTransitionSettingItemFade FadeOut = new();
}

internal sealed class MusicTransitionSettingItemDestination
{
    public long Identifier = 0L;
    public MusicTransitionSettingSynchronizeMode SynchronizeTo = default;
    public bool PlayPreEntry = false;
    public bool CustomCueFilterMatchSourceCueName = false;
    public long CustomCueFilterMatchTarget = 0L;
    public MusicTransitionSettingItemFade FadeIn = new();
    public MusicTransitionSettingJumpMode JumpTo = default;
}

internal sealed class MusicTransitionSettingItemSegment
{
    public bool Enable = false;
    public long Identifier = 0L;
    public bool PlayPreEntry = false;
    public MusicTransitionSettingItemFade FadeIn = new();
    public bool PlayPostExit = false;
    public MusicTransitionSettingItemFade FadeOut = new();
}

internal sealed class MusicTransitionSettingItem
{
    public long U1 = 0L;
    public MusicTransitionSettingItemSource Source = new();
    public MusicTransitionSettingItemDestination Destination = new();
    public MusicTransitionSettingItemSegment Segment = new();
}

internal sealed class MusicTransitionSetting
{
    public List<MusicTransitionSettingItem> Item = [];
}

internal sealed class MusicTrackTransitionSettingItemSource
{
    public TimePoint ExitSourceAt = default;
    public long ExitSourceAtCustomCueMatch = 0L;
    public MusicTransitionSettingItemFade FadeOut = new();
}

internal sealed class MusicTrackTransitionSettingItemDestination
{
    public MusicTransitionSettingItemFade FadeIn = new();
}

internal sealed class MusicTrackTransitionSetting
{
    public long Switcher = 0L;
    public MusicTrackTransitionSettingItemSource Source = new();
    public MusicTrackTransitionSettingItemDestination Destination = new();
}

internal sealed class MusicStingerSettingItem
{
    public long Trigger = 0L;
    public long SegmentToPlay = 0L;
    public TimePoint PlayAt = default;
    public long CueName = 0L;
    public long DoNotPlayThisStingerAgainFor = 0L;
    public bool AllowPlayingStingerInNextSegment = false;
}

internal sealed class MusicStingerSetting
{
    public List<MusicStingerSettingItem> Item = [];
}

internal sealed class BusHdrSettingWindowTopOutputGameParameter
{
    public long Identifier = 0L;
    public double Minimum = 0d;
    public double Maximum = 0d;
}

internal sealed class BusHdrSettingDynamic
{
    public double Threshold = 0d;
    public double Ratio = 0d;
    public double ReleaseTime = 0d;
    public BusHdrSettingDynamicReleaseMode ReleaseMode = default;
}

internal sealed class BusHdrSetting
{
    public bool Enable = false;
    public bool U1 = false;
    public BusHdrSettingDynamic Dynamic = new();
    public BusHdrSettingWindowTopOutputGameParameter WindowTopOutputGameParameter = new();
}

internal sealed class AudioHdrSettingEnvelopeTracking
{
    public bool Enable = false;
    public double ActiveRange = 12d;
}

internal sealed class AudioHdrSetting
{
    public AudioHdrSettingEnvelopeTracking EnvelopeTracking = new();
}

internal sealed class SoundMidiSettingEvent
{
    public SoundMidiSettingEventPlayOn PlayOn = default;
    public bool BreakOnNoteOff = false;
}

internal sealed class SoundMidiSettingNoteTracking
{
    public bool Enable = false;
    public long RootNote = 60L;
}

internal sealed class SoundMidiSettingTransformation
{
    public RegularValue<long> Transposition = new();
    public RegularValue<long> VelocityOffset = new();
}

internal sealed class SoundMidiSettingFilter
{
    public long KeyRangeMinimum = 0L;
    public long KeyRangeMaximum = 127L;
    public long VelocityMinimum = 0L;
    public long VelocityMaximum = 127L;
    public long Channel = 65535L;
}

internal sealed class SoundMidiSetting
{
    public SoundMidiSettingEvent Event = new();
    public SoundMidiSettingNoteTracking NoteTracking = new();
    public SoundMidiSettingTransformation Transformation = new();
    public SoundMidiSettingFilter Filter = new();
}

internal sealed class MusicMidiSettingTarget
{
    public long Identifier = 0L;
}

internal sealed class MusicMidiSettingClipTempo
{
    public MusicMidiSettingClipTempoSource Source = default;
}

internal sealed class MusicMidiSetting
{
    public MusicMidiSettingTarget Target = new();
    public MusicMidiSettingClipTempo ClipTempo = new();
}
