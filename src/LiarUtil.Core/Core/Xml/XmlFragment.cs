using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace LiarUtil.Core.Core.Xml;

public static class XmlFragment
{
    private static readonly XmlWriterSettings FragmentSettings = new()
    {
        Indent = true,
        IndentChars = "  ",
        OmitXmlDeclaration = true,
        ConformanceLevel = ConformanceLevel.Fragment,
    };

    public static string Render(IEnumerable<XElement> elements)
    {
        var builder = new StringBuilder();
        using (var writer = XmlWriter.Create(builder, FragmentSettings))
        {
            var first = true;
            foreach (var element in elements)
            {
                if (!first)
                {
                    writer.WriteWhitespace("\n");
                }
                first = false;
                element.WriteTo(writer);
            }
        }

        builder.Append('\n');
        return builder.ToString();
    }

    public static XElement Parse(string text)
    {
        try
        {
            return ParseDocument(text);
        }
        catch (XmlException)
        {
            return ParseDocument(text.Replace("&", "&amp;", StringComparison.Ordinal));
        }
    }

    private static XElement ParseDocument(string text)
    {
        var document = XDocument.Parse("<root>" + text + "</root>", LoadOptions.None);
        return document.Root ?? throw new InvalidDataException(LiarUtil.Core.Strings.XMLContentInvalid);
    }
}
