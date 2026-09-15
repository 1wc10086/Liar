using LiarUtil.Core.Particles;
using LiarUtil.Core.PopFx;
using LiarUtil.Core.Reanim;
using LiarUtil.Core.Reanim.Flash;
using LiarUtil.Core.Trail;

namespace LiarUtil.Core.Services;

public sealed record ProcessingRequest
{
    public XflWriterOptions? XflOptions { get; init; }
    public required int Function { get; init; }
    public required int Mode { get; init; }
    public required string InputPath { get; init; }
    public required string OutputPath { get; init; }
    public string InfoPath { get; init; } = "";
    public string PatchPath { get; init; } = "";
    public bool Decode { get; init; }
    public bool UseHeader { get; init; }
    public bool ConvertImages { get; init; }
    public bool DeleteAfterConvert { get; init; }
    public bool ExportResources { get; init; }
    public bool WriteTextureHeader { get; init; }
    public int Encoding { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public int Version { get; init; }
    public int AtlasFormat { get; init; }
    public int TextTableVersion { get; init; }
    public int RsbVersion { get; init; } = 3;
    public uint BnkVersion { get; init; } = 150;
    public int PpfVersion { get; init; } = 1;
    public int PamXflResolution { get; init; } = 768;
    public PopFxVariant Variant { get; init; } = PopFxVariant.V3;
    public ParticlePlatform ParticlePlatform { get; init; } = ParticlePlatform.Pc;
    public TrailPlatform TrailPlatform { get; init; } = TrailPlatform.Pc;
    public ReanimPlatform ReanimPlatform { get; init; } = ReanimPlatform.Pc;
    public bool UseXml { get; init; }
    public bool UseRawPacket { get; init; }
    public bool UseCompression { get; init; } = true;
    public string TextureFormat { get; init; } = "";
    public string Ptx0Format { get; init; } = "ARGB";
    public string RsbPtxFormat { get; init; } = "ARGB";
    public string RtonKey { get; init; } = "";
    public string CdatKey { get; init; } = "";
}
