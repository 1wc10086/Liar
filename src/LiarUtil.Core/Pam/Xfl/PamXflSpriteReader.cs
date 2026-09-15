using System.Xml.Linq;

namespace LiarUtil.Core.Pam.Xfl;

internal static class PamXflSpriteReader
{
    public static PamSprite Read(XElement document, PamXflExtraSprite extra, int? index, float documentFrameRate)
    {
        var context = index is null ? LiarUtil.Core.Strings.MainSpriteSymbol : string.Format(LiarUtil.Core.Strings.SpriteSymbol0, index.Value + 1);
        var expectedName = index is null ? PamXflConstants.MainSpriteName : PamXflNames.SpriteSymbol(index.Value);
        if (document.Name.LocalName != "DOMSymbolItem")
        {
            throw new PamXflException(string.Format(LiarUtil.Core.Strings.RootNodeOf0MustDOMSymbolItem, context));
        }
        if (PamXflXml.Text(document, "name") != expectedName)
        {
            throw new PamXflException(string.Format(LiarUtil.Core.Strings.NameAttributeOf0Should1, context, expectedName));
        }

        var domTimeline = PamXflXml.RequireChild(
            PamXflXml.RequireChild(document, "timeline", context), "DOMTimeline", context);
        var layerNodes = PamXflXml.ChildList(PamXflXml.RequireChild(domTimeline, "layers", context), "DOMLayer");
        layerNodes.Reverse();
        var frames = new List<PamFrame>();
        var layerCount = 0;
        var maxIndex = 0;
        foreach (var layer in layerNodes)
        {
            var layerContext = string.Format(LiarUtil.Core.Strings.Layer1Of0, context, PamXflXml.Text(layer, "name"));
            PamXflLayerState? state = null;
            foreach (var node in PamXflXml.ChildList(PamXflXml.RequireChild(layer, "frames", layerContext), "DOMFrame"))
            {
                var frameIndex = PamXflXml.RequireInt(node, "index", layerContext);
                var frameDuration = ReadDuration(node);
                var elements = PamXflXml.Child(node, "elements");
                if (elements is null)
                {
                    state = CloseLayer(state, frames, ref maxIndex);
                    continue;
                }

                var instance = PamXflXml.Child(elements, "DOMSymbolInstance");
                if (instance is null)
                {
                    continue;
                }

                maxIndex = Math.Max(maxIndex, frameIndex);

                var libraryName = PamXflXml.Text(instance, "libraryItemName");
                if (!PamXflImageReader.TryParseInstance(libraryName, out var resource, out var sprite))
                {
                    throw new PamXflException(string.Format(LiarUtil.Core.Strings.Instance1Of0NotValidImageOr, layerContext, libraryName));
                }

                var matrix = PamXflImageReader.ParseInstanceMatrix(instance, layerContext);
                var color = ParseColor(instance);
                var target = FrameAt(frames, frameIndex);
                if (state is null)
                {
                    state = new PamXflLayerState
                    {
                        Index = layerCount,
                        Resource = resource,
                        Sprite = sprite,
                        Color = PamXflConstants.InitialColor,
                    };
                    target.Appends.Add(new PamLayerAppend
                    {
                        Index = state.Index,
                        Name = null,
                        Resource = resource,
                        Sprite = sprite,
                        Additive = false,
                        PreloadFrame = 0,
                        TimeScale = 1f,
                    });
                    layerCount++;
                }
                else if (state.Resource != resource || state.Sprite != sprite)
                {
                    throw new PamXflException(string.Format(LiarUtil.Core.Strings.N0ChangedItsInstanceTypeAtFrame1, layerContext, frameIndex));
                }

                state.FrameStart = frameIndex;
                state.FrameDuration = frameDuration;
                var colorChanged = !state.Color.Matches(color);
                if (colorChanged)
                {
                    state.Color = color;
                }

                target.Changes.Add(new PamLayerChange
                {
                    Index = state.Index,
                    Transform = PamXflTransform.ToVariant(matrix),
                    Color = colorChanged ? ToPamColor(color) : null,
                    SpriteFrameNumber = null,
                    SourceRectangle = null,
                });
            }

            CloseLayer(state, frames, ref maxIndex);
        }

        if (frames.Count > maxIndex)
        {
            frames.RemoveRange(maxIndex, frames.Count - maxIndex);
        }

        return new PamSprite
        {
            Name = extra.Name,
            FrameRate = documentFrameRate,
            WorkAreaStart = 0,
            WorkAreaDuration = frames.Count,
            Frames = frames,
        };
    }

    private static PamXflLayerState? CloseLayer(PamXflLayerState? state, List<PamFrame> frames, ref int maxIndex)
    {
        if (state is null)
        {
            return null;
        }

        var removal = state.FrameStart + state.FrameDuration;
        maxIndex = Math.Max(maxIndex, removal);
        FrameAt(frames, removal).Removes.Add(new PamLayerRemove { Index = state.Index });
        return null;
    }

    private static int ReadDuration(XElement node)
    {
        var duration = PamXflXml.Int(node, "duration", 1);
        return duration > 0 ? duration : 1;
    }

    private static PamXflColor ParseColor(XElement instance)
    {
        var color = PamXflXml.Child(instance, "color");
        var value = color is null ? null : PamXflXml.Child(color, "Color");
        return value is null
            ? PamXflConstants.InitialColor
            : new PamXflColor(
                Compute(value, "redMultiplier", "redOffset"),
                Compute(value, "greenMultiplier", "greenOffset"),
                Compute(value, "blueMultiplier", "blueOffset"),
                Compute(value, "alphaMultiplier", "alphaOffset"));
    }

    private static double Compute(XElement color, string multiplierName, string offsetName)
    {
        var multiplier = PamXflXml.TryDouble(PamXflXml.Text(color, multiplierName), 1);
        var offset = PamXflXml.TryDouble(PamXflXml.Text(color, offsetName), 0);
        return Math.Clamp(multiplier * 255 + offset, 0, 255) / 255;
    }

    private static PamColor ToPamColor(PamXflColor color) => new()
    {
        Red = (float)color.Red,
        Green = (float)color.Green,
        Blue = (float)color.Blue,
        Alpha = (float)color.Alpha,
    };

    private static PamFrame FrameAt(List<PamFrame> frames, int index)
    {
        while (frames.Count <= index)
        {
            frames.Add(new PamFrame());
        }
        return frames[index];
    }
}

internal sealed class PamXflLayerState
{
    public int Index { get; init; }

    public int Resource { get; init; }

    public bool Sprite { get; init; }

    public int FrameStart { get; set; }

    public int FrameDuration { get; set; }

    public PamXflColor Color { get; set; }
}
