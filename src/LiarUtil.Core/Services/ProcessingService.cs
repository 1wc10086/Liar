using LiarUtil.Core.Particles;
using LiarUtil.Core.PopFx;
using LiarUtil.Core.Reanim;
using LiarUtil.Core.Rsb;
using LiarUtil.Core.Rton;
using LiarUtil.Core.Trail;

namespace LiarUtil.Core.Services;

public sealed class ProcessingService(
    RtonService rton,
    TextureService texture,
    RsbService rsb,
    BnkService bnk,
    PamService pam,
    PpfService ppf,
    PopFxService popFx,
    Cfw2Service cfw2,
    ParticleService particle,
    TrailService trail,
    ReanimService reanim,
    DzService dz,
    PakService pak,
    ArcvService arcv,
    XprService xpr,
    AtlasService atlas,
    NewtonService newton,
    RsbPatchService rsbPatch,
    FontWidgetDatService fontWidgetDat,
    PaxService pax,
    WemService wem,
    TextTableService textTable,
    SpsService sps,
    SnrService snr,
    CafService caf,
    WmaService wma,
    XmService xm,
    XmaService xma,
    XnbAudioService xnbAudio,
    XnbFontService xnbFont,
    Cfu2Service cfu2) : IProcessingService
{
    public Task ProcessAsync(ProcessingRequest request) => Task.Run(() => Process(request));

    private void Process(ProcessingRequest request)
    {
        switch (request.Function)
        {
            case 0:
                rton.Process(ToRtonFunction(request.Mode), request.InputPath, request.OutputPath,
                    request.Encoding == 0 ? StringEncoding.Utf8 : StringEncoding.Eascii, request.RtonKey);
                break;
            case 1 when request.Decode:
                texture.Decode(request.Mode, request.InputPath, request.OutputPath,
                    request.UseHeader ? request.Ptx0Format : request.TextureFormat, request.UseHeader,
                    request.Width, request.Height, request.Ptx0Format, request.CdatKey);
                break;
            case 1:
                texture.Encode(request.Mode, request.InputPath, request.OutputPath, request.TextureFormat,
                    request.UseHeader, request.RsbPtxFormat, request.CdatKey);
                break;
            case 2 when request.Mode == 0:
                rsb.Unpack(request.InputPath, request.OutputPath, ToRsbVersion(request.RsbVersion),
                    request.ExportResources, request.WriteTextureHeader, request.ConvertImages,
                    request.DeleteAfterConvert, request.RsbPtxFormat);
                break;
            case 2:
                rsb.Pack(request.InputPath, request.OutputPath, ToRsbVersion(request.RsbVersion), request.RsbPtxFormat);
                break;
            case 3 when request.Mode == 0:
                bnk.Unpack(request.InputPath, request.OutputPath, request.BnkVersion);
                break;
            case 3:
                bnk.Pack(request.InputPath, request.OutputPath, request.BnkVersion);
                break;
            case 4 when request.Mode == 0:
                pam.Decode(request.InputPath, request.OutputPath);
                break;
            case 4 when request.Mode == 1:
                pam.Encode(request.InputPath, request.OutputPath, request.Version);
                break;
            case 4 when request.Mode >= 2:
                ConvertPamFlash(request);
                break;
            case 5 when request.Mode == 0:
                ppf.Decode(request.InputPath, request.OutputPath);
                break;
            case 5:
                ppf.Encode(request.InputPath, request.OutputPath, request.PpfVersion);
                break;
            case 6 when request.Mode == 0:
                popFx.Decode(request.InputPath, request.OutputPath, request.Variant);
                break;
            case 6:
                popFx.Encode(request.InputPath, request.OutputPath, request.Variant);
                break;
            case 7 when request.Mode == 0:
                cfw2.Decode(request.InputPath, request.OutputPath);
                break;
            case 7:
                cfw2.Encode(request.InputPath, request.OutputPath);
                break;
            case 8 when request.Mode == 0:
                particle.Decode(request.InputPath, request.OutputPath, request.ParticlePlatform, request.UseXml);
                break;
            case 8:
                particle.Encode(request.InputPath, request.OutputPath, request.ParticlePlatform, request.UseXml, request.UseCompression);
                break;
            case 9 when request.Mode == 0:
                trail.Decode(request.InputPath, request.OutputPath, request.TrailPlatform, request.UseXml);
                break;
            case 9:
                trail.Encode(request.InputPath, request.OutputPath, request.TrailPlatform, request.UseXml, request.UseCompression);
                break;
            case 10 when request.Mode == 0:
                reanim.Decode(request.InputPath, request.OutputPath, request.ReanimPlatform, request.UseXml);
                break;
            case 10:
                reanim.Encode(request.InputPath, request.OutputPath, request.ReanimPlatform, request.UseXml, request.UseCompression, request.XflOptions);
                break;
            case 11 when request.Mode == 0:
                dz.Unpack(request.InputPath, request.OutputPath);
                break;
            case 11:
                dz.Pack(request.InputPath, request.OutputPath);
                break;
            case 12 when request.Mode == 0:
                pak.Unpack(request.InputPath, request.OutputPath);
                break;
            case 12:
                pak.Pack(request.InputPath, request.OutputPath);
                break;
            case 13 when request.Mode == 0:
                arcv.Unpack(request.InputPath, request.OutputPath);
                break;
            case 13:
                arcv.Pack(request.InputPath, request.OutputPath);
                break;
            case 14 when request.Mode == 0:
                xpr.Unpack(request.InputPath, request.OutputPath);
                break;
            case 14:
                xpr.Pack(request.InputPath, request.OutputPath);
                break;
            case 15 when request.Mode == 0:
                atlas.Cut(request.AtlasFormat, request.InputPath, request.OutputPath, request.InfoPath, "");
                break;
            case 15:
                atlas.Splice(request.AtlasFormat, request.InputPath, request.OutputPath, request.InfoPath, "",
                    request.Width, request.Height);
                break;
            case 16 when request.Mode == 0:
                newton.Decode(request.InputPath, request.OutputPath);
                break;
            case 16:
                newton.Encode(request.InputPath, request.OutputPath);
                break;
            case 17 when request.Mode == 0:
                rsbPatch.Decode(request.PatchPath, request.InputPath, request.OutputPath, request.UseRawPacket);
                break;
            case 17:
                rsbPatch.Encode(request.InputPath, request.OutputPath, request.PatchPath, request.UseRawPacket);
                break;
            case 18 when request.Mode == 0:
                fontWidgetDat.Decode(request.InputPath, request.OutputPath);
                break;
            case 18:
                fontWidgetDat.Encode(request.InputPath, request.OutputPath);
                break;
            case 19 when request.Mode == 0:
                pax.Decode(request.InputPath, request.OutputPath);
                break;
            case 19:
                pax.Encode(request.InputPath, request.OutputPath);
                break;
            case 20:
                wem.Decode(request.InputPath, request.OutputPath);
                break;
            case 21:
                textTable.Convert(request.InputPath, request.OutputPath, request.TextTableVersion);
                break;
            case 22 when request.Mode == 0:
                sps.Decode(request.InputPath, request.OutputPath);
                break;
            case 22:
                sps.Encode(request.InputPath, request.OutputPath);
                break;
            case 23 when request.Mode == 0:
                snr.Decode(request.InputPath, request.OutputPath);
                break;
            case 23:
                snr.Encode(request.InputPath, request.OutputPath);
                break;
            case 24:
                caf.Decode(request.InputPath, request.OutputPath);
                break;
            case 25:
                wma.Decode(request.InputPath, request.OutputPath);
                break;
            case 26:
                xm.Decode(request.InputPath, request.OutputPath);
                break;
            case 27 when request.Mode == 0:
                xma.Decode(request.InputPath, request.OutputPath);
                break;
            case 27:
                xma.Encode(request.InputPath, request.OutputPath);
                break;
            case 28:
                xnbAudio.Decode(request.InputPath, request.OutputPath);
                break;
            case 29 when request.Mode == 0:
                xnbFont.Decode(request.InputPath, request.OutputPath);
                break;
            case 29:
                xnbFont.Encode(request.InputPath, request.OutputPath);
                break;
            case 30 when request.Mode == 0:
                cfu2.Decode(request.InputPath, request.OutputPath);
                break;
            case 30:
                cfu2.Encode(request.InputPath, request.OutputPath);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(request));
        }
    }

    private void ConvertPamFlash(ProcessingRequest request)
    {
        if (Directory.Exists(request.InputPath))
        {
            pam.FlashToJson(request.InputPath, request.OutputPath);
            return;
        }

        pam.JsonToFlash(request.InputPath, request.OutputPath, request.PamXflResolution);
    }

    private static RtonFunction ToRtonFunction(int mode) => mode switch
    {
        0 => RtonFunction.Decode,
        1 => RtonFunction.Encode,
        2 => RtonFunction.Encrypt,
        _ => RtonFunction.Decrypt,
    };

    private static RsbVersion ToRsbVersion(int value) => value switch
    {
        1 => RsbVersion.V1,
        4 => RsbVersion.V4,
        _ => RsbVersion.V3,
    };
}
