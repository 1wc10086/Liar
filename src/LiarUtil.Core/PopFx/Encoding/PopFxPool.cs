using LiarUtil.Core.PopFx.Binary;
using LiarUtil.Core.PopFx.Models;
using LiarUtil.Core.PopFx.Values;

namespace LiarUtil.Core.PopFx.Encoding;

internal sealed class PopFxPool
{
    private readonly Dictionary<PopFxValue, uint> _valueIndices = new(PopFxReferenceComparer<PopFxValue>.Instance);
    private readonly Dictionary<PopFxShader, uint> _shaderIndices = new(PopFxReferenceComparer<PopFxShader>.Instance);

    public List<PopFxTechniqueRecord> Techniques { get; } = [];
    public List<PopFxPassRecord> Passes { get; } = [];
    public List<PopFxAnnotationRecord> Annotations { get; } = [];
    public List<PopFxSettingRecord> Settings { get; } = [];
    public List<PopFxStringRecord> Strings { get; } = [];
    public List<PopFxValueRecord> Values { get; } = [];
    public List<PopFxShaderParameterRecord> ShaderParameters { get; } = [];
    public List<PopFxShaderRecord> Shaders { get; } = [];

    public uint AddString(string value, uint format)
    {
        Strings.Add(new(value, format));
        return (uint)(Strings.Count - 1);
    }

    public uint AddValue(PopFxValue value)
    {
        if (_valueIndices.TryGetValue(value, out var existing))
        {
            return existing;
        }
        var index = AddValueCore(value);
        _valueIndices[value] = index;
        return index;
    }

    public uint AddAnnotation(PopFxAnnotation annotation)
    {
        var name = AddString(annotation.Name ?? "", 0);
        var value = AddValue(annotation.Value ?? new PopFxValue());
        Annotations.Add(new(name, value, annotation.Unknown ?? 0));
        return (uint)(Annotations.Count - 1);
    }

    public (uint Count, uint Begin) AddAnnotations(IReadOnlyList<PopFxAnnotation> annotations)
    {
        if (annotations.Count == 0)
        {
            return (0, PopFxLayout.EmptyIndex);
        }
        var begin = (uint)Annotations.Count;
        foreach (var annotation in annotations)
        {
            AddAnnotation(annotation);
        }
        return ((uint)annotations.Count, begin);
    }

    public uint AddShader(PopFxShader shader)
    {
        if (_shaderIndices.TryGetValue(shader, out var existing))
        {
            return existing;
        }
        var format = AddString(shader.Format ?? "", 0);
        var entryPoint = AddString(shader.EntryPoint ?? "", 0);
        var data = AddString(shader.Code ?? "", shader.CodeFormat);
        var parameters = shader.Parameters ?? [];
        var parameterBegin = (uint)ShaderParameters.Count;
        foreach (var parameter in parameters)
        {
            var name = AddString(parameter.Name ?? "", 0);
            var register = AddValue(parameter.Register ?? new PopFxValue());
            ShaderParameters.Add(new(name, register));
        }
        var parameterCount = (uint)parameters.Count;
        Shaders.Add(new(format, data, parameterCount, parameterCount == 0 ? 0u : parameterBegin, entryPoint));
        var index = (uint)(Shaders.Count - 1);
        _shaderIndices[shader] = index;
        return index;
    }

    public uint AddSetting(PopFxSetting setting)
    {
        var (annotationCount, annotationBegin) = AddAnnotations(setting.Annotations ?? []);
        var valueBegin = (uint)Values.Count;
        foreach (var value in setting.Values ?? [])
        {
            AddValueCore(value);
        }
        Settings.Add(new(setting.Category, setting.Type, annotationCount, annotationBegin, valueBegin));
        return (uint)(Settings.Count - 1);
    }

    public uint AddPass(PopFxPass pass)
    {
        var name = AddString(pass.Name ?? "", 0);
        var vertexShader = pass.VertexShader is null ? PopFxLayout.EmptyIndex : AddShader(pass.VertexShader);
        var (vertexCount, vertexBegin) = AddAnnotations(pass.VertexAnnotations ?? []);
        var pixelShader = pass.PixelShader is null ? PopFxLayout.EmptyIndex : AddShader(pass.PixelShader);
        var (pixelCount, pixelBegin) = AddAnnotations(pass.PixelAnnotations ?? []);
        var settings = pass.Settings ?? [];
        var settingBegin = (uint)Settings.Count;
        foreach (var setting in settings)
        {
            AddSetting(setting);
        }
        var annotationCount = vertexCount + pixelCount;
        var annotationBegin = vertexCount == 0 ? (pixelCount == 0 ? PopFxLayout.EmptyIndex : pixelBegin) : vertexBegin;
        Passes.Add(new(
            name,
            annotationCount,
            annotationBegin,
            (uint)settings.Count,
            settings.Count == 0 ? PopFxLayout.EmptyIndex : settingBegin,
            vertexShader,
            pixelShader));
        return (uint)(Passes.Count - 1);
    }

    public uint AddTechnique(PopFxTechnique technique)
    {
        var name = AddString(technique.Name ?? "", 0);
        var (annotationCount, annotationBegin) = AddAnnotations(technique.Annotations ?? []);
        var passes = technique.Passes ?? [];
        var passBegin = (uint)Passes.Count;
        foreach (var pass in passes)
        {
            AddPass(pass);
        }
        Techniques.Add(new(name, technique.Number, (uint)passes.Count, passBegin, annotationCount, annotationBegin));
        return (uint)(Techniques.Count - 1);
    }

    private uint AddValueCore(PopFxValue value)
    {
        if (!Enum.IsDefined(value.Type))
        {
            throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.POPFXValueTypeInvalid0, (uint)value.Type));
        }
        var count = PopFxValueCount.Of(value.Type);
        var words = new uint[4];
        switch (value.Type)
        {
            case PopFxValueType.Float:
                words[0] = BitConverter.SingleToUInt32Bits(value.Number ?? 0f);
                break;
            case PopFxValueType.Int:
                words[0] = unchecked((uint)(value.Integer ?? 0));
                break;
            case PopFxValueType.Float2:
            case PopFxValueType.Float3:
            case PopFxValueType.Float4:
                var numbers = value.Numbers ?? [];
                for (var index = 0; index < count; index++)
                {
                    words[index] = BitConverter.SingleToUInt32Bits(index < numbers.Length ? numbers[index] : 0f);
                }
                break;
            case PopFxValueType.Int2:
            case PopFxValueType.Int3:
            case PopFxValueType.Int4:
                var integers = value.Integers ?? [];
                for (var index = 0; index < count; index++)
                {
                    words[index] = unchecked((uint)(index < integers.Length ? integers[index] : 0));
                }
                break;
            case PopFxValueType.String:
                words[0] = AddString(value.Text ?? "", 0);
                break;
        }
        var padding = value.Padding ?? [];
        for (var index = 0; index < 4 - count && index < padding.Length; index++)
        {
            words[count + index] = padding[index];
        }
        Values.Add(new((uint)value.Type, words[0], words[1], words[2], words[3]));
        return (uint)(Values.Count - 1);
    }
}
