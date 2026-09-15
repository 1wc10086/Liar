using LiarUtil.Core.PopFx.Binary;

namespace LiarUtil.Core.PopFx.Decoding;

internal sealed partial class PopFxDecoder
{
    private void ReadTables()
    {
        _techniques = ReadTechniques();
        _annotations = ReadAnnotations();
        _strings = ReadStrings();
        _stringValues = ReadStringValues();
        _values = ReadValues();
        _passes = ReadPasses();
        _settings = ReadSettings();
        _shaderParameters = ReadShaderParameters();
        _shaders = ReadShaders();
    }

    private PopFxTechniqueRecord[] ReadTechniques()
    {
        var section = _sections[0];
        var result = new PopFxTechniqueRecord[section.Count];
        for (var index = 0; index < result.Length; index++)
        {
            _reader.Seek((long)section.Offset + (long)index * section.Size);
            result[index] = new(
                _reader.ReadUInt32(),
                _reader.ReadUInt32(),
                _reader.ReadUInt32(),
                _reader.ReadUInt32(),
                _reader.ReadUInt32(),
                _reader.ReadUInt32());
        }
        return result;
    }

    private PopFxAnnotationRecord[] ReadAnnotations()
    {
        var section = _sections[1];
        var hasExtra = section.Size > PopFxLayout.AnnotationSize;
        var result = new PopFxAnnotationRecord[section.Count];
        for (var index = 0; index < result.Length; index++)
        {
            _reader.Seek((long)section.Offset + (long)index * section.Size);
            var name = _reader.ReadUInt32();
            var value = _reader.ReadUInt32();
            var extra = hasExtra ? _reader.ReadUInt32() : 0;
            result[index] = new(name, value, extra);
        }
        return result;
    }

    private PopFxStringRecord[] ReadStrings()
    {
        var section = _sections[2];
        var result = new PopFxStringRecord[section.Count];
        for (var index = 0; index < result.Length; index++)
        {
            _reader.Seek((long)section.Offset + (long)index * section.Size);
            var characterCount = _reader.ReadUInt32();
            var format = _reader.ReadUInt32();
            var offset = _reader.ReadUInt32();
            var text = _reader.ReadString((long)_stringDataOffset + offset, characterCount);
            result[index] = new(text, format);
        }
        return result;
    }

    private string[] ReadStringValues()
    {
        var result = new string[_strings.Length];
        for (var index = 0; index < result.Length; index++)
        {
            result[index] = _strings[index].Value;
        }
        return result;
    }

    private PopFxValueRecord[] ReadValues()
    {
        var section = _sections[3];
        var result = new PopFxValueRecord[section.Count];
        for (var index = 0; index < result.Length; index++)
        {
            _reader.Seek((long)section.Offset + (long)index * section.Size);
            result[index] = new(
                _reader.ReadUInt32(),
                _reader.ReadUInt32(),
                _reader.ReadUInt32(),
                _reader.ReadUInt32(),
                _reader.ReadUInt32());
        }
        return result;
    }

    private PopFxPassRecord[] ReadPasses()
    {
        var section = _sections[4];
        var result = new PopFxPassRecord[section.Count];
        for (var index = 0; index < result.Length; index++)
        {
            _reader.Seek((long)section.Offset + (long)index * section.Size);
            result[index] = new(
                _reader.ReadUInt32(),
                _reader.ReadUInt32(),
                _reader.ReadUInt32(),
                _reader.ReadUInt32(),
                _reader.ReadUInt32(),
                _reader.ReadUInt32(),
                _reader.ReadUInt32());
        }
        return result;
    }

    private PopFxSettingRecord[] ReadSettings()
    {
        var section = _sections[5];
        var result = new PopFxSettingRecord[section.Count];
        for (var index = 0; index < result.Length; index++)
        {
            _reader.Seek((long)section.Offset + (long)index * section.Size);
            result[index] = new(
                _reader.ReadUInt32(),
                _reader.ReadUInt32(),
                _reader.ReadUInt32(),
                _reader.ReadUInt32(),
                _reader.ReadUInt32());
        }
        return result;
    }

    private PopFxShaderParameterRecord[] ReadShaderParameters()
    {
        if (!PopFxLayout.HasShaderParameters(_variant))
        {
            return [];
        }
        var section = _sections[6];
        var result = new PopFxShaderParameterRecord[section.Count];
        for (var index = 0; index < result.Length; index++)
        {
            _reader.Seek((long)section.Offset + (long)index * section.Size);
            result[index] = new(_reader.ReadUInt32(), _reader.ReadUInt32());
        }
        return result;
    }

    private PopFxShaderRecord[] ReadShaders()
    {
        var hasParameters = PopFxLayout.HasShaderParameters(_variant);
        var section = _sections[hasParameters ? 7 : 6];
        var result = new PopFxShaderRecord[section.Count];
        for (var index = 0; index < result.Length; index++)
        {
            _reader.Seek((long)section.Offset + (long)index * section.Size);
            var format = _reader.ReadUInt32();
            var data = _reader.ReadUInt32();
            if (hasParameters)
            {
                result[index] = new(format, data, _reader.ReadUInt32(), _reader.ReadUInt32(), _reader.ReadUInt32());
            }
            else
            {
                result[index] = new(format, data, 0, 0, _reader.ReadUInt32());
            }
        }
        return result;
    }
}
