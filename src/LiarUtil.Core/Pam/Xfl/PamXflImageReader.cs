using System.Xml.Linq;

namespace LiarUtil.Core.Pam.Xfl;

internal static class PamXflImageReader
{
    public static PamImage Read(XElement document, PamXflExtraImage extra, int index, int version)
    {
        var context = string.Format(LiarUtil.Core.Strings.ImageSymbol0, index + 1);
        if (document.Name.LocalName != "DOMSymbolItem")
        {
            throw new PamXflException(string.Format(LiarUtil.Core.Strings.RootNodeOf0MustDOMSymbolItem, context));
        }
        if (PamXflXml.Text(document, "name") != PamXflNames.ImageSymbol(index))
        {
            throw new PamXflException(string.Format(LiarUtil.Core.Strings.NameAttributeOf0InconsistentWithExtraJson, context));
        }

        var timeline = PamXflXml.RequireChild(document, "timeline", context);
        var domTimeline = PamXflXml.RequireChild(timeline, "DOMTimeline", context);
        var layer = PamXflXml.RequireChild(PamXflXml.RequireChild(domTimeline, "layers", context), "DOMLayer", context);
        var frame = PamXflXml.RequireChild(PamXflXml.RequireChild(layer, "frames", context), "DOMFrame", context);
        var elements = PamXflXml.RequireChild(frame, "elements", context);
        var instance = PamXflXml.RequireChild(elements, "DOMSymbolInstance", context);
        if (PamXflXml.Text(instance, "libraryItemName") != PamXflNames.SourceSymbol(index))
        {
            throw new PamXflException(string.Format(LiarUtil.Core.Strings.N0DoesNotReference1, context, PamXflNames.SourceSymbol(index)));
        }

        var matrix = ParseInstanceMatrix(instance, context);
        var size = extra.Size is { Length: >= 2 } ? extra.Size : null;
        return new PamImage
        {
            Name = extra.Name,
            Width = version < 4 ? null : size?[0],
            Height = version < 4 ? null : size?[1],
            Transform = version < 2
                ? PamXflTransform.ToRotate(matrix)
                : new PamMatrixTransform
                {
                    A = (float)matrix.A,
                    B = (float)matrix.B,
                    C = (float)matrix.C,
                    D = (float)matrix.D,
                    X = (float)matrix.X,
                    Y = (float)matrix.Y,
                },
        };
    }

    public static PamXflMatrix ParseInstanceMatrix(XElement instance, string context) =>
        PamXflXml.Child(instance, "matrix") is { } matrix
            ? ParseMatrix(matrix, context)
            : PamXflMatrix.Identity;

    public static PamXflMatrix ParseMatrix(XElement matrix, string context)
    {
        var value = PamXflXml.RequireChild(matrix, "Matrix", context);
        return new PamXflMatrix(
            PamXflXml.Double(value, "a", 1),
            PamXflXml.Double(value, "b", 0),
            PamXflXml.Double(value, "c", 0),
            PamXflXml.Double(value, "d", 1),
            PamXflXml.Double(value, "tx", 0),
            PamXflXml.Double(value, "ty", 0));
    }

    public static bool TryParseInstance(string libraryName, out int resource, out bool sprite)
    {
        var match = PamXflXml.ImageSymbolPattern().Match(libraryName);
        if (match.Success && match.Groups[1].Value == match.Groups[2].Value)
        {
            resource = int.Parse(match.Groups[3].Value) - 1;
            sprite = match.Groups[1].Value == PamXflConstants.SpriteLayerName;
            return true;
        }

        resource = -1;
        sprite = false;
        return false;
    }
}
