using System.Text;
using LiarUtil.Core.PopFx.Binary;
using LiarUtil.Core.PopFx.Models;

namespace LiarUtil.Core.PopFx.Encoding;

public sealed class PopFxEncoder
{
    private static readonly UTF8Encoding Utf8 = new(false, true);

    public byte[] Encode(PopFxFile file)
    {
        var variant = file.Variant;
        Validate(file, variant);
        var pool = new PopFxPool();
        foreach (var technique in file.Techniques ?? [])
        {
            pool.AddTechnique(technique);
        }
        foreach (var text in file.UnusedStrings ?? [])
        {
            pool.AddString(text ?? "", 0);
        }
        return Write(pool, variant);
    }

    private static void Validate(PopFxFile file, PopFxVariant variant)
    {
        if (file.Number != PopFxFormat.Number)
        {
            throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.UnsupportedPOPFXVersion0, file.Number));
        }
        if (variant is not (PopFxVariant.V1 or PopFxVariant.V2 or PopFxVariant.V3))
        {
            throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.UnsupportedPOPFXVersionVariant0, (int)variant));
        }
    }

    private static byte[] Write(PopFxPool pool, PopFxVariant variant)
    {
        var sectionCount = PopFxLayout.SectionCount(variant);
        var baseOffset = (int)PopFxLayout.HeaderSize(variant);
        var techniqueOffset = baseOffset;
        var passOffset = techniqueOffset + pool.Techniques.Count * (int)PopFxLayout.TechniqueSize;
        var settingOffset = passOffset + pool.Passes.Count * (int)PopFxLayout.PassSize;
        var annotationOffset = settingOffset + pool.Settings.Count * (int)PopFxLayout.AnnotationSizeFor(variant);
        var stringOffset = annotationOffset + pool.Annotations.Count * (int)PopFxLayout.AnnotationSizeFor(variant);
        var valueOffset = stringOffset + pool.Strings.Count * (int)PopFxLayout.StringSize;
        var hasShaderParameters = PopFxLayout.HasShaderParameters(variant);
        var shaderParameterOffset = valueOffset + pool.Values.Count * (int)PopFxLayout.ValueSize;
        var shaderOffset = hasShaderParameters
            ? shaderParameterOffset + pool.ShaderParameters.Count * (int)PopFxLayout.ShaderParameterSize
            : shaderParameterOffset;
        var stringDataOffset = shaderOffset + pool.Shaders.Count * (int)PopFxLayout.ShaderSizeFor(variant);

        var stringData = new List<byte>();
        var stringOffsets = new uint[pool.Strings.Count];
        for (var index = 0; index < pool.Strings.Count; index++)
        {
            stringOffsets[index] = (uint)stringData.Count;
            stringData.AddRange(Utf8.GetBytes(pool.Strings[index].Value));
            stringData.Add(0);
        }

        var writer = new PopFxWriter(stringDataOffset + stringData.Count);
        writer.WriteUInt32(0, PopFxFormat.MagicMarker);
        writer.WriteUInt32(4, PopFxFormat.Number);
        var sections = new[]
        {
            new PopFxSection((uint)pool.Techniques.Count, (uint)techniqueOffset, PopFxLayout.TechniqueSize),
            new PopFxSection((uint)pool.Annotations.Count, (uint)annotationOffset, PopFxLayout.AnnotationSizeFor(variant)),
            new PopFxSection((uint)pool.Strings.Count, (uint)stringOffset, PopFxLayout.StringSize),
            new PopFxSection((uint)pool.Values.Count, (uint)valueOffset, PopFxLayout.ValueSize),
            new PopFxSection((uint)pool.Passes.Count, (uint)passOffset, PopFxLayout.PassSize),
            new PopFxSection((uint)pool.Settings.Count, (uint)settingOffset, PopFxLayout.SettingSize),
            hasShaderParameters
                ? new PopFxSection((uint)pool.ShaderParameters.Count, (uint)shaderParameterOffset, PopFxLayout.ShaderParameterSize)
                : new PopFxSection((uint)pool.Shaders.Count, (uint)shaderOffset, PopFxLayout.ShaderSizeFor(variant)),
            hasShaderParameters
                ? new PopFxSection((uint)pool.Shaders.Count, (uint)shaderOffset, PopFxLayout.ShaderSizeFor(variant))
                : default,
        };
        for (var index = 0; index < sectionCount; index++)
        {
            var headerOffset = 8 + index * 12;
            writer.WriteUInt32(headerOffset, sections[index].Count);
            writer.WriteUInt32(headerOffset + 4, sections[index].Offset);
            writer.WriteUInt32(headerOffset + 8, sections[index].Size);
        }
        writer.WriteUInt32(8 + sectionCount * 12, (uint)stringDataOffset);

        for (var index = 0; index < pool.Techniques.Count; index++)
        {
            var record = pool.Techniques[index];
            WriteRecord(writer, techniqueOffset + index * (int)PopFxLayout.TechniqueSize,
                record.Name, record.Number, record.PassCount, record.PassBegin, record.AnnotationCount, record.AnnotationBegin);
        }
        for (var index = 0; index < pool.Passes.Count; index++)
        {
            var record = pool.Passes[index];
            WriteRecord(writer, passOffset + index * (int)PopFxLayout.PassSize,
                record.Name, record.AnnotationCount, record.AnnotationBegin, record.SettingCount, record.SettingBegin, record.VertexShader, record.PixelShader);
        }
        for (var index = 0; index < pool.Settings.Count; index++)
        {
            var record = pool.Settings[index];
            WriteRecord(writer, settingOffset + index * (int)PopFxLayout.SettingSize,
                record.Category, record.Type, record.AnnotationCount, record.AnnotationBegin, record.ValueBegin);
        }
        var annotationSize = PopFxLayout.AnnotationSizeFor(variant);
        for (var index = 0; index < pool.Annotations.Count; index++)
        {
            var record = pool.Annotations[index];
            var offset = annotationOffset + index * (int)annotationSize;
            writer.WriteUInt32(offset, record.Name);
            writer.WriteUInt32(offset + 4, record.Value);
            if (annotationSize > PopFxLayout.AnnotationSize)
            {
                writer.WriteUInt32(offset + 8, record.Extra);
            }
        }
        for (var index = 0; index < pool.Strings.Count; index++)
        {
            var offset = stringOffset + index * (int)PopFxLayout.StringSize;
            writer.WriteUInt32(offset, (uint)Utf8.GetByteCount(pool.Strings[index].Value));
            writer.WriteUInt32(offset + 4, pool.Strings[index].Format);
            writer.WriteUInt32(offset + 8, stringOffsets[index]);
        }
        for (var index = 0; index < pool.Values.Count; index++)
        {
            var record = pool.Values[index];
            WriteRecord(writer, valueOffset + index * (int)PopFxLayout.ValueSize,
                record.Type, record.Word0, record.Word1, record.Word2, record.Word3);
        }
        if (hasShaderParameters)
        {
            for (var index = 0; index < pool.ShaderParameters.Count; index++)
            {
                var record = pool.ShaderParameters[index];
                WriteRecord(writer, shaderParameterOffset + index * (int)PopFxLayout.ShaderParameterSize, record.Name, record.Register);
            }
        }
        for (var index = 0; index < pool.Shaders.Count; index++)
        {
            var record = pool.Shaders[index];
            var offset = shaderOffset + index * (int)PopFxLayout.ShaderSizeFor(variant);
            if (hasShaderParameters)
            {
                WriteRecord(writer, offset, record.Format, record.Data, record.ParameterCount, record.ParameterBegin, record.EntryPoint);
            }
            else
            {
                WriteRecord(writer, offset, record.Format, record.Data, record.EntryPoint);
            }
        }
        writer.WriteBytes(stringDataOffset, System.Runtime.InteropServices.CollectionsMarshal.AsSpan(stringData));
        return writer.ToArray();
    }

    private static void WriteRecord(PopFxWriter writer, int offset, params uint[] values)
    {
        for (var index = 0; index < values.Length; index++)
        {
            writer.WriteUInt32(offset + index * 4, values[index]);
        }
    }
}
