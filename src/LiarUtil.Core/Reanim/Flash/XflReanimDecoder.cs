using System.Globalization;
using LiarUtil.Core.Core.Xml;
using LiarUtil.Core.PopCap;

namespace LiarUtil.Core.Reanim.Flash;

internal static class XflReanimDecoder
{
    private const double RadiansToDegrees = 180.0 / Math.PI;

    public static ReanimFile Decode(string path) => Decode(XflProject.Load(path));

    public static ReanimFile DecodeArchive(string path) => Decode(XflProject.LoadArchive(path));

    private static ReanimFile Decode(XflProject project)
    {
        var timeline = project.MainTimeline;
        var totalFrames = timeline.FrameCount;
        var tracks = new List<ReanimTrack>();
        for (var layerIndex = timeline.Layers.Count - 1; layerIndex >= 0; layerIndex--)
        {
            var layer = timeline.Layers[layerIndex];
            if (layer.Name.StartsWith('_') && layer.Name != "_ground")
            {
                continue;
            }
            var frames = layer.BakeFrames(totalFrames);
            var basesByFrame = new List<BaseResult>[frames.Count];
            var elementCount = 1;
            for (var frameIndex = 0; frameIndex < frames.Count; frameIndex++)
            {
                basesByFrame[frameIndex] = FindBases(frames[frameIndex], project);
                elementCount = Math.Max(elementCount, basesByFrame[frameIndex].Count);
            }
            for (var elementIndex = 0; elementIndex < elementCount; elementIndex++)
            {
                var previous = new FrameState();
                var transforms = new ReanimTransform[totalFrames];
                for (var frameIndex = 0; frameIndex < totalFrames; frameIndex++)
                {
                    var bases = basesByFrame[frameIndex];
                    transforms[frameIndex] = elementIndex < bases.Count
                        ? ConvertFrame(bases[elementIndex], frames.Count == 0 ? null : frames[0], previous, frameIndex, layer.Name, project)
                        : HideFrame(previous);
                }
                tracks.Add(new ReanimTrack
                {
                    Name = layer.Name + (elementIndex == 0 ? "" : (elementIndex + 1).ToString(CultureInfo.InvariantCulture)),
                    Transforms = [.. transforms],
                });
            }
        }
        return new ReanimFile
        {
            Fps = ToFloat(project.FrameRate),
            Tracks = tracks,
        };
    }

    private static ReanimTransform HideFrame(FrameState previous)
    {
        var transform = new ReanimTransform();
        if (previous.Frame != -1)
        {
            previous.Frame = -1;
            transform.Frame = -1;
        }
        return transform;
    }

    private static ReanimTransform ConvertFrame(BaseResult result, XflFrame? root, FrameState previous,
        int frameIndex, string layerName, XflProject project)
    {
        var element = result.Element;
        var matrix = result.Matrix;
        var parts = XflMatrixParts.Decompose(matrix);
        var skewX = parts.XAngle * RadiansToDegrees;
        var skewY = -parts.YAngle * RadiansToDegrees;
        if (frameIndex == 0)
        {
            if (skewX < 0)
            {
                skewX += 360;
            }
            if (skewY < 0)
            {
                skewY += 360;
            }
        }
        else
        {
            while (previous.SkewX - skewX > 180)
            {
                skewX += 360;
            }
            while (previous.SkewX - skewX < -180)
            {
                skewX -= 360;
            }
            while (previous.SkewY - skewY > 180)
            {
                skewY += 360;
            }
            while (previous.SkewY - skewY < -180)
            {
                skewY -= 360;
            }
        }

        var image = "";
        var font = "";
        var text = "";
        if (layerName is "_ground" || layerName == "fullscreen" || layerName.StartsWith("locator", StringComparison.Ordinal))
        {
        }
        else if (layerName.StartsWith("attacher__", StringComparison.Ordinal))
        {
            var symbolItem = result.Symbol is null ? null : project.FindLibraryItem(result.Symbol.LibraryItemName);
            var elementItem = project.FindLibraryItem(element.LibraryItemName);
            if (symbolItem is not null && symbolItem.Name.Contains("__", StringComparison.Ordinal))
            {
                text = symbolItem.Name;
            }
            else if (elementItem is not null && elementItem.Name.Contains("__", StringComparison.Ordinal))
            {
                text = elementItem.Name;
            }
            else if (elementItem is not null)
            {
                text = BeforeFirstDot(elementItem.Name);
                if (!text.StartsWith("attacher__", StringComparison.Ordinal))
                {
                    image = "IMAGE_REANIM_" + text;
                    text = "";
                }
            }
        }
        else if (element.IsText)
        {
            var run = XflElement.Child(XflElement.Child(element.Node, "textRuns"), "DOMTextRun");
            if (run is not null)
            {
                var attrs = XflElement.Child(XflElement.Child(run, "textAttrs"), "DOMTextAttrs");
                var family = "";
                if (attrs is not null)
                {
                    var url = XflElement.Attr(attrs, "url");
                    family = string.IsNullOrEmpty(url)
                        ? XflElement.Attr(attrs, "face") + Round(XmlNumbers.ParseDouble(XflElement.Attr(attrs, "size"), 0) * 0.8, 0).ToString(CultureInfo.InvariantCulture)
                        : url;
                }
                font = "FONT_" + family;
                text = XflElement.Child(run, "characters")?.Value ?? "";
                matrix = new XflMatrix(matrix.A, matrix.B, matrix.C, matrix.D, matrix.Tx - element.Matrix.Tx, matrix.Ty);
            }
        }
        else
        {
            var item = project.FindLibraryItem(element.LibraryItemName);
            var emptySymbol = item?.Timeline is not null
                && item.Timeline.Layers.Count > 0
                && item.Timeline.Layers[0].FrameAt(0)?.Elements.Count == 0;
            if (item is not null && !emptySymbol)
            {
                image = "IMAGE_REANIM_" + BeforeFirstDot(item.Name);
            }
            else if (item is null && root?.Elements.Count == 1)
            {
                var rootItem = project.FindLibraryItem(root.Elements[0].LibraryItemName);
                if (rootItem is not null)
                {
                    image = "IMAGE_REANIM_" + rootItem.Name;
                }
            }
        }

        var x = Round(matrix.Tx, 1);
        var y = Round(matrix.Ty, 1);
        var roundedSkewX = Round(skewX, 1);
        var roundedSkewY = Round(skewY, 1);
        var scaleX = Round(parts.ScaleX, 3);
        var scaleY = Round(parts.ScaleY, 3);
        var alpha = Round(result.Alpha, 2);
        const float Visible = 0;

        var transform = new ReanimTransform();
        if (previous.X != x)
        {
            previous.X = x;
            transform.X = x;
        }
        if (previous.Y != y)
        {
            previous.Y = y;
            transform.Y = y;
        }
        if (previous.SkewX != roundedSkewX)
        {
            previous.SkewX = roundedSkewX;
            transform.SkewX = roundedSkewX;
        }
        if (previous.SkewY != roundedSkewY)
        {
            previous.SkewY = roundedSkewY;
            transform.SkewY = roundedSkewY;
        }
        if (previous.ScaleX != scaleX)
        {
            previous.ScaleX = scaleX;
            transform.ScaleX = scaleX;
        }
        if (previous.ScaleY != scaleY)
        {
            previous.ScaleY = scaleY;
            transform.ScaleY = scaleY;
        }
        if (previous.Frame != Visible)
        {
            previous.Frame = Visible;
            transform.Frame = Visible;
        }
        if (previous.Alpha != alpha)
        {
            previous.Alpha = alpha;
            transform.Alpha = alpha;
        }
        image = image.ToUpperInvariant();
        font = font.ToUpperInvariant();
        if (previous.Image != image)
        {
            previous.Image = image;
            if (!string.IsNullOrEmpty(image))
            {
                transform.Image = ImageReference.FromName(image);
            }
        }
        if (previous.Font != font)
        {
            previous.Font = font;
            transform.Font = font;
        }
        if (previous.Text != text)
        {
            previous.Text = text;
            transform.Text = string.IsNullOrEmpty(text) ? "_" : text;
        }
        return transform;
    }

    private static List<BaseResult> FindBases(XflFrame frame, XflProject project)
    {
        var result = new List<BaseResult>();
        foreach (var element in frame.Elements)
        {
            CollectBases(element, XflMatrix.Identity, 1, null, result, project, 0);
        }
        return result;
    }

    private static void CollectBases(XflElement element, XflMatrix outer, double outerAlpha, XflElement? symbol,
        List<BaseResult> result, XflProject project, int depth)
    {
        var combined = XflMatrix.Concat(element.Matrix, outer);
        var combinedAlpha = element.Alpha * outerAlpha;
        if (depth >= 64 || !element.IsInstance)
        {
            result.Add(new BaseResult(element, symbol, combined, combinedAlpha));
            return;
        }

        var item = project.FindLibraryItem(element.LibraryItemName);
        if (item?.Timeline is null || (item.ItemType != "movie clip" && item.ItemType != "graphic"))
        {
            result.Add(new BaseResult(element, symbol, combined, combinedAlpha));
            return;
        }

        var foundNested = false;
        foreach (var layer in item.Timeline.Layers)
        {
            var frame = layer.FrameAt(0);
            if (frame is null)
            {
                continue;
            }
            foreach (var nested in frame.Elements)
            {
                foundNested = true;
                CollectBases(nested, combined, combinedAlpha, element, result, project, depth + 1);
            }
        }
        if (!foundNested)
        {
            result.Add(new BaseResult(element, symbol, combined, combinedAlpha));
        }
    }

    private static float Round(double value, int places)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            value = 0;
        }
        return (float)Math.Round(value, places, MidpointRounding.AwayFromZero);
    }

    private static float ToFloat(double value) =>
        double.IsNaN(value) || double.IsInfinity(value) ? 0 : (float)value;

    private static string BeforeFirstDot(string value)
    {
        var dot = value.IndexOf('.');
        return dot < 0 ? value : value[..dot];
    }

    private sealed class FrameState
    {
        public float X;
        public float Y;
        public float SkewX;
        public float SkewY;
        public float ScaleX = 1;
        public float ScaleY = 1;
        public float Frame;
        public float Alpha = 1;
        public string Image = "";
        public string Font = "";
        public string Text = "";
    }

    private sealed class BaseResult(XflElement element, XflElement? symbol, XflMatrix matrix, double alpha)
    {
        public XflElement Element { get; } = element;

        public XflElement? Symbol { get; } = symbol;

        public XflMatrix Matrix { get; } = matrix;

        public double Alpha { get; } = alpha;
    }
}
