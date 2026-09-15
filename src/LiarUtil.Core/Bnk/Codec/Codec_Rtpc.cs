namespace LiarUtil.Core.Bnk.Codec;

using LiarUtil.Core.Bnk.Model;

internal static partial class BankCodecImpl
{
    public static void Section(BankContext c, RealTimeParameterControlSetting realTimeParameterControlValue)
    {
        if (c.Version.AtLeast(72))
        {
            c.List(realTimeParameterControlValue.Item, SizeKind.U16,
    static (BankContext ctx1, ref RealTimeParameterControlSettingItem item1) =>
            {
                if (ctx1.Version.AtLeast(72))
                {
                    ctx1.Id(ref item1.Parameter.Identifier);
                }
                if (ctx1.Version.AtLeast(112))
                {
                    var bits1 = ctx1.Bits8();
                    bits1.Enum(ref item1.Parameter.Category, ParameterCategoryWire.Instance);
                    bits1.Done();
                }
                if (ctx1.Version.AtLeast(112))
                {
                    var bits2 = ctx1.Bits8();
                    bits2.Enum(ref item1.U1, PropertyCategoryWire.Instance);
                    bits2.Done();
                }
                if (ctx1.Version.In(72, 112))
                {
                    ctx1.U32(ref item1.Type);
                }
                if (ctx1.Version.AtLeast(112))
                {
                    ctx1.U8(ref item1.Type);
                }
                if (ctx1.Version.AtLeast(72))
                {
                    ctx1.Id(ref item1.U2);
                }
                if (ctx1.Version.AtLeast(72))
                {
                    var bits3 = ctx1.Bits8();
                    bits3.Enum(ref item1.Mode, CoordinateModeWire.Instance);
                    bits3.Done();
                }
                if (ctx1.Version.AtLeast(72))
                {
                    ctx1.List(item1.Point, SizeKind.U16,
    static (BankContext ctx2, ref CoordinatePoint item2) =>
                    {
                        if (ctx2.Version.AtLeast(72))
                        {
                            ctx2.F32(ref item2.Position.X);
                        }
                        if (ctx2.Version.AtLeast(72))
                        {
                            ctx2.F32(ref item2.Position.Y);
                        }
                        if (ctx2.Version.AtLeast(72))
                        {
                            var bits4 = ctx2.Bits32();
                            bits4.Enum(ref item2.Curve, CurveWire.Instance);
                            bits4.Done();
                        }
                    }
                    );
                }
            }
            );
        }
    }

    public static void Section(BankContext c, StateSetting stateValue)
    {
        if (c.Version.In(72, 125))
        {
            c.List(stateValue.Item, SizeKind.U32,
    static (BankContext ctx3, ref StateSettingItem item3) =>
            {
                if (ctx3.Version.In(72, 125))
                {
                    ctx3.Id(ref item3.Group);
                }
                if (ctx3.Version.In(72, 125))
                {
                    var bits5 = ctx3.Bits8();
                    bits5.Enum(ref item3.ChangeOccurAt, TimePointWire.Instance);
                    bits5.Done();
                }
                if (ctx3.Version.In(72, 125))
                {
                    ctx3.List(item3.Apply, SizeKind.U16,
    static (BankContext ctx4, ref StateSettingApplyItem item4) =>
                    {
                        if (ctx4.Version.In(72, 125))
                        {
                            ctx4.Id(ref item4.Target);
                        }
                        if (ctx4.Version.In(72, 125))
                        {
                            ctx4.Id(ref item4.Setting);
                        }
                    }
                    );
                }
            }
            );
        }
        if (c.Version.AtLeast(125))
        {
            c.List(stateValue.Attribute, SizeKind.U8,
    static (BankContext ctx5, ref StateSettingAttribute item5) =>
            {
                if (ctx5.Version.AtLeast(125))
                {
                    ctx5.U8(ref item5.Type);
                }
                if (ctx5.Version.AtLeast(125))
                {
                    var bits6 = ctx5.Bits8();
                    bits6.Enum(ref item5.Category, PropertyCategoryWire.Instance);
                    bits6.Done();
                }
                if (ctx5.Version.AtLeast(128))
                {
                    ctx5.U8(ref item5.U1);
                }
            }
            );
            c.List(stateValue.Item, SizeKind.U8,
    static (BankContext ctx6, ref StateSettingItem item6) =>
            {
                if (ctx6.Version.AtLeast(125))
                {
                    ctx6.Id(ref item6.Group);
                }
                if (ctx6.Version.AtLeast(125))
                {
                    var bits7 = ctx6.Bits8();
                    bits7.Enum(ref item6.ChangeOccurAt, TimePointWire.Instance);
                    bits7.Done();
                }
                if (ctx6.Version.AtLeast(125))
                {
                    ctx6.List(item6.Apply, SizeKind.U8,
    static (BankContext ctx7, ref StateSettingApplyItem item7) =>
                    {
                        if (ctx7.Version.AtLeast(125))
                        {
                            ctx7.Id(ref item7.Target);
                        }
                        if (ctx7.Version.AtLeast(125))
                        {
                            ctx7.Id(ref item7.Setting);
                        }
                    }
                    );
                }
            }
            );
        }
    }
}
