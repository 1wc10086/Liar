using System.Xml.Linq;

namespace LiarUtil.Core.Pam.Xfl;

internal static class PamXflSpriteWriter
{
    public static XElement Create(PamSprite sprite, int? index, IReadOnlyList<PamSprite> resources)
    {
        var models = new Dictionary<int, PamXflLayerModel>();
        var frames = new Dictionary<int, List<XElement>>();
        for (var frameIndex = 0; frameIndex < sprite.Frames.Count; frameIndex++)
        {
            ApplyFrame(sprite.Frames[frameIndex], frameIndex, models, frames, resources);
        }

        foreach (var (layerIndex, model) in models)
        {
            var nodes = frames[layerIndex];
            if (nodes.Count > 0)
            {
                nodes[^1].SetAttributeValue("duration", model.FrameDuration);
            }
        }

        var layers = frames.Keys
            .OrderByDescending(layerIndex => layerIndex)
            .Select(layerIndex => new XElement("DOMLayer",
                new XAttribute("name", layerIndex + 1),
                new XElement("frames", frames[layerIndex])));

        var symbolName = index is null ? PamXflConstants.MainSpriteName : PamXflNames.SpriteSymbol(index.Value);
        var timelineName = index is null ? PamXflConstants.MainSpriteName : "sprite_" + (index.Value + 1);
        return new XElement("DOMSymbolItem",
            PamXflXml.XsiAttribute,
            new XAttribute("name", symbolName),
            new XAttribute("symbolType", PamXflConstants.SymbolType),
            new XElement("timeline",
                new XElement("DOMTimeline",
                    new XAttribute("name", timelineName),
                    new XElement("layers", layers))));
    }

    private static void ApplyFrame(
        PamFrame frame,
        int frameIndex,
        Dictionary<int, PamXflLayerModel> models,
        Dictionary<int, List<XElement>> frames,
        IReadOnlyList<PamSprite> resources)
    {
        foreach (var remove in frame.Removes)
        {
            if (models.TryGetValue(remove.Index, out var removed))
            {
                removed.State = false;
            }
        }

        foreach (var append in frame.Appends)
        {
            if (append.Sprite && (append.Resource < 0 || append.Resource >= resources.Count))
            {
                throw new PamXflException(string.Format(LiarUtil.Core.Strings.PAMSpriteFrame0ReferencesOutOfRange, frameIndex, append.Resource));
            }

            models[append.Index] = new PamXflLayerModel
            {
                State = null,
                Resource = append.Resource,
                Sprite = append.Sprite,
                Transform = PamXflMatrix.Identity,
                Color = PamXflConstants.InitialColor,
                FrameStart = frameIndex,
                FrameDuration = frameIndex,
            };
            frames[append.Index] = [];
            if (frameIndex > 0)
            {
                frames[append.Index].Add(Frame(0, frameIndex));
            }
        }

        foreach (var change in frame.Changes)
        {
            if (!models.TryGetValue(change.Index, out var layer))
            {
                throw new PamXflException(string.Format(LiarUtil.Core.Strings.PAMSpriteFrame0ModifiesLayerThatDoes, frameIndex, change.Index));
            }

            layer.State = true;
            layer.Transform = PamXflTransform.FromVariant(change.Transform);
            if (change.Color is { } color)
            {
                layer.Color = new PamXflColor(color.Red, color.Green, color.Blue, color.Alpha);
            }
        }

        foreach (var layerIndex in models.Keys.ToList())
        {
            var layer = models[layerIndex];
            var nodes = frames[layerIndex];
            if (layer.State is not null && nodes.Count > 0)
            {
                nodes[^1].SetAttributeValue("duration", layer.FrameDuration);
            }

            if (layer.State is true)
            {
                nodes.Add(new XElement("DOMFrame",
                    new XAttribute("index", frameIndex),
                    new XAttribute("duration", ""),
                    new XElement("elements", CreateInstance(layer, frameIndex, resources))));
                layer.State = null;
                layer.FrameDuration = 0;
            }

            if (layer.State is false)
            {
                models.Remove(layerIndex);
                continue;
            }

            layer.FrameDuration++;
        }
    }

    private static XElement Frame(int index, int duration) =>
        new("DOMFrame",
            new XAttribute("index", index),
            new XAttribute("duration", duration),
            new XElement("elements"));

    private static XElement CreateInstance(PamXflLayerModel layer, int frameIndex, IReadOnlyList<PamSprite> resources)
    {
        var instance = new XElement("DOMSymbolInstance");
        if (layer.Sprite)
        {
            var count = Math.Max(1, resources[layer.Resource].Frames.Count);
            instance.Add(
                new XAttribute("libraryItemName", PamXflNames.SpriteSymbol(layer.Resource)),
                new XAttribute("symbolType", PamXflConstants.SymbolType),
                new XAttribute("loop", PamXflConstants.LoopType),
                new XAttribute("firstFrame", (frameIndex - layer.FrameStart) % count));
        }
        else
        {
            instance.Add(
                new XAttribute("libraryItemName", PamXflNames.ImageSymbol(layer.Resource)),
                new XAttribute("symbolType", PamXflConstants.SymbolType),
                new XAttribute("loop", PamXflConstants.LoopType));
        }

        instance.Add(
            PamXflXml.Matrix(layer.Transform),
            new XElement("color",
                new XElement("Color",
                    new XAttribute("redMultiplier", PamXflXml.Number(layer.Color.Red)),
                    new XAttribute("greenMultiplier", PamXflXml.Number(layer.Color.Green)),
                    new XAttribute("blueMultiplier", PamXflXml.Number(layer.Color.Blue)),
                    new XAttribute("alphaMultiplier", PamXflXml.Number(layer.Color.Alpha)))));
        return instance;
    }
}

internal sealed class PamXflLayerModel
{
    public bool? State { get; set; }

    public int Resource { get; init; }

    public bool Sprite { get; init; }

    public PamXflMatrix Transform { get; set; }

    public PamXflColor Color { get; set; }

    public int FrameStart { get; init; }

    public int FrameDuration { get; set; }
}
