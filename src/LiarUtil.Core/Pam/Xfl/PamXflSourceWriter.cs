using System.Xml.Linq;

namespace LiarUtil.Core.Pam.Xfl;

internal static class PamXflSourceWriter
{
    public static XElement Create(PamImage image, int index, int resolution)
    {
        var scale = (double)PamXflConstants.StandardResolution / resolution;
        return new XElement("DOMSymbolItem",
            PamXflXml.XsiAttribute,
            new XAttribute("name", PamXflNames.SourceSymbol(index)),
            new XAttribute("symbolType", PamXflConstants.SymbolType),
            new XElement("timeline",
                new XElement("DOMTimeline",
                    new XAttribute("name", "source_" + (index + 1)),
                    new XElement("layers",
                        new XElement("DOMLayer",
                            new XElement("frames",
                                new XElement("DOMFrame",
                                    new XAttribute("index", 0),
                                    new XElement("elements",
                                        new XElement("DOMBitmapInstance",
                                            new XAttribute("libraryItemName", PamXflConstants.MediaPrefix + PamXflNames.MediaName(image.Name)),
                                            new XElement("matrix",
                                                new XElement("Matrix",
                                                    new XAttribute("a", PamXflXml.Number(scale)),
                                                    new XAttribute("d", PamXflXml.Number(scale)))))))))))));
    }
}
