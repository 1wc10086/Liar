using System.Xml.Linq;
using LiarUtil.Core.Core.Xml;

namespace LiarUtil.Core.PopCap;

public sealed class ImageIdMap
{
    private readonly Dictionary<int, string> _idToName = [];

    private readonly Dictionary<string, int> _nameToId = new(StringComparer.Ordinal);

    public string Name { get; private set; } = "";

    public bool IsEmpty => _idToName.Count == 0;

    public int Count => _idToName.Count;

    public static ImageIdMap Empty { get; } = new();

    public static ImageIdMap Load(string xml)
    {
        var map = new ImageIdMap();
        map.LoadFrom(xml);
        return map;
    }

    public static ImageIdMap LoadFromFile(string path)
    {
        var map = new ImageIdMap();
        map.LoadFromFileContent(path);
        return map;
    }

    public void LoadFrom(string xml)
    {
        var document = XDocument.Parse(xml, LoadOptions.None);
        var root = document.Root ?? throw new InvalidDataException(LiarUtil.Core.Strings.ImageStringContentInvalid);
        if (!string.Equals(root.Name.LocalName, "ImageString", StringComparison.Ordinal))
        {
            throw new InvalidDataException(LiarUtil.Core.Strings.ImageStringRootNodeInvalid);
        }

        _idToName.Clear();
        _nameToId.Clear();
        Name = XmlNodes.Attribute(root, "name");

        foreach (var element in root.Elements("String"))
        {
            var id = XmlNodes.AttributeOrNull(element, "value");
            var identifier = XmlNodes.AttributeOrNull(element, "id");
            if (id is null || identifier is null || !XmlNumbers.TryInt(id, out var value))
            {
                continue;
            }

            _idToName[value] = identifier;
            _nameToId[identifier] = value;
        }
    }

    public void LoadFromFileContent(string path) => LoadFrom(File.ReadAllText(path));

    public string? FindName(int id) => _idToName.TryGetValue(id, out var name) ? name : null;

    public int? FindId(string name) => _nameToId.TryGetValue(name, out var id) ? id : null;
}
