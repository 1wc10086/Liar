using LiarUtil.Core.PopFx.Binary;
using LiarUtil.Core.PopFx.Models;
using LiarUtil.Core.PopFx.Values;

namespace LiarUtil.Core.PopFx.Decoding;

internal sealed partial class PopFxDecoder
{
    private PopFxFile Build()
    {
        var values = BuildValues();
        var shaders = BuildShaders(values);
        var annotations = BuildAnnotations(values);
        var settings = BuildSettings(values, annotations);
        var passes = BuildPasses(shaders, annotations, settings);
        var techniques = BuildTechniques(annotations, passes);
        var unusedStrings = new List<string>();
        for (var index = 0; index < _stringValues.Length; index++)
        {
            if (!_referencedStrings.Contains((uint)index))
            {
                unusedStrings.Add(_stringValues[index]);
            }
        }
        return new PopFxFile
        {
            Number = PopFxFormat.Number,
            Variant = _variant,
            Techniques = [.. techniques],
            UnusedStrings = unusedStrings,
        };
    }

    private PopFxValue[] BuildValues()
    {
        var result = new PopFxValue[_values.Length];
        for (var index = 0; index < result.Length; index++)
        {
            result[index] = ReadValue(_values[index]);
        }
        return result;
    }

    private PopFxShader[] BuildShaders(PopFxValue[] values)
    {
        var result = new PopFxShader[_shaders.Length];
        for (var index = 0; index < result.Length; index++)
        {
            result[index] = ReadShader(_shaders[index], values);
        }
        return result;
    }

    private PopFxAnnotation[] BuildAnnotations(PopFxValue[] values)
    {
        var result = new PopFxAnnotation[_annotations.Length];
        for (var index = 0; index < result.Length; index++)
        {
            result[index] = ReadAnnotation(_annotations[index], values);
        }
        return result;
    }

    private PopFxSetting[] BuildSettings(PopFxValue[] values, PopFxAnnotation[] annotations)
    {
        var result = new PopFxSetting[_settings.Length];
        for (var index = 0; index < result.Length; index++)
        {
            var raw = _settings[index];
            if (raw.Type > 8)
            {
                throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.POPFXSettingTypeInvalid0, raw.Type));
            }
            var setting = new PopFxSetting { Category = raw.Category, Type = raw.Type };
            ValidateRange(raw.AnnotationBegin, raw.AnnotationCount, annotations.Length, LiarUtil.Core.Strings.SettingAnnotation);
            for (var offset = 0u; offset < raw.AnnotationCount; offset++)
            {
                setting.Annotations.Add(annotations[raw.AnnotationBegin + offset]);
            }
            var valueCount = (uint)PopFxValueCount.Of((PopFxValueType)raw.Type);
            ValidateRange(raw.ValueBegin, valueCount, values.Length, LiarUtil.Core.Strings.SettingValue);
            for (var offset = 0u; offset < valueCount; offset++)
            {
                setting.Values.Add(values[raw.ValueBegin + offset]);
            }
            result[index] = setting;
        }
        return result;
    }

    private PopFxPass[] BuildPasses(PopFxShader[] shaders, PopFxAnnotation[] annotations, PopFxSetting[] settings)
    {
        var result = new PopFxPass[_passes.Length];
        for (var index = 0; index < result.Length; index++)
        {
            var raw = _passes[index];
            var pass = new PopFxPass
            {
                Name = StringAt(raw.Name),
                VertexShader = raw.VertexShader == PopFxLayout.EmptyIndex ? (PopFxShader?)null : ShaderAt(shaders, raw.VertexShader),
                PixelShader = raw.PixelShader == PopFxLayout.EmptyIndex ? (PopFxShader?)null : ShaderAt(shaders, raw.PixelShader),
            };
            var items = new List<PopFxAnnotation>();
            ValidateRange(raw.AnnotationBegin, raw.AnnotationCount, annotations.Length, LiarUtil.Core.Strings.ChannelAnnotation);
            for (var offset = 0u; offset < raw.AnnotationCount; offset++)
            {
                items.Add(annotations[raw.AnnotationBegin + offset]);
            }
            SplitAnnotations(items, pass.VertexAnnotations, pass.PixelAnnotations);
            ValidateRange(raw.SettingBegin, raw.SettingCount, settings.Length, LiarUtil.Core.Strings.ChannelSetting);
            for (var offset = 0u; offset < raw.SettingCount; offset++)
            {
                pass.Settings.Add(settings[raw.SettingBegin + offset]);
            }
            result[index] = pass;
        }
        return result;
    }

    private PopFxTechnique[] BuildTechniques(PopFxAnnotation[] annotations, PopFxPass[] passes)
    {
        var result = new PopFxTechnique[_techniques.Length];
        for (var index = 0; index < result.Length; index++)
        {
            var raw = _techniques[index];
            var technique = new PopFxTechnique { Name = StringAt(raw.Name), Number = raw.Number };
            ValidateRange(raw.AnnotationBegin, raw.AnnotationCount, annotations.Length, LiarUtil.Core.Strings.TechniqueAnnotation);
            for (var offset = 0u; offset < raw.AnnotationCount; offset++)
            {
                technique.Annotations.Add(annotations[raw.AnnotationBegin + offset]);
            }
            ValidateRange(raw.PassBegin, raw.PassCount, passes.Length, LiarUtil.Core.Strings.TechniquePass);
            for (var offset = 0u; offset < raw.PassCount; offset++)
            {
                technique.Passes.Add(passes[raw.PassBegin + offset]);
            }
            result[index] = technique;
        }
        return result;
    }

    private PopFxShader ReadShader(in PopFxShaderRecord raw, PopFxValue[] values)
    {
        var shader = new PopFxShader
        {
            Format = StringAt(raw.Format),
            EntryPoint = StringAt(raw.EntryPoint),
            Code = StringAt(raw.Data),
            CodeFormat = _strings[raw.Data].Format,
        };
        if (!PopFxLayout.HasShaderParameters(_variant))
        {
            return shader;
        }
        ValidateRange(raw.ParameterBegin, raw.ParameterCount, _shaderParameters.Length, LiarUtil.Core.Strings.ShaderParameter);
        for (var offset = 0u; offset < raw.ParameterCount; offset++)
        {
            var parameter = _shaderParameters[raw.ParameterBegin + offset];
            shader.Parameters.Add(new PopFxShaderParameter
            {
                Name = StringAt(parameter.Name),
                Register = ValueAt(values, parameter.Register),
            });
        }
        return shader;
    }

    private PopFxAnnotation ReadAnnotation(in PopFxAnnotationRecord raw, PopFxValue[] values) => new()
    {
        Name = StringAt(raw.Name),
        Value = ValueAt(values, raw.Value),
        Unknown = raw.Extra == 0 ? (uint?)null : raw.Extra,
    };

    private string StringAt(uint index)
    {
        if (index >= (uint)_stringValues.Length)
        {
            throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.POPFXStringIndexOutOfRange0, index));
        }
        _referencedStrings.Add(index);
        return _stringValues[index];
    }

    private static PopFxValue ValueAt(PopFxValue[] values, uint index)
    {
        if (index >= (uint)values.Length)
        {
            throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.POPFXValueIndexOutOfRange0, index));
        }
        return values[index];
    }

    private static PopFxShader ShaderAt(PopFxShader[] shaders, uint index)
    {
        if (index >= (uint)shaders.Length)
        {
            throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.POPFXShaderIndexOutOfRange0, index));
        }
        return shaders[index];
    }

    private static void ValidateRange(uint begin, uint count, int length, string label)
    {
        if (count == 0)
        {
            return;
        }
        if (begin == PopFxLayout.EmptyIndex || (ulong)begin + count > (ulong)length)
        {
            throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.POPFX0OutOfRange, label));
        }
    }

    private static void SplitAnnotations(List<PopFxAnnotation> items, List<PopFxAnnotation> vertex, List<PopFxAnnotation> pixel)
    {
        var split = items.Count;
        for (var index = 0; index < items.Count; index++)
        {
            if (items[index].Name.StartsWith("ps", StringComparison.OrdinalIgnoreCase))
            {
                split = index;
                break;
            }
        }
        for (var index = 0; index < items.Count; index++)
        {
            (index < split ? vertex : pixel).Add(items[index]);
        }
    }
}
