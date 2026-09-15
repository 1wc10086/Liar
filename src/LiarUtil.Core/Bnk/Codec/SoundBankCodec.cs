using LiarUtil.Core.Bnk.Model;
using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Bnk.Codec;

internal sealed record EmbeddedMediaItem(long Id, byte[] Data);

internal static class SoundBankCodec
{
    private const uint IdBkhd = 0x44484B42;
    private const uint IdDidx = 0x58444944;
    private const uint IdData = 0x41544144;
    private const uint IdInit = 0x54494E49;
    private const uint IdStmg = 0x474D5453;
    private const uint IdHirc = 0x43524948;
    private const uint IdStid = 0x44495453;
    private const uint IdEnvs = 0x53564E45;
    private const uint IdPlat = 0x54414C50;

    public static SoundBank Read(byte[] data, BankVersion version, List<EmbeddedMediaItem>? media = null)
    {
        var bank = new SoundBank();
        var reader = BankWire.Reader(data);
        BufferReader? index = null;
        while (!reader.AtEnd)
        {
            var id = reader.ReadUInt32();
            var size = reader.ReadUInt32();
            var body = reader.Slice((int)size);
            var context = new BankContext(body, version);
            switch (id)
            {
                case IdBkhd:
                    Bkhd(context, bank);
                    break;
                case IdDidx:
                    index = body;
                    break;
                case IdData:
                    if (index is not null)
                    {
                        Media(index, body, bank, media);
                        index = null;
                    }
                    break;
                case IdInit:
                    bank.Setting ??= new Setting();
                    Init(context, bank.Setting);
                    break;
                case IdStmg:
                    bank.Setting ??= new Setting();
                    bank.GameSynchronization ??= new GameSynchronization();
                    Stmg(context, bank.Setting, bank.GameSynchronization);
                    break;
                case IdHirc:
                    HierarchyCodec.List(context, bank.Hierarchy);
                    break;
                case IdStid:
                    Stid(context, bank.Reference);
                    break;
                case IdEnvs:
                    bank.Setting ??= new Setting();
                    Envs(context, bank.Setting);
                    break;
                case IdPlat:
                    bank.Setting ??= new Setting();
                    Plat(context, bank.Setting);
                    break;
            }
        }
        return bank;
    }

    public static byte[] Write(SoundBank bank, BankVersion version, IReadOnlyList<EmbeddedMediaItem>? media = null)
    {
        var writer = BankWire.Writer();
        Chunk(writer, IdBkhd, version, c => Bkhd(c, bank));
        if (bank.EmbeddedMedia.Count > 0)
        {
            var dataStart = writer.Length + 8 + (bank.EmbeddedMedia.Count * 12) + 8;
            Chunk(writer, IdDidx, version, c => Didx(c, bank, media, dataStart));
            Chunk(writer, IdData, version, c => MediaData(c, media, dataStart));
        }
        var setting = bank.Setting;
        var sync = bank.GameSynchronization;
        if (setting is not null)
        {
            if (version.AtLeast(118))
            {
                Chunk(writer, IdInit, version, c => Init(c, setting));
            }
            Chunk(writer, IdEnvs, version, c => Envs(c, setting));
            if (version.AtLeast(113))
            {
                Chunk(writer, IdPlat, version, c => Plat(c, setting));
            }
        }
        if (setting is not null && sync is not null)
        {
            Chunk(writer, IdStmg, version, c => Stmg(c, setting, sync));
        }
        if (bank.Hierarchy.Count > 0)
        {
            Chunk(writer, IdHirc, version, c => HierarchyCodec.List(c, bank.Hierarchy));
        }
        if (bank.Reference.Count > 0)
        {
            Chunk(writer, IdStid, version, c => Stid(c, bank.Reference));
        }
        return writer.ToArray();
    }

    private static void Chunk(BufferWriter writer, uint id, BankVersion version, Action<BankContext> body)
    {
        var sub = BankWire.Writer();
        body(new BankContext(sub, version));
        writer.WriteUInt32(id);
        writer.WriteUInt32((uint)sub.Length);
        writer.WriteBytes(sub.ToArray());
    }

    private static void Init(BankContext c, Setting setting)
    {
        c.List(setting.PlugIn, SizeKind.U32, static (BankContext ctx, ref PlugInReference item) =>
        {
            ctx.Id(ref item.Identifier);
            if (ctx.Version.In(118, 140))
            {
                ctx.Str32Zero(ref item.Library);
            }
            else if (ctx.Version.AtLeast(140))
            {
                ctx.StrNull(ref item.Library);
            }
        });
    }

    private static void Bkhd(BankContext c, SoundBank bank)
    {
        if (c.Reading)
        {
            c.Reader.ReadUInt32();
        }
        else
        {
            c.Writer.WriteUInt32(c.Version.Number);
        }
        c.Id(ref bank.Identifier);
        if (c.Version.In(72, 125))
        {
            c.U32(ref bank.Language);
        }
        else
        {
            c.Id(ref bank.Language);
        }
        if (c.Reading)
        {
            var rest = c.Reader.Rest();
            bank.HeaderExpand = rest.ToArray();
        }
        else
        {
            c.Writer.WriteBytes(bank.HeaderExpand);
        }
    }

    private static void Media(BufferReader index, BufferReader data, SoundBank bank, List<EmbeddedMediaItem>? media)
    {
        while (!index.AtEnd)
        {
            var id = (long)index.ReadUInt32();
            var offset = (int)index.ReadUInt32();
            var size = (int)index.ReadUInt32();
            bank.EmbeddedMedia.Add(id);
            if (media is not null && id != 0 && size > 0)
            {
                media.Add(new EmbeddedMediaItem(id, data.ReadAt(offset, size)));
            }
        }
    }

    private static uint Align16(uint value) => (value + 15u) & ~15u;

    private static void Didx(BankContext c, SoundBank bank, IReadOnlyList<EmbeddedMediaItem>? media, int dataStart)
    {
        var position = 0u;
        foreach (var id in bank.EmbeddedMedia)
        {
            var size = 0;
            if (media is not null)
            {
                foreach (var item in media)
                {
                    if (item.Id == id)
                    {
                        size = item.Data.Length;
                        break;
                    }
                }
            }
            var absolute = (uint)dataStart + position;
            var aligned = Align16(absolute);
            position = aligned - (uint)dataStart;
            c.Writer.WriteUInt32((uint)id);
            c.Writer.WriteUInt32(position);
            c.Writer.WriteUInt32((uint)size);
            position += (uint)size;
        }
    }

    private static void MediaData(BankContext c, IReadOnlyList<EmbeddedMediaItem>? media, int dataStart)
    {
        if (media is null)
        {
            return;
        }
        var position = 0u;
        foreach (var item in media)
        {
            var absolute = (uint)dataStart + position;
            var aligned = Align16(absolute);
            var pad = aligned - absolute;
            if (pad > 0)
            {
                c.Writer.WriteZeros((int)pad);
                position += pad;
            }
            c.Writer.WriteBytes(item.Data);
            position += (uint)item.Data.Length;
        }
    }

    private static void Stmg(BankContext c, Setting setting, GameSynchronization sync)
    {
        if (c.Version.AtLeast(145))
        {
            var bits = c.Bits16();
            bits.Enum(ref setting.VoiceFilterBehavior, VoiceFilterBehaviorWire.Instance);
            bits.Done();
        }
        c.F32(ref setting.VolumeThreshold);
        c.U16(ref setting.MaximumVoiceInstance);
        if (c.Version.AtLeast(128))
        {
            c.Const16(50);
        }
        c.List(sync.StateGroup, SizeKind.U32, static (BankContext ctx, ref StateGroup item) => BankCodecImpl.Section(ctx, item));
        c.List(sync.SwitchGroup, SizeKind.U32, static (BankContext ctx, ref SwitchGroup item) => BankCodecImpl.Section(ctx, item));
        c.List(sync.GameParameter, SizeKind.U32, static (BankContext ctx, ref GameParameter item) => BankCodecImpl.Section(ctx, item));
        if (c.Version.In(120, 125))
        {
            c.Const32(0);
            c.Const32(0);
        }
        if (c.Version.In(125, 140))
        {
            c.Const32(0);
        }
        if (c.Version.AtLeast(140))
        {
            c.List(sync.U1, SizeKind.U32, static (BankContext ctx, ref GameSynchronizationU1 item) => BankCodecImpl.Section(ctx, item));
        }
    }

    private static void Stid(BankContext c, List<SoundBankReference> list)
    {
        c.Const32(1);
        c.List(list, SizeKind.U32, static (BankContext ctx, ref SoundBankReference item) =>
        {
            ctx.Id(ref item.Identifier);
            ctx.Str8(ref item.Name);
        });
    }

    private static void Envs(BankContext c, Setting setting)
    {
        Obstruction(c, setting.Obstruction.Volume);
        Obstruction(c, setting.Obstruction.LowPassFilter);
        if (c.Version.AtLeast(112))
        {
            Obstruction(c, setting.Obstruction.HighPassFilter);
        }
        Obstruction(c, setting.Occlusion.Volume);
        Obstruction(c, setting.Occlusion.LowPassFilter);
        if (c.Version.AtLeast(112))
        {
            Obstruction(c, setting.Occlusion.HighPassFilter);
        }
    }

    private static void Obstruction(BankContext c, ObstructionSetting value)
    {
        var bits = c.Bits8();
        bits.Bit(ref value.Enable);
        bits.Enum(ref value.Mode, CoordinateModeWire.Instance);
        bits.Done();
        c.List(value.Point, SizeKind.U16, static (BankContext ctx, ref CoordinatePoint item) =>
        {
            ctx.F32(ref item.Position.X);
            ctx.F32(ref item.Position.Y);
            var inner = ctx.Bits32();
            inner.Enum(ref item.Curve, CurveWire.Instance);
            inner.Done();
        });
    }

    private static void Plat(BankContext c, Setting setting)
    {
        if (c.Version.In(113, 118))
        {
            c.Str32(ref setting.Platform);
        }
        else if (c.Version.In(118, 140))
        {
            c.Str32Zero(ref setting.Platform);
        }
        else if (c.Version.AtLeast(140))
        {
            c.StrNull(ref setting.Platform);
        }
    }
}
