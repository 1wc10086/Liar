using LiarUtil.Core.PopFx.Binary;
using LiarUtil.Core.PopFx.Models;

namespace LiarUtil.Core.PopFx.Decoding;

internal sealed partial class PopFxDecoder(byte[] data, PopFxVariant variant)
{
    private readonly byte[] _data = data;
    private readonly PopFxVariant _variant = variant;
    private readonly PopFxReader _reader = new(data);
    private readonly HashSet<uint> _referencedStrings = [];

    private PopFxSection[] _sections = [];
    private uint _stringDataOffset;
    private PopFxTechniqueRecord[] _techniques = [];
    private PopFxAnnotationRecord[] _annotations = [];
    private PopFxStringRecord[] _strings = [];
    private PopFxValueRecord[] _values = [];
    private PopFxPassRecord[] _passes = [];
    private PopFxSettingRecord[] _settings = [];
    private PopFxShaderParameterRecord[] _shaderParameters = [];
    private PopFxShaderRecord[] _shaders = [];
    private string[] _stringValues = [];

    public PopFxFile Decode()
    {
        if (_variant is not (PopFxVariant.V1 or PopFxVariant.V2 or PopFxVariant.V3))
        {
            throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.UnsupportedPOPFXVersionVariant0, (int)_variant));
        }
        ReadHeader();
        ReadTables();
        return Build();
    }

    private void ReadHeader()
    {
        if (_data.Length < PopFxLayout.HeaderSize(_variant))
        {
            throw new InvalidDataException(LiarUtil.Core.Strings.POPFXDataLengthInsufficient);
        }
        if (_reader.ReadUInt32() != PopFxFormat.MagicMarker)
        {
            throw new InvalidDataException(LiarUtil.Core.Strings.POPFXMagicNumberIncorrect);
        }
        var number = _reader.ReadUInt32();
        if (number != PopFxFormat.Number)
        {
            throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.UnsupportedPOPFXVersion0, number));
        }
        var sectionCount = PopFxLayout.SectionCount(_variant);
        _sections = new PopFxSection[sectionCount];
        for (var index = 0; index < sectionCount; index++)
        {
            _sections[index] = new(_reader.ReadUInt32(), _reader.ReadUInt32(), _reader.ReadUInt32());
        }
        _stringDataOffset = _reader.ReadUInt32();
        if (_stringDataOffset > _data.Length)
        {
            throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.POPFXStringDataOffsetOutOfRange0, _stringDataOffset));
        }
        for (var index = 0; index < sectionCount; index++)
        {
            var section = _sections[index];
            var expectedSize = PopFxLayout.SectionSize(_variant, index);
            if (section.Size != expectedSize)
            {
                throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.POPFXVersionVariantDoesNotMatchFileSection, index, section.Size, expectedSize));
            }
            if ((ulong)section.Offset + (ulong)section.Count * section.Size > (ulong)_data.Length)
            {
                throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.POPFXSection0OutOfRange, index));
            }
        }
    }
}
