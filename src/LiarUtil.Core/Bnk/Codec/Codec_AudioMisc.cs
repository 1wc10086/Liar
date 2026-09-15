namespace LiarUtil.Core.Bnk.Codec;

using LiarUtil.Core.Bnk.Model;

internal static partial class BankCodecImpl
{
    public static void Section(BankContext c, AudioPlaybackPrioritySetting playbackPriorityValue, ref bool playbackPriorityOverride)
    {
        if (c.Version.In(72, 112))
        {
            var bits43 = c.Bits8();
            bits43.Bit(ref playbackPriorityOverride);
            bits43.Done();
            var bits44 = c.Bits8();
            bits44.Bit(ref playbackPriorityValue.UseDistanceFactor);
            bits44.Done();
        }
        if (c.Version.AtLeast(112))
        {
            var bits45 = c.Bits8();
            bits45.Bit(ref playbackPriorityOverride);
            bits45.Bit(ref playbackPriorityValue.UseDistanceFactor);
            bits45.Done();
        }
    }

    public static void Section(BankContext c, AudioPlaybackLimitSetting playbackLimitValue, ref bool playbackLimitOverride)
    {
        if (c.Version.In(72, 112))
        {
            var bits46 = c.Bits8();
            bits46.Enum(ref playbackLimitValue.WhenPriorityIsEqual, AudioPlaybackLimitSettingWhenPriorityIsEqualWire.Instance);
            bits46.Done();
        }
        if (c.Version.In(72, 112))
        {
            var bits47 = c.Bits8();
            bits47.Enum(ref playbackLimitValue.WhenLimitIsReached, AudioPlaybackLimitSettingWhenLimitIsReachedWire.Instance);
            bits47.Done();
        }
        if (c.Version.In(72, 112))
        {
            c.U16(ref playbackLimitValue.Value.Value);
        }
        if (c.Version.In(72, 112))
        {
            var bits48 = c.Bits8();
            bits48.Bit(ref playbackLimitOverride);
            bits48.Done();
        }
    }

    public static void Section(BankContext c, AudioPlaybackLimitSetting playbackLimitValue, AudioBusMuteForBackgroundMusic muteForBackgroundMusicValue, ref bool playbackLimitOverride)
    {
        if (c.Version.AtLeast(112))
        {
            var bits49 = c.Bits8();
            bits49.Enum(ref playbackLimitValue.WhenPriorityIsEqual, AudioPlaybackLimitSettingWhenPriorityIsEqualWire.Instance);
            bits49.Enum(ref playbackLimitValue.WhenLimitIsReached, AudioPlaybackLimitSettingWhenLimitIsReachedWire.Instance);
            bits49.Bit(ref playbackLimitOverride);
            bits49.Bit(ref muteForBackgroundMusicValue.Enable);
            bits49.Done();
        }
        if (c.Version.AtLeast(112))
        {
            c.U16(ref playbackLimitValue.Value.Value);
        }
    }

    public static void Section(BankContext c, AudioPlaybackLimitSetting playbackLimitValue, AudioVirtualVoiceSetting virtualVoiceValue, ref bool playbackLimitOverride, ref bool virtualVoiceOverride)
    {
        if (c.Version.In(72, 112))
        {
            var bits50 = c.Bits8();
            bits50.Enum(ref virtualVoiceValue.OnReturnToPhysical, AudioVirtualVoiceSettingOnReturnToPhysicalWire.Instance);
            bits50.Done();
        }
        if (c.Version.In(72, 112))
        {
            var bits51 = c.Bits8();
            bits51.Enum(ref playbackLimitValue.WhenPriorityIsEqual, AudioPlaybackLimitSettingWhenPriorityIsEqualWire.Instance);
            bits51.Done();
        }
        if (c.Version.In(72, 112))
        {
            var bits52 = c.Bits8();
            bits52.Enum(ref playbackLimitValue.WhenLimitIsReached, AudioPlaybackLimitSettingWhenLimitIsReachedWire.Instance);
            bits52.Done();
        }
        if (c.Version.In(72, 112))
        {
            c.U16(ref playbackLimitValue.Value.Value);
        }
        if (c.Version.In(72, 112))
        {
            var bits53 = c.Bits8();
            bits53.Enum(ref playbackLimitValue.Scope, AudioPlaybackLimitSettingScopeWire.Instance);
            bits53.Done();
        }
        if (c.Version.In(72, 112))
        {
            var bits54 = c.Bits8();
            bits54.Enum(ref virtualVoiceValue.Behavior, AudioVirtualVoiceSettingBehaviorWire.Instance);
            bits54.Done();
        }
        if (c.Version.In(72, 112))
        {
            var bits55 = c.Bits8();
            bits55.Bit(ref playbackLimitOverride);
            bits55.Done();
        }
        if (c.Version.In(72, 112))
        {
            var bits56 = c.Bits8();
            bits56.Bit(ref virtualVoiceOverride);
            bits56.Done();
        }
        if (c.Version.AtLeast(112))
        {
            var bits57 = c.Bits8();
            bits57.Enum(ref playbackLimitValue.WhenPriorityIsEqual, AudioPlaybackLimitSettingWhenPriorityIsEqualWire.Instance);
            bits57.Enum(ref playbackLimitValue.WhenLimitIsReached, AudioPlaybackLimitSettingWhenLimitIsReachedWire.Instance);
            bits57.Enum(ref playbackLimitValue.Scope, AudioPlaybackLimitSettingScopeWire.Instance);
            bits57.Bit(ref playbackLimitOverride);
            bits57.Bit(ref virtualVoiceOverride);
            bits57.Done();
        }
        if (c.Version.AtLeast(112))
        {
            var bits58 = c.Bits8();
            bits58.Enum(ref virtualVoiceValue.OnReturnToPhysical, AudioVirtualVoiceSettingOnReturnToPhysicalWire.Instance);
            bits58.Done();
        }
        if (c.Version.AtLeast(112))
        {
            c.U16(ref playbackLimitValue.Value.Value);
        }
        if (c.Version.AtLeast(112))
        {
            var bits59 = c.Bits8();
            bits59.Enum(ref virtualVoiceValue.Behavior, AudioVirtualVoiceSettingBehaviorWire.Instance);
            bits59.Done();
        }
    }

    public static void Section(BankContext c, AudioSourceSetting value)
    {
        if (c.Version.AtLeast(72))
        {
            c.Id(ref value.PlugIn);
        }
        if (c.Version.In(72, 112))
        {
            var bits63 = c.Bits32();
            bits63.Enum(ref value.Type, AudioSourceTypeWire.Instance);
            bits63.Done();
        }
        if (c.Version.AtLeast(112))
        {
            var bits64 = c.Bits8();
            bits64.Enum(ref value.Type, AudioSourceTypeWire.Instance);
            bits64.Done();
        }
        if (c.Version.AtLeast(72))
        {
            c.Id(ref value.Resource);
        }
        if (c.Version.In(72, 113))
        {
            c.Id(ref value.Source);
        }
        if (c.Version.In(72, 113))
        {
            if (value.Type != AudioSourceType.Streamed)
            {
                c.U32(ref value.ResourceOffset);
            }
        }
        if (c.Version.In(72, 112))
        {
            if (value.Type != AudioSourceType.Streamed)
            {
                if (c.Version.AtLeast(72))
                {
                    c.U32(ref value.ResourceSize);
                }
            }
        }
        if (c.Version.AtLeast(112))
        {
            c.U32(ref value.ResourceSize);
        }
        if (c.Version.In(72, 112))
        {
            var bits65 = c.Bits8();
            bits65.Bit(ref value.IsVoice);
            bits65.Done();
        }
        if (c.Version.AtLeast(112))
        {
            var bits66 = c.Bits8();
            bits66.Bit(ref value.IsVoice);
            bits66.Const(false);
            bits66.Const(false);
            bits66.Bit(ref value.NonCachableStream);
            bits66.Done();
        }
        if (c.Version.AtLeast(72))
        {
            if ((value.PlugIn & 0x0000FFFF) >= 0x0002)
            {
                c.Const32(0u);
            }
        }
    }

    public static void Section(BankContext c, List<AudioSourceSetting> value)
    {
        if (c.Version.AtLeast(72))
        {
            c.List(value, SizeKind.U32,
    static (BankContext ctx16, ref AudioSourceSetting item16) =>
            {
                Section(ctx16, item16);
            }
            );
        }
    }

    public static void Section(BankContext c, AudioTimeSetting timeSettingValue, ref bool timeSettingOverride)
    {
        if (c.Version.AtLeast(72))
        {
            c.F64(ref timeSettingValue.Time);
        }
        if (c.Version.AtLeast(72))
        {
            c.F64(ref timeSettingValue.Offset);
        }
        if (c.Version.AtLeast(72))
        {
            c.F32(ref timeSettingValue.Tempo);
        }
        if (c.Version.AtLeast(72))
        {
            c.U8(ref timeSettingValue.Signature.First);
        }
        if (c.Version.AtLeast(72))
        {
            c.U8(ref timeSettingValue.Signature.Second);
        }
        if (c.Version.In(72, 140))
        {
            var b2 = false;
            var b3 = false;
            var b4 = false;
            var b5 = false;
            var b6 = false;
            var b7 = false;
            var b8 = false;
            var bits72 = c.Bits8();
            bits72.Bit(ref timeSettingOverride);
            bits72.Bit(ref b2);
            bits72.Bit(ref b3);
            bits72.Bit(ref b4);
            bits72.Bit(ref b5);
            bits72.Bit(ref b6);
            bits72.Bit(ref b7);
            bits72.Bit(ref b8);
            bits72.Done();
            // assert b2 == b3 && b3 == b4 && b4 == b5 && b5 == b6 && b6 == b7 && b7 == b8
        }
        if (c.Version.AtLeast(140))
        {
            var bits73 = c.Bits8();
            bits73.Bit(ref timeSettingOverride);
            bits73.Done();
        }
    }

    public static void Section(BankContext c, AudioSwitcherSetting switcherValue)
    {
        if (c.Version.In(72, 112))
        {
            var bits92 = c.Bits32();
            bits92.Bit(ref switcherValue.IsState);
            bits92.Done();
        }
        if (c.Version.AtLeast(112))
        {
            var bits93 = c.Bits8();
            bits93.Bit(ref switcherValue.IsState);
            bits93.Done();
        }
        if (c.Version.AtLeast(72))
        {
            c.Id(ref switcherValue.Group);
        }
        if (c.Version.AtLeast(72))
        {
            c.Id(ref switcherValue.DefaultItem);
        }
    }

    public static void Section(BankContext c, AudioAssociationSetting associationValue)
    {
        c.List(associationValue.Argument, SizeKind.U32, static (BankContext ctx, ref AudioAssociationSettingArgument item) =>
        {
            ctx.Id(ref item.Identifier);
            if (ctx.Version.AtLeast(88))
            {
                var bits = ctx.Bits8();
                bits.Bit(ref item.IsState);
                bits.Done();
            }
        });
        if (c.Reading)
        {
            var total = (int)c.Reader.ReadUInt32();
            var count = total / 12;
            if (c.Version.In(72, 88))
            {
                c.U8(ref associationValue.Probability);
            }
            var mode = c.Bits8();
            mode.Enum(ref associationValue.Mode, AudioAssociationSettingModeWire.Instance);
            mode.Done();
            associationValue.Path.Clear();
            for (var i = 0; i < count; i++)
            {
                var item = new AudioAssociationSettingPath();
                c.Id(ref item.U1);
                c.Id(ref item.Object);
                c.U16(ref item.Weight);
                c.U16(ref item.Probability);
                associationValue.Path.Add(item);
            }
        }
        else
        {
            c.Writer.WriteUInt32((uint)(associationValue.Path.Count * 12));
            if (c.Version.In(72, 88))
            {
                c.U8(ref associationValue.Probability);
            }
            var mode = c.Bits8();
            mode.Enum(ref associationValue.Mode, AudioAssociationSettingModeWire.Instance);
            mode.Done();
            foreach (var item in associationValue.Path)
            {
                c.Id(ref item.U1);
                c.Id(ref item.Object);
                c.U16(ref item.Weight);
                c.U16(ref item.Probability);
            }
        }
    }
}
