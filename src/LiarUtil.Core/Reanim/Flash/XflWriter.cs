using System.Globalization;
using System.Text;
using LiarUtil.Core.Core.IO;
using LiarUtil.Core.PopCap;

namespace LiarUtil.Core.Reanim.Flash;

internal static class XflWriter
{
    private const string LocatorName = "__reanim_locator__";
    private const string DocumentHeader = "PROXY-CS5";

    public static void Write(ReanimFile reanim, string outFile, XflWriterOptions options)
    {
        var root = Path.GetFullPath(outFile);
        var library = Path.Combine(root, "LIBRARY");
        Directory.CreateDirectory(library);
        FileIO.WriteAllText(Path.Combine(root, "main.xfl"), DocumentHeader);

        var media = new List<string>();
        var symbols = new List<string> { LocatorName };
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { LocatorName };
        var registered = new Dictionary<string, string>(StringComparer.Ordinal);
        WriteLocatorSymbol(library, LocatorName);

        var document = new StringBuilder();
        AppendDocumentStart(document, reanim, options);
        for (var index = reanim.Tracks.Count - 1; index >= 0; index--)
        {
            WriteLayer(document, reanim.Tracks[index], index, library, media, symbols, existing, registered, options);
        }
        AppendDocumentEnd(document, media, symbols);
        FileIO.WriteAllText(Path.Combine(root, "DOMDocument.xml"), document.ToString());

        foreach (var name in media)
        {
            var pngPath = Path.Combine(library, name + ".png");
            if (!File.Exists(pngPath))
            {
                FileIO.WriteAllBytes(pngPath, DefaultBitmap.Bytes);
            }
        }
    }

    private static void AppendDocumentStart(StringBuilder document, ReanimFile reanim, XflWriterOptions options)
    {
        document.Append("<DOMDocument frameRate=\"").Append(Number(reanim.Fps))
            .Append("\" width=\"").Append(Number(options.Width))
            .Append("\" height=\"").Append(Number(options.Height))
            .Append("\" xflVersion=\"2.97\" xmlns=\"http://ns.adobe.com/xfl/2008/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">\n");
        document.Append("    <timelines>\n");
        document.Append("        <DOMTimeline name=\"Scene 1\">\n");
        document.Append("            <layers>\n");
    }

    private static void AppendDocumentEnd(StringBuilder document, List<string> media, List<string> symbols)
    {
        document.Append("            </layers>\n");
        document.Append("        </DOMTimeline>\n");
        document.Append("    </timelines>\n");
        document.Append("    <media>\n");
        foreach (var name in media)
        {
            document.Append("        <DOMBitmapItem name=\"").Append(name).Append(".png\" href=\"")
                .Append(name).Append(".png\" />\n");
        }
        document.Append("    </media>\n");
        document.Append("    <symbols>\n");
        foreach (var name in symbols)
        {
            document.Append("        <Include href=\"").Append(name).Append(".xml\" />\n");
        }
        document.Append("    </symbols>\n");
        document.Append("</DOMDocument>");
    }

    private static void WriteLayer(StringBuilder document, ReanimTrack track, int trackIndex, string library,
        List<string> media, List<string> symbols, HashSet<string> existing, Dictionary<string, string> registered,
        XflWriterOptions options)
    {
        var state = new TransformState();
        var imageNames = new Dictionary<string, string>(StringComparer.Ordinal);
        var images = new List<string>();
        var labelIndex = 0;
        document.Append("                <DOMLayer name=\"").Append(Escape(track.Name)).Append("\">\n");
        document.Append("                    <frames>\n");
        for (var frameIndex = 0; frameIndex < track.Transforms.Count; frameIndex++)
        {
            var transform = track.Transforms[frameIndex];
            state.Apply(transform);
            if (transform.Image is not null)
            {
                var imageId = ImageReference.ResolveName(transform.Image) ?? "";
                if (imageId.Length == 0)
                {
                    state.Image = null;
                }
                else
                {
                    if (!imageNames.TryGetValue(imageId, out var libraryName))
                    {
                        var key = options.UseLabelName > 0 ? trackIndex.ToString(CultureInfo.InvariantCulture) + "\0" + imageId : imageId;
                        if (!registered.TryGetValue(key, out libraryName))
                        {
                            libraryName = UniqueName(GetImageName(imageId, track.Name, labelIndex, options.UseLabelName), existing);
                            registered[key] = libraryName;
                            existing.Add(libraryName);
                            images.Add(libraryName);
                            media.Add(libraryName);
                            symbols.Add(libraryName);
                        }
                        labelIndex++;
                        imageNames[imageId] = libraryName;
                    }
                    state.Image = libraryName;
                }
            }

            document.Append("                        <DOMFrame index=\"").Append(frameIndex.ToString(CultureInfo.InvariantCulture)).Append("\">\n");
            if (state.Frame != -1)
            {
                document.Append("                            <elements>\n");
                document.Append("                                <DOMSymbolInstance libraryItemName=\"")
                    .Append(state.Image ?? LocatorName).Append("\">\n");
                document.Append("                                    <matrix>\n");
                AppendMatrix(document, state, options);
                document.Append("                                    </matrix>\n");
                if (state.Alpha != 1)
                {
                    document.Append("                                    <color>\n");
                    document.Append("                                        <Color alphaMultiplier=\"").Append(Number(state.Alpha)).Append("\" />\n");
                    document.Append("                                    </color>\n");
                }
                document.Append("                                </DOMSymbolInstance>\n");
                document.Append("                            </elements>\n");
            }
            else
            {
                document.Append("                            <elements />\n");
            }
            document.Append("                        </DOMFrame>\n");
        }
        document.Append("                    </frames>\n");
        document.Append("                </DOMLayer>\n");

        foreach (var name in images)
        {
            WriteImageSymbol(library, name);
        }
    }

    private static void AppendMatrix(StringBuilder document, TransformState state, XflWriterOptions options)
    {
        const double DegreesToRadians = Math.PI / 180;
        var skewX = state.SkewX * DegreesToRadians;
        var skewY = -state.SkewY * DegreesToRadians;
        document.Append("                                        <Matrix a=\"").Append(Number(Math.Cos(skewX) * state.ScaleX))
            .Append("\" b=\"").Append(Number(Math.Sin(skewX) * state.ScaleX))
            .Append("\" c=\"").Append(Number(Math.Sin(skewY) * state.ScaleY))
            .Append("\" d=\"").Append(Number(Math.Cos(skewY) * state.ScaleY))
            .Append("\" tx=\"").Append(Number(state.X * options.ScaleX))
            .Append("\" ty=\"").Append(Number(state.Y * options.ScaleY))
            .Append("\" />\n");
    }

    private static void WriteImageSymbol(string library, string name) =>
        WriteSymbol(library, name,
            "                                <DOMBitmapInstance selected=\"true\" libraryItemName=\"" + name + ".png\" />");

    private static void WriteLocatorSymbol(string library, string name) => WriteSymbol(library, name, null);

    private static void WriteSymbol(string library, string name, string? element)
    {
        var document = new StringBuilder();
        document.Append("<DOMSymbolItem xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://ns.adobe.com/xfl/2008/\" name=\"").Append(name).Append("\">\n");
        document.Append("    <timeline>\n");
        document.Append("        <DOMTimeline name=\"").Append(name).Append("\">\n");
        document.Append("            <layers>\n");
        document.Append("                <DOMLayer name=\"Layer 1\" color=\"#4FFF4F\" current=\"true\" isSelected=\"true\">\n");
        document.Append("                    <frames>\n");
        document.Append("                        <DOMFrame index=\"0\">\n");
        if (element is null)
        {
            document.Append("                            <elements />\n");
        }
        else
        {
            document.Append("                            <elements>\n");
            document.Append(element).Append('\n');
            document.Append("                            </elements>\n");
        }
        document.Append("                        </DOMFrame>\n");
        document.Append("                    </frames>\n");
        document.Append("                </DOMLayer>\n");
        document.Append("            </layers>\n");
        document.Append("        </DOMTimeline>\n");
        document.Append("    </timeline>\n");
        document.Append("</DOMSymbolItem>");
        FileIO.WriteAllText(Path.Combine(library, name + ".xml"), document.ToString());
    }

    private static string UniqueName(string requested, HashSet<string> existing)
    {
        var baseName = string.IsNullOrEmpty(requested) ? "image" : requested;
        if (existing.Add(baseName))
        {
            return baseName;
        }
        var suffix = 1;
        string name;
        do
        {
            name = baseName + "_" + suffix++.ToString(CultureInfo.InvariantCulture);
        }
        while (!existing.Add(name));
        return name;
    }

    private static string GetImageName(string imageId, string? labelName, int labelIndex, int useLabelName)
    {
        if (useLabelName > 0)
        {
            return labelIndex != 0
                ? labelName + "_" + labelIndex.ToString(CultureInfo.InvariantCulture)
                : labelName ?? imageId;
        }
        if (useLabelName < 0)
        {
            return imageId.ToLowerInvariant();
        }
        var name = imageId;
        if (name.StartsWith("IMAGE_REANIM_", StringComparison.OrdinalIgnoreCase))
        {
            name = name[13..];
        }
        return name.ToLowerInvariant();
    }

    private static string Escape(string? value) =>
        (value ?? "").Replace("&", "&amp;", StringComparison.Ordinal).Replace("\"", "&quot;", StringComparison.Ordinal);

    private static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    private sealed class TransformState
    {
        public float X;
        public float Y;
        public float SkewX;
        public float SkewY;
        public float ScaleX = 1;
        public float ScaleY = 1;
        public double Alpha = 1;
        public string? Image;
        public float Frame;

        public void Apply(ReanimTransform transform)
        {
            if (transform.X is { } x)
            {
                X = x;
            }
            if (transform.Y is { } y)
            {
                Y = y;
            }
            if (transform.SkewX is { } skewX)
            {
                SkewX = skewX;
            }
            if (transform.SkewY is { } skewY)
            {
                SkewY = skewY;
            }
            if (transform.ScaleX is { } scaleX)
            {
                ScaleX = scaleX;
            }
            if (transform.ScaleY is { } scaleY)
            {
                ScaleY = scaleY;
            }
            if (transform.Frame is { } frame)
            {
                Frame = frame;
            }
            if (transform.Alpha is { } alpha)
            {
                Alpha = alpha;
            }
        }
    }
}
