namespace LiarUtil.Core.PopFx.Binary;

internal readonly record struct PopFxTechniqueRecord(
    uint Name,
    uint Number,
    uint PassCount,
    uint PassBegin,
    uint AnnotationCount,
    uint AnnotationBegin);

internal readonly record struct PopFxAnnotationRecord(uint Name, uint Value, uint Extra);

internal readonly record struct PopFxStringRecord(string Value, uint Format);

internal readonly record struct PopFxValueRecord(uint Type, uint Word0, uint Word1, uint Word2, uint Word3);

internal readonly record struct PopFxPassRecord(
    uint Name,
    uint AnnotationCount,
    uint AnnotationBegin,
    uint SettingCount,
    uint SettingBegin,
    uint VertexShader,
    uint PixelShader);

internal readonly record struct PopFxSettingRecord(
    uint Category,
    uint Type,
    uint AnnotationCount,
    uint AnnotationBegin,
    uint ValueBegin);

internal readonly record struct PopFxShaderParameterRecord(uint Name, uint Register);

internal readonly record struct PopFxShaderRecord(
    uint Format,
    uint Data,
    uint ParameterCount,
    uint ParameterBegin,
    uint EntryPoint);
