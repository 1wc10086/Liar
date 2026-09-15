namespace LiarUtil.Core.Bnk.Codec;

using LiarUtil.Core.Bnk.Model;

internal static partial class BankCodecImpl
{
    public static void Section(BankContext c, AudioOutputBusSetting outputBusValue)
    {
        if (c.Version.AtLeast(72))
        {
            c.Id(ref outputBusValue.Bus);
        }
    }

    public static void Section(BankContext c, AudioAuxiliarySendSetting auxiliarySendValue, ref bool gameDefinedAuxiliarySendOverride, ref bool userDefinedAuxiliarySendOverride)
    {
        if (c.Version.In(72, 112))
        {
            var bits15 = c.Bits8();
            bits15.Bit(ref gameDefinedAuxiliarySendOverride);
            bits15.Done();
            var bits16 = c.Bits8();
            bits16.Bit(ref auxiliarySendValue.GameDefined.Enable);
            bits16.Done();
            var bits17 = c.Bits8();
            bits17.Bit(ref userDefinedAuxiliarySendOverride);
            bits17.Done();
            var bits18 = c.Bits8();
            bits18.Bit(ref auxiliarySendValue.UserDefined.Enable);
            bits18.Done();
        }
        if (c.Version.In(112, 135))
        {
            var bits19 = c.Bits8();
            bits19.Bit(ref gameDefinedAuxiliarySendOverride);
            bits19.Bit(ref auxiliarySendValue.GameDefined.Enable);
            bits19.Bit(ref userDefinedAuxiliarySendOverride);
            bits19.Bit(ref auxiliarySendValue.UserDefined.Enable);
            bits19.Done();
        }
        if (c.Version.In(72, 135))
        {
            if (auxiliarySendValue.UserDefined.Enable)
            {
                if (c.Version.In(72, 135))
                {
                    c.Id(ref auxiliarySendValue.UserDefined.Item1.Bus);
                }
                if (c.Version.In(72, 135))
                {
                    c.Id(ref auxiliarySendValue.UserDefined.Item2.Bus);
                }
                if (c.Version.In(72, 135))
                {
                    c.Id(ref auxiliarySendValue.UserDefined.Item3.Bus);
                }
                if (c.Version.In(72, 135))
                {
                    c.Id(ref auxiliarySendValue.UserDefined.Item4.Bus);
                }
            }
        }
    }

    public static void Section(BankContext c, AudioAuxiliarySendSetting auxiliarySendValue, ref bool gameDefinedAuxiliarySendOverride, ref bool userDefinedAuxiliarySendOverride, ref bool earlyReflectionAuxiliarySendOverride)
    {
        if (c.Version.AtLeast(135))
        {
            var bits20 = c.Bits8();
            bits20.Bit(ref gameDefinedAuxiliarySendOverride);
            bits20.Bit(ref auxiliarySendValue.GameDefined.Enable);
            bits20.Bit(ref userDefinedAuxiliarySendOverride);
            bits20.Bit(ref auxiliarySendValue.UserDefined.Enable);
            bits20.Bit(ref earlyReflectionAuxiliarySendOverride);
            bits20.Done();
        }
        if (c.Version.AtLeast(135))
        {
            if (auxiliarySendValue.UserDefined.Enable)
            {
                if (c.Version.AtLeast(135))
                {
                    c.Id(ref auxiliarySendValue.UserDefined.Item1.Bus);
                }
                if (c.Version.AtLeast(135))
                {
                    c.Id(ref auxiliarySendValue.UserDefined.Item2.Bus);
                }
                if (c.Version.AtLeast(135))
                {
                    c.Id(ref auxiliarySendValue.UserDefined.Item3.Bus);
                }
                if (c.Version.AtLeast(135))
                {
                    c.Id(ref auxiliarySendValue.UserDefined.Item4.Bus);
                }
            }
        }
        if (c.Version.AtLeast(135))
        {
            c.Id(ref auxiliarySendValue.EarlyReflection.Bus);
        }
    }
}
