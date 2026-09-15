using System.Xml.Linq;

namespace LiarUtil.Core.Pam.Xfl;

internal static class PamXflImageWriter
{
    public static XElement Create(PamImage image, int index)
    {
        var matrix = PamXflTransform.FromVariant(image.Transform);
        return new XElement("DOMSymbolItem",
            PamXflXml.XsiAttribute,
            new XAttribute("name", PamXflNames.ImageSymbol(index)),
            new XAttribute("symbolType", PamXflConstants.SymbolType),
            new XElement("timeline",
                new XElement("DOMTimeline",
                    new XAttribute("name", "image_" + (index + 1)),
                    new XElement("layers",
                        new XElement("DOMLayer",
                            new XElement("frames",
                                new XElement("DOMFrame",
                                    new XAttribute("index", 0),
                                    new XElement("elements",
                                        new XElement("DOMSymbolInstance",
                                            new XAttribute("libraryItemName", PamXflNames.SourceSymbol(index)),
                                            new XAttribute("symbolType", PamXflConstants.SymbolType),
                                            new XAttribute("loop", PamXflConstants.LoopType),
                                            PamXflXml.Matrix(matrix),
                                            new XElement("transformationPoint",
                                                new XElement("Point",
                                                    new XAttribute("x", PamXflXml.Number(-matrix.X)),
                                                    new XAttribute("y", PamXflXml.Number(-matrix.Y)))))))))))));
    }
}
