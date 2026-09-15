namespace LiarUtil.Core.Bnk.Codec;

using LiarUtil.Core.Bnk.Model;

internal static partial class BankCodecImpl
{
    public static void Section(BankContext c, AudioBusConfiguration busConfigurationValue)
    {
        if (c.Version.AtLeast(88))
        {
            c.U32(ref busConfigurationValue.U1);
        }
    }

    public static void Section(BankContext c, AudioDevice value)
    {
        c.Id(ref value.Identifier);
        if (c.Version.AtLeast(128))
        {
            c.Id(ref value.PlugIn);
        }
        if (c.Version.AtLeast(128))
        {
            c.Data(ref value.Expand, SizeKind.U32);
        }
        if (c.Version.AtLeast(128))
        {
            c.Const8(0);
        }
        if (c.Version.AtLeast(128))
        {
            Section(c, value.RealTimeParameterControl);
        }
        if (c.Version.AtLeast(128))
        {
            Section(c, value.State);
        }
        if (c.Version.AtLeast(128))
        {
            Section(c, value.U1);
        }
        if (c.Version.AtLeast(140))
        {
            Section(c, value.Effect);
        }
    }

    public static void Section(BankContext c, AudioBus value)
    {
        c.Id(ref value.Identifier);
        if (c.Version.AtLeast(72))
        {
            c.Id(ref value.Parent);
        }
        if (c.Version.AtLeast(128))
        {
            if (value.Parent == 0)
            {
                if (c.Version.AtLeast(128))
                {
                    c.Id(ref value.AudioDevice);
                }
            }
        }
        if (c.Version.AtLeast(72))
        {
            {
                var commonProperty = new CommonPropertyMap<AudioCommonPropertyType>();
                void ApplyCommonProperties()
                {
                if (c.Version.AtLeast(72))
                {
                    LoadProperty(c, commonProperty, value.Voice);
                }
                if (c.Version.AtLeast(125))
                {
                    LoadProperty(c, commonProperty, value.VoiceVolumeGain);
                }
                if (c.Version.AtLeast(72))
                {
                    LoadProperty(c, commonProperty, value.Bus);
                }
                if (c.Version.AtLeast(128))
                {
                    LoadProperty(c, commonProperty, value.OutputBus);
                }
                if (c.Version.AtLeast(125))
                {
                    LoadProperty(c, commonProperty, value.AuxiliarySend);
                }
                if (c.Version.AtLeast(88))
                {
                    LoadProperty(c, commonProperty, value.Positioning);
                }
                if (c.Version.AtLeast(88))
                {
                    LoadProperty(c, commonProperty, value.Hdr);
                }
                if (c.Version.AtLeast(72))
                {
                    LoadProperty(c, commonProperty, value.PlaybackLimit);
                }
                }
                if (!c.Reading)
                {
                    ApplyCommonProperties();
                }
                CommonProperty.Read(c, commonProperty, AudioCommonPropertyTypeTable.Instance, false);
                if (c.Reading)
                {
                    ApplyCommonProperties();
                }
            }

        }
        if (c.Version.In(88, 112))
        {
            var bits122 = c.Bits8();
            bits122.Bit(ref value.OverridePositioning);
            bits122.Done();
            var bits123 = c.Bits8();
            bits123.Bit(ref value.Positioning.SpeakerPanning.Enable);
            bits123.Done();
        }
        if (c.Version.In(112, 125))
        {
            var bits124 = c.Bits8();
            bits124.Bit(ref value.OverridePositioning);
            bits124.Bit(ref value.Positioning.SpeakerPanning.Enable);
            bits124.Done();
        }
        if (c.Version.AtLeast(125))
        {
            var overridePositioning = false;
            Section(c, value.Positioning, ref overridePositioning);
            // assert overridePositioning
        }
        if (c.Version.In(125, 135))
        {
            var overrideGameDefinedAuxiliarySend = false;
            var overrideUserDefinedAuxiliarySend = false;
            Section(c, value.AuxiliarySend, ref overrideGameDefinedAuxiliarySend, ref overrideUserDefinedAuxiliarySend);
            // assert overrideGameDefinedAuxiliarySend
            // assert overrideUserDefinedAuxiliarySend
        }
        if (c.Version.AtLeast(135))
        {
            var overrideGameDefinedAuxiliarySend = false;
            var overrideUserDefinedAuxiliarySend = false;
            var overrideEarlyReflectionAuxiliarySend = false;
            Section(c, value.AuxiliarySend, ref overrideGameDefinedAuxiliarySend, ref overrideUserDefinedAuxiliarySend, ref overrideEarlyReflectionAuxiliarySend);
            // assert overrideGameDefinedAuxiliarySend
            // assert overrideUserDefinedAuxiliarySend
            // assert overrideEarlyReflectionAuxiliarySend
        }
        if (c.Version.In(72, 112))
        {
            Section(c, value.PlaybackLimit, ref value.OverridePlaybackLimit);
        }
        if (c.Version.AtLeast(112))
        {
            Section(c, value.PlaybackLimit, value.MuteForBackgroundMusic, ref value.OverridePlaybackLimit);
        }
        if (c.Version.AtLeast(88))
        {
            Section(c, value.BusConfiguration);
        }
        if (c.Version.AtLeast(88))
        {
            Section(c, value.Hdr);
        }
        if (c.Version.In(72, 88))
        {
            c.Const32(63u);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.AutomaticDucking);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.Effect);
        }
        if (c.Version.In(112, 150))
        {
            c.Id(ref value.Mixer);
        }
        if (c.Version.In(112, 150))
        {
            c.Const16(0);
        }
        if (c.Version.AtLeast(140))
        {
            Section(c, value.Metadata);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.RealTimeParameterControl);
        }
        if (c.Version.AtLeast(72))
        {
            Section(c, value.State);
        }
    }
}
