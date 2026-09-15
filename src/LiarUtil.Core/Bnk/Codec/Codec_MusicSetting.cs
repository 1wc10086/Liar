namespace LiarUtil.Core.Bnk.Codec;

using LiarUtil.Core.Bnk.Model;

internal static partial class BankCodecImpl
{
    public static void Section(BankContext c, MusicMidiSetting midiValue, ref bool midiTargetOverride, ref bool midiClipTempoOverride)
    {
        if (c.Version.AtLeast(112))
        {
            var bits41 = c.Bits8();
            bits41.Const(false);
            bits41.Bit(ref midiClipTempoOverride);
            bits41.Bit(ref midiTargetOverride);
            bits41.Done();
        }
    }

    public static void Section(BankContext c, SoundMidiSetting midiValue, AudioPlaybackPrioritySetting playbackPriorityValue, ref bool midiEventOverride, ref bool midiNoteTrackingOverride, ref bool playbackPriorityOverride)
    {
        if (c.Version.AtLeast(112))
        {
            var bits42 = c.Bits8();
            bits42.Bit(ref playbackPriorityOverride);
            bits42.Bit(ref playbackPriorityValue.UseDistanceFactor);
            bits42.Bit(ref midiEventOverride);
            bits42.Bit(ref midiNoteTrackingOverride);
            bits42.Bit(ref midiValue.NoteTracking.Enable);
            bits42.Bit(ref midiValue.Event.BreakOnNoteOff);
            bits42.Done();
        }
    }

    public static void Section(BankContext c, BusHdrSetting hdrValue)
    {
        if (c.Version.In(88, 112))
        {
            var bits69 = c.Bits8();
            bits69.Bit(ref hdrValue.Enable);
            bits69.Done();
            var bits70 = c.Bits8();
            bits70.Enum(ref hdrValue.Dynamic.ReleaseMode, BusHdrSettingDynamicReleaseModeWire.Instance);
            bits70.Done();
        }
        if (c.Version.AtLeast(112))
        {
            var bits71 = c.Bits8();
            bits71.Bit(ref hdrValue.Enable);
            bits71.Enum(ref hdrValue.Dynamic.ReleaseMode, BusHdrSettingDynamicReleaseModeWire.Instance);
            bits71.Done();
        }
    }

    public static void Section(BankContext c, MusicStingerSetting stingerValue)
    {
        if (c.Version.AtLeast(72))
        {
            c.List(stingerValue.Item, SizeKind.U32,
    static (BankContext ctx21, ref MusicStingerSettingItem item21) =>
            {
                if (ctx21.Version.AtLeast(72))
                {
                    ctx21.Id(ref item21.Trigger);
                }
                if (ctx21.Version.AtLeast(72))
                {
                    ctx21.Id(ref item21.SegmentToPlay);
                }
                if (ctx21.Version.AtLeast(72))
                {
                    var bits76 = ctx21.Bits32();
                    bits76.Enum(ref item21.PlayAt, TimePointWire.Instance);
                    bits76.Done();
                }
                if (ctx21.Version.AtLeast(72))
                {
                    ctx21.Id(ref item21.CueName);
                }
                if (ctx21.Version.AtLeast(72))
                {
                    ctx21.U32(ref item21.DoNotPlayThisStingerAgainFor);
                }
                if (ctx21.Version.AtLeast(72))
                {
                    var bits77 = ctx21.Bits32();
                    bits77.Bit(ref item21.AllowPlayingStingerInNextSegment);
                    bits77.Done();
                }
            }
            );
        }
    }

    public static void Section(BankContext c, MusicTransitionSettingItemFade fadeValue)
    {
        if (c.Version.AtLeast(72))
        {
            c.U32(ref fadeValue.Time);
        }
        if (c.Version.AtLeast(72))
        {
            c.U32(ref fadeValue.Curve);
        }
        if (c.Version.AtLeast(72))
        {
            c.S32(ref fadeValue.Offset);
        }
    }

    public static void Section(BankContext c, MusicTransitionSetting transitionValue)
    {
        if (c.Version.AtLeast(72))
        {
            c.List(transitionValue.Item, SizeKind.U32,
    static (BankContext ctx22, ref MusicTransitionSettingItem item22) =>
            {
                if (ctx22.Version.AtLeast(88))
                {
                    ctx22.Const32(1u);
                }
                if (ctx22.Version.AtLeast(72))
                {
                    ctx22.Id(ref item22.Source.Identifier);
                }
                if (ctx22.Version.AtLeast(88))
                {
                    ctx22.Const32(1u);
                }
                if (ctx22.Version.AtLeast(72))
                {
                    ctx22.Id(ref item22.Destination.Identifier);
                }
                if (ctx22.Version.AtLeast(72))
                {
                    Section(ctx22, item22.Source.FadeOut);
                }
                if (ctx22.Version.AtLeast(72))
                {
                    var bits78 = ctx22.Bits32();
                    bits78.Enum(ref item22.Source.ExitSourceAt, TimePointWire.Instance);
                    bits78.Done();
                }
                if (ctx22.Version.AtLeast(72))
                {
                    ctx22.Id(ref item22.Source.ExitSourceAtCustomCueMatch);
                }
                if (ctx22.Version.In(72, 140))
                {
                    var bits79 = ctx22.Bits8();
                    bits79.Bit(ref item22.Source.PlayPostExit);
                    bits79.Done(true);
                }
                if (ctx22.Version.AtLeast(140))
                {
                    var bits80 = ctx22.Bits8();
                    bits80.Bit(ref item22.Source.PlayPostExit);
                    bits80.Done();
                }
                if (ctx22.Version.AtLeast(72))
                {
                    Section(ctx22, item22.Destination.FadeIn);
                }
                if (ctx22.Version.AtLeast(72))
                {
                    ctx22.Id(ref item22.Destination.CustomCueFilterMatchTarget);
                }
                if (ctx22.Version.AtLeast(72))
                {
                    ctx22.Id(ref item22.U1);
                }
                if (ctx22.Version.AtLeast(134))
                {
                    var bits81 = ctx22.Bits16();
                    bits81.Enum(ref item22.Destination.JumpTo, MusicTransitionSettingJumpModeWire.Instance);
                    bits81.Done();
                }
                if (ctx22.Version.AtLeast(72))
                {
                    var bits82 = ctx22.Bits16();
                    bits82.Enum(ref item22.Destination.SynchronizeTo, MusicTransitionSettingSynchronizeModeWire.Instance);
                    bits82.Done();
                }
                if (ctx22.Version.In(72, 140))
                {
                    var bits83 = ctx22.Bits8();
                    bits83.Bit(ref item22.Destination.PlayPreEntry);
                    bits83.Done(true);
                }
                if (ctx22.Version.AtLeast(140))
                {
                    var bits84 = ctx22.Bits8();
                    bits84.Bit(ref item22.Destination.PlayPreEntry);
                    bits84.Done();
                }
                if (ctx22.Version.AtLeast(72))
                {
                    var bits85 = ctx22.Bits8();
                    bits85.Bit(ref item22.Destination.CustomCueFilterMatchSourceCueName);
                    bits85.Done();
                }
                if (ctx22.Version.AtLeast(72))
                {
                    var bits86 = ctx22.Bits8();
                    bits86.Bit(ref item22.Segment.Enable);
                    bits86.Done();
                }
                if (ctx22.Version.AtLeast(72))
                {
                    var hasSegmentData = false;
                    if (ctx22.Version.In(72, 88))
                    {
                        hasSegmentData = true;
                    }
                    if (ctx22.Version.AtLeast(88))
                    {
                        hasSegmentData = item22.Segment.Enable;
                    }
                    if (hasSegmentData)
                    {
                        if (ctx22.Version.AtLeast(72))
                        {
                            ctx22.Id(ref item22.Segment.Identifier);
                        }
                        if (ctx22.Version.AtLeast(72))
                        {
                            Section(ctx22, item22.Segment.FadeIn);
                        }
                        if (ctx22.Version.AtLeast(72))
                        {
                            Section(ctx22, item22.Segment.FadeOut);
                        }
                        if (ctx22.Version.In(72, 140))
                        {
                            var bits87 = ctx22.Bits8();
                            bits87.Bit(ref item22.Segment.PlayPreEntry);
                            bits87.Done(true);
                        }
                        if (ctx22.Version.AtLeast(140))
                        {
                            var bits88 = ctx22.Bits8();
                            bits88.Bit(ref item22.Segment.PlayPreEntry);
                            bits88.Done();
                        }
                        if (ctx22.Version.In(72, 140))
                        {
                            var bits89 = ctx22.Bits8();
                            bits89.Bit(ref item22.Segment.PlayPostExit);
                            bits89.Done(true);
                        }
                        if (ctx22.Version.AtLeast(140))
                        {
                            var bits90 = ctx22.Bits8();
                            bits90.Bit(ref item22.Segment.PlayPreEntry);
                            bits90.Done();
                        }
                    }
                }
            }
            );
        }
    }

    public static void Section(BankContext c, MusicTrackTransitionSetting transitionValue)
    {
        if (c.Version.AtLeast(112))
        {
            c.Const32(1u);
        }
        if (c.Version.AtLeast(112))
        {
            c.Id(ref transitionValue.Switcher);
        }
        if (c.Version.AtLeast(112))
        {
            Section(c, transitionValue.Source.FadeOut);
        }
        if (c.Version.AtLeast(112))
        {
            var bits91 = c.Bits32();
            bits91.Enum(ref transitionValue.Source.ExitSourceAt, TimePointWire.Instance);
            bits91.Done();
        }
        if (c.Version.AtLeast(112))
        {
            c.Id(ref transitionValue.Source.ExitSourceAtCustomCueMatch);
        }
        if (c.Version.AtLeast(112))
        {
            Section(c, transitionValue.Destination.FadeIn);
        }
    }
}
