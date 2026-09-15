namespace LiarUtil.Core.Bnk.Codec;

using LiarUtil.Core.Bnk.Model;

internal static partial class BankCodecImpl
{
    public static void Section(BankContext c, AudioVoiceVolumeGainSetting voiceVolumeGainValue, AudioHdrSetting hdrValue, ref bool voiceVolumeLoudnessNormalizationOverride, ref bool hdrEnvelopeTrackingOverride)
    {
        if (c.Version.In(88, 112))
        {
            var bits9 = c.Bits8();
            bits9.Bit(ref hdrEnvelopeTrackingOverride);
            bits9.Done();
        }
        if (c.Version.In(88, 112))
        {
            var bits10 = c.Bits8();
            bits10.Bit(ref voiceVolumeLoudnessNormalizationOverride);
            bits10.Done();
        }
        if (c.Version.In(88, 112))
        {
            var bits11 = c.Bits8();
            bits11.Bit(ref voiceVolumeGainValue.Normalization);
            bits11.Done();
        }
        if (c.Version.In(88, 112))
        {
            var bits12 = c.Bits8();
            bits12.Bit(ref hdrValue.EnvelopeTracking.Enable);
            bits12.Done();
        }
        if (c.Version.AtLeast(112))
        {
            var bits13 = c.Bits8();
            bits13.Bit(ref hdrEnvelopeTrackingOverride);
            bits13.Bit(ref voiceVolumeLoudnessNormalizationOverride);
            bits13.Bit(ref voiceVolumeGainValue.Normalization);
            bits13.Bit(ref hdrValue.EnvelopeTracking.Enable);
            bits13.Done();
        }
    }

    public static void Section(BankContext c, AudioPlayMode playModeValue)
    {
        if (c.Version.AtLeast(72))
        {
            var bits113 = c.Bits8();
            bits113.Enum(ref playModeValue, AudioPlayModeWire.Instance);
            bits113.Done();
        }
    }
}
