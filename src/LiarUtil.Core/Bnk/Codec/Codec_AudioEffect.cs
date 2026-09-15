namespace LiarUtil.Core.Bnk.Codec;

using LiarUtil.Core.Bnk.Model;

internal static partial class BankCodecImpl
{
    public static void Section(BankContext c, AudioEffectSetting effectValue)
    {
        if (c.Reading)
        {
            var count = (int)c.Reader.ReadUInt8();
            if (count > 0)
            {
                if (c.Version.In(72, 150))
                {
                    var bits = c.Bits8();
                    bits.Bit(ref effectValue.Bypass.Item1);
                    bits.Bit(ref effectValue.Bypass.Item2);
                    bits.Bit(ref effectValue.Bypass.Item3);
                    bits.Bit(ref effectValue.Bypass.Item4);
                    bits.Bit(ref effectValue.Bypass.Item5);
                    bits.Done();
                }
                else if (c.Version.AtLeast(150))
                {
                    var bits = c.Bits8();
                    bits.Bit(ref effectValue.Bypass.Item1);
                    bits.Done();
                }
            }
            effectValue.Item.Clear();
            for (var i = 0; i < count; i++)
            {
                var item = new AudioEffectSettingItem();
                c.U8(ref item.Index);
                c.Id(ref item.Identifier);
                if (c.Version.In(72, 150))
                {
                    var b0 = c.Bits8();
                    b0.Bit(ref item.UseShareSet);
                    b0.Done();
                    var b1 = c.Bits8();
                    b1.Bit(ref item.U1);
                    b1.Done();
                }
                else if (c.Version.AtLeast(150))
                {
                    var b0 = c.Bits8();
                    b0.Bit(ref item.Bypass);
                    b0.Bit(ref item.UseShareSet);
                    b0.Done();
                }
                effectValue.Item.Add(item);
            }
        }
        else
        {
            c.Writer.WriteUInt8((byte)effectValue.Item.Count);
            if (effectValue.Item.Count > 0)
            {
                if (c.Version.In(72, 150))
                {
                    var bits = c.Bits8();
                    bits.Bit(ref effectValue.Bypass.Item1);
                    bits.Bit(ref effectValue.Bypass.Item2);
                    bits.Bit(ref effectValue.Bypass.Item3);
                    bits.Bit(ref effectValue.Bypass.Item4);
                    bits.Bit(ref effectValue.Bypass.Item5);
                    bits.Done();
                }
                else if (c.Version.AtLeast(150))
                {
                    var bits = c.Bits8();
                    bits.Bit(ref effectValue.Bypass.Item1);
                    bits.Done();
                }
            }
            foreach (var item in effectValue.Item)
            {
                c.U8(ref item.Index);
                c.Id(ref item.Identifier);
                if (c.Version.In(72, 150))
                {
                    var b0 = c.Bits8();
                    b0.Bit(ref item.UseShareSet);
                    b0.Done();
                    var b1 = c.Bits8();
                    b1.Bit(ref item.U1);
                    b1.Done();
                }
                else if (c.Version.AtLeast(150))
                {
                    var b0 = c.Bits8();
                    b0.Bit(ref item.Bypass);
                    b0.Bit(ref item.UseShareSet);
                    b0.Done();
                }
            }
        }
    }

    public static void Section(BankContext c, AudioEffectSetting effectValue, ref bool effectOverride)
    {
        if (c.Version.AtLeast(72))
        {
            var bits60 = c.Bits8();
            bits60.Bit(ref effectOverride);
            bits60.Done();
        }
        Section(c, effectValue);
    }

    public static void Section(BankContext c, AudioMetadataSetting metadataValue)
    {
        if (c.Version.AtLeast(140))
        {
            c.List(metadataValue.Item, SizeKind.U8,
    static (BankContext ctx15, ref AudioMetadataSettingItem item15) =>
            {
                if (ctx15.Version.AtLeast(140))
                {
                    ctx15.U8(ref item15.Index);
                }
                if (ctx15.Version.AtLeast(140))
                {
                    ctx15.Id(ref item15.Identifier);
                }
                if (ctx15.Version.AtLeast(140))
                {
                    var bits61 = ctx15.Bits8();
                    bits61.Bit(ref item15.UseShareSet);
                    bits61.Done();
                }
            }
            );
        }
    }

    public static void Section(BankContext c, AudioMetadataSetting metadataValue, ref bool metadataOverride)
    {
        if (c.Version.AtLeast(140))
        {
            var bits62 = c.Bits8();
            bits62.Bit(ref metadataOverride);
            bits62.Done();
        }
        Section(c, metadataValue);
    }
}
