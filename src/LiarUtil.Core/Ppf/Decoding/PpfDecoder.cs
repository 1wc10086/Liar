using LiarUtil.Core.Core.Binary;
using LiarUtil.Core.Ppf.Models;

namespace LiarUtil.Core.Ppf.Decoding;

internal sealed partial class PpfDecoder
{
    private static readonly byte[] MagicMarker = [0x04, (byte)'P', (byte)'P', (byte)'F', (byte)'1'];

    private readonly BufferReader _reader;

    public PpfDecoder(byte[] data) => _reader = new BufferReader(data) { ErrorFactory = static message => new InvalidDataException(message) };

    public PpfEffect Decode()
    {
        if (!_reader.ReadSpan(MagicMarker.Length).SequenceEqual(MagicMarker))
        {
            throw new InvalidDataException(LiarUtil.Core.Strings.PPFMagicNumberIncorrect);
        }
        var version = _reader.ReadUInt32();
        if (version != PpfCodec.Version)
        {
            throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.UnsupportedPPFVersion0, version));
        }
        return ReadEffect();
    }

    private PpfEffect ReadEffect()
    {
        var note = _reader.ReadStringByByteHead();
        var textures = ReadList(ReadTexture);
        var emitters = ReadList(ReadEmitter);
        var layers = ReadList(ReadLayer);
        var backgroundColor = ReadColor();
        var unknown1 = _reader.ReadInt32();
        var unknown2 = _reader.ReadInt32();
        var frameRate = _reader.ReadInt16();
        var unknown3 = _reader.ReadInt16();
        var unknown4 = _reader.ReadInt16();
        var unknown5 = _reader.ReadInt16();
        var size = ReadSize();
        var unknown6 = _reader.ReadInt32();
        var unknown7 = _reader.ReadInt32();
        var unknown8 = _reader.ReadInt32();
        var unknown9 = _reader.ReadInt32();
        var unknown10 = _reader.ReadInt32();
        var frameRange = ReadFrameRange();
        var unknown11 = _reader.ReadStringByByteHead();
        var unknown12 = _reader.ReadByte();
        var unknown13 = _reader.ReadInt16();
        var unknown14 = _reader.ReadInt16();
        var startupStateData = ReadBytesList();
        var effect = new PpfEffect
        {
            Note = note,
            Size = size,
            FrameRate = frameRate,
            FrameRange = frameRange,
            BackgroundColor = backgroundColor,
            StartupState = null,
            Textures = textures,
            Emitters = emitters,
            Layers = layers,
            Unknown1 = unknown1,
            Unknown2 = unknown2,
            Unknown3 = unknown3,
            Unknown4 = unknown4,
            Unknown5 = unknown5,
            Unknown6 = unknown6,
            Unknown7 = unknown7,
            Unknown8 = unknown8,
            Unknown9 = unknown9,
            Unknown10 = unknown10,
            Unknown11 = unknown11,
            Unknown12 = unknown12,
            Unknown13 = unknown13,
            Unknown14 = unknown14,
        };
        if (startupStateData.Length > 0)
        {
            effect.StartupState = new PpfStartupStateReader(effect).Read(startupStateData);
        }
        return effect;
    }

    private List<T> ReadList<T>(Func<T> readItem)
    {
        var count = _reader.ReadUInt16();
        var items = new List<T>(count);
        for (var index = 0; index < count; index++)
        {
            items.Add(readItem());
        }
        return items;
    }

    private List<string> ReadStringList()
    {
        var count = _reader.ReadUInt16();
        var items = new List<string>(count);
        for (var index = 0; index < count; index++)
        {
            items.Add(_reader.ReadStringByByteHead());
        }
        return items;
    }

    private byte[] ReadBytesList()
    {
        var length = _reader.ReadUInt32();
        if (length > int.MaxValue)
        {
            throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.PPFDataBlockLengthInvalid0, length));
        }
        return _reader.ReadBytes((int)length);
    }
}
