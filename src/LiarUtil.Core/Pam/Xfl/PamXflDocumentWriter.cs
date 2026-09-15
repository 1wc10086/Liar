using System.Xml.Linq;

namespace LiarUtil.Core.Pam.Xfl;

internal static class PamXflDocumentWriter
{
    public static XElement Create(PamAnimation animation)
    {
        var frames = animation.MainSprite?.Frames ?? [];
        var flow = new List<XElement>();
        var command = new List<XElement>();
        var sprite = new List<XElement>();
        if (animation.MainSprite is not null)
        {
            BuildFlow(flow, frames);
            BuildCommand(command, frames);
            BuildSprite(sprite, frames.Count);
        }

        return new XElement("DOMDocument",
            PamXflXml.XsiAttribute,
            new XAttribute("frameRate", PamXflXml.Scalar(animation.MainSprite?.FrameRate ?? animation.FrameRate)),
            new XAttribute("width", PamXflXml.Scalar(animation.Width)),
            new XAttribute("height", PamXflXml.Scalar(animation.Height)),
            new XAttribute("xflVersion", PamXflConstants.DocumentVersion),
            BuildFolders(),
            BuildMedia(animation.Images),
            BuildSymbols(animation),
            BuildTimelines(flow, command, sprite));
    }

    private static XElement BuildFolders() => new("folders",
        new[] { PamXflConstants.MediaFolder, PamXflConstants.SourceFolder, PamXflConstants.ImageFolder, PamXflConstants.SpriteFolder }
            .Select(name => new XElement("DOMFolderItem",
                new XAttribute("name", name),
                new XAttribute("isExpanded", "true"))));

    private static XElement BuildMedia(IReadOnlyList<PamImage> images) => new("media",
        images.Select(image =>
        {
            var name = PamXflNames.MediaName(image.Name);
            return new XElement("DOMBitmapItem",
                new XAttribute("name", PamXflConstants.MediaPrefix + name),
                new XAttribute("href", PamXflConstants.MediaPrefix + name + PamXflConstants.ImageFileSuffix));
        }));

    private static XElement BuildSymbols(PamAnimation animation) => new("symbols",
        animation.Images.Select((_, index) => Include(PamXflNames.SourceSymbol(index) + PamXflConstants.IncludeFileSuffix))
            .Concat(animation.Images.Select((_, index) => Include(PamXflNames.ImageSymbol(index) + PamXflConstants.IncludeFileSuffix)))
            .Concat(animation.Sprites.Select((_, index) => Include(PamXflNames.SpriteSymbol(index) + PamXflConstants.IncludeFileSuffix)))
            .Append(Include(PamXflConstants.MainSpriteFileName)));

    private static XElement Include(string href) => new("Include", new XAttribute("href", href));

    private static XElement BuildTimelines(IReadOnlyList<XElement> flow, IReadOnlyList<XElement> command, IReadOnlyList<XElement> sprite) =>
        new("timelines",
            new XElement("DOMTimeline",
                new XAttribute("name", PamXflConstants.AnimationTimelineName),
                new XElement("layers",
                    Layer(PamXflConstants.FlowLayerName, flow),
                    Layer(PamXflConstants.CommandLayerName, command),
                    Layer(PamXflConstants.SpriteLayerName, sprite))));

    private static XElement Layer(string name, IReadOnlyList<XElement> frames) =>
        new("DOMLayer", new XAttribute("name", name), new XElement("frames", frames));

    private static void BuildFlow(ICollection<XElement> flow, IReadOnlyList<PamFrame> frames)
    {
        var end = -1;
        for (var index = 0; index < frames.Count; index++)
        {
            var frame = frames[index];
            if (frame.Label is null && !frame.Stop)
            {
                continue;
            }

            if (end + 1 < index)
            {
                flow.Add(Frame(end + 1, index - (end + 1), true));
            }

            var node = new XElement("DOMFrame",
                new XAttribute("index", index),
                new XElement("elements"));
            if (frame.Label is { } label)
            {
                node.SetAttributeValue("name", label);
                node.SetAttributeValue("labelType", PamXflConstants.NameLabelType);
            }
            if (frame.Stop)
            {
                node.AddFirst(new XElement("Actionscript",
                    new XElement("script", new XCData(PamXflConstants.StopScript))));
            }
            flow.Add(node);
            end = index;
        }

        if (end + 1 < frames.Count)
        {
            flow.Add(Frame(end + 1, frames.Count - (end + 1), false));
        }
    }

    private static void BuildCommand(ICollection<XElement> command, IReadOnlyList<PamFrame> frames)
    {
        var end = -1;
        for (var index = 0; index < frames.Count; index++)
        {
            var frame = frames[index];
            if (frame.Commands.Count == 0)
            {
                continue;
            }

            if (end + 1 < index)
            {
                command.Add(Frame(end + 1, index - (end + 1), false));
            }

            var script = string.Join("\n", frame.Commands.Select(item =>
                string.Format(PamXflConstants.CommandFormat, item.Command, item.Argument)));
            command.Add(new XElement("DOMFrame",
                new XAttribute("index", index),
                new XElement("Actionscript",
                    new XElement("script", new XCData(script)))));
            end = index;
        }

        if (end + 1 < frames.Count)
        {
            command.Add(Frame(end + 1, frames.Count - (end + 1), false));
        }
    }

    private static void BuildSprite(ICollection<XElement> sprite, int frameCount) =>
        sprite.Add(new XElement("DOMFrame",
            new XAttribute("index", 0),
            new XAttribute("duration", frameCount),
            new XElement("elements",
                new XElement("DOMSymbolInstance",
                    new XAttribute("libraryItemName", PamXflConstants.MainSpriteName),
                    new XAttribute("symbolType", PamXflConstants.SymbolType),
                    new XAttribute("loop", PamXflConstants.LoopType)))));

    private static XElement Frame(int index, int duration, bool withElements)
    {
        var frame = new XElement("DOMFrame",
            new XAttribute("index", index),
            new XAttribute("duration", duration));
        if (withElements)
        {
            frame.Add(new XElement("elements"));
        }
        return frame;
    }
}
