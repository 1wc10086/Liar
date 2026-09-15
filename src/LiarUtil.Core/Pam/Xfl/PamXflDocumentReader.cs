using System.Xml.Linq;

namespace LiarUtil.Core.Pam.Xfl;

internal static class PamXflDocumentReader
{
    public static void Read(XElement document, PamAnimation animation)
    {
        if (document.Name.LocalName != "DOMDocument")
        {
            throw new PamXflException(LiarUtil.Core.Strings.RootNodeOfDOMDocumentXmlMustDOMDocument);
        }

        animation.Width = (float)PamXflXml.Double(document, "width", 0);
        animation.Height = (float)PamXflXml.Double(document, "height", 0);
        if (animation.MainSprite is not { } mainSprite)
        {
            return;
        }

        var timelines = PamXflXml.RequireChild(document, "timelines", "DOMDocument");
        var timeline = PamXflXml.RequireChild(timelines, "DOMTimeline", "DOMDocument");
        var layers = PamXflXml.ChildList(PamXflXml.RequireChild(timeline, "layers", "DOMDocument"), "DOMLayer");
        if (layers.Count != 3)
        {
            throw new PamXflException(LiarUtil.Core.Strings.AnimationTimelineMustContainFlowCommandAndSprite);
        }

        ReadFlow(layers[0], mainSprite);
        ReadCommand(layers[1], mainSprite);
    }

    private static void ReadFlow(XElement layer, PamSprite sprite)
    {
        foreach (var node in Frames(layer))
        {
            var frameIndex = PamXflXml.RequireInt(node, "index", LiarUtil.Core.Strings.FlowLayer);
            if (node.Attribute("name") is not null)
            {
                if (PamXflXml.Text(node, "labelType") != PamXflConstants.NameLabelType)
                {
                    throw new PamXflException(string.Format(LiarUtil.Core.Strings.LabelTypeOfFlowLayerFrame0MustName, frameIndex));
                }
                FrameAt(sprite, frameIndex).Label = PamXflXml.Text(node, "name");
            }

            var actionScript = PamXflXml.Child(node, "Actionscript");
            if (actionScript is null)
            {
                continue;
            }

            if (ScriptText(actionScript, string.Format(LiarUtil.Core.Strings.FlowLayerFrame0, frameIndex)).Trim() != PamXflConstants.StopScript)
            {
                throw new PamXflException(string.Format(LiarUtil.Core.Strings.ScriptOfFlowLayerFrame0MustStop, frameIndex));
            }
            FrameAt(sprite, frameIndex).Stop = true;
        }
    }

    private static void ReadCommand(XElement layer, PamSprite sprite)
    {
        foreach (var node in Frames(layer))
        {
            var frameIndex = PamXflXml.RequireInt(node, "index", LiarUtil.Core.Strings.CommandLayer);
            var actionScript = PamXflXml.Child(node, "Actionscript");
            if (actionScript is null)
            {
                continue;
            }

            foreach (var line in ScriptText(actionScript, string.Format(LiarUtil.Core.Strings.CommandLayerFrame0, frameIndex)).Trim().Split('\n'))
            {
                var match = PamXflXml.CommandPattern().Match(line.Trim());
                if (!match.Success)
                {
                    throw new PamXflException(string.Format(LiarUtil.Core.Strings.CommandLayerFrame0ContainsUnparsableCommand1, frameIndex, line.Trim()));
                }
                FrameAt(sprite, frameIndex).Commands.Add(new PamCommand
                {
                    Command = match.Groups[1].Value,
                    Argument = match.Groups[2].Value,
                });
            }
        }
    }

    private static IReadOnlyList<XElement> Frames(XElement layer) =>
        PamXflXml.ChildList(PamXflXml.RequireChild(layer, "frames", LiarUtil.Core.Strings.FlowCommandLayer), "DOMFrame");

    private static string ScriptText(XElement actionScript, string context)
    {
        var script = PamXflXml.RequireChild(actionScript, "script", context);
        var nodes = script.Nodes().ToList();
        if (nodes is not [XCData data])
        {
            throw new PamXflException(string.Format(LiarUtil.Core.Strings.ScriptOf0MustCDATAText, context));
        }
        return data.Value;
    }

    private static PamFrame FrameAt(PamSprite sprite, int index)
    {
        while (sprite.Frames.Count <= index)
        {
            sprite.Frames.Add(new PamFrame());
        }
        return sprite.Frames[index];
    }
}
