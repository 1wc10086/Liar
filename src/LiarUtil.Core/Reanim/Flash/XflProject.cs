using System.IO.Compression;
using System.Xml.Linq;
using LiarUtil.Core.Core.Xml;

namespace LiarUtil.Core.Reanim.Flash;

internal sealed class XflProject
{
    private readonly string? _root;
    private readonly Dictionary<string, string>? _archiveXml;
    private readonly Dictionary<string, XflLibraryItem> _libraryItems = new(StringComparer.Ordinal);
    private readonly List<string> _includes = [];
    private readonly HashSet<string> _loadedIncludes = new(StringComparer.OrdinalIgnoreCase);

    private XflProject(string? root, Dictionary<string, string>? archiveXml, XDocument document)
    {
        _root = root;
        _archiveXml = archiveXml;
        var documentRoot = document.Root ?? throw new InvalidDataException(LiarUtil.Core.Strings.DOMDocumentXmlContentEmpty);
        FrameRate = XmlNumbers.ParseDouble(XflElement.Attr(documentRoot, "frameRate"), 0);
        MainTimeline = new XflTimeline(XflElement.Children(XflElement.Child(documentRoot, "timelines"), "DOMTimeline").FirstOrDefault());

        foreach (var media in XflElement.Child(documentRoot, "media")?.Elements() ?? Enumerable.Empty<XElement>())
        {
            AddLibraryItem(new XflLibraryItem(media));
        }
        foreach (var include in XflElement.Children(XflElement.Child(documentRoot, "symbols"), "Include"))
        {
            var href = XflElement.Attr(include, "href");
            if (!string.IsNullOrEmpty(href))
            {
                _includes.Add(href);
            }
        }
    }

    public double FrameRate { get; }

    public XflTimeline MainTimeline { get; }

    public static XflProject Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException(LiarUtil.Core.Strings.XFLPathMustSpecified, nameof(path));
        }
        var source = Path.GetFullPath(path);
        if (IsZipXfl(source))
        {
            return LoadArchive(source);
        }

        string? root = Directory.Exists(source) ? source : Path.GetDirectoryName(source);
        while (!string.IsNullOrEmpty(root) && !File.Exists(Path.Combine(root, "DOMDocument.xml")))
        {
            var parent = Directory.GetParent(root);
            if (parent is null || string.Equals(parent.FullName, root, StringComparison.OrdinalIgnoreCase))
            {
                root = null;
                break;
            }
            root = parent.FullName;
        }
        if (string.IsNullOrEmpty(root))
        {
            throw new FileNotFoundException(LiarUtil.Core.Strings.DOMDocumentXmlOfXFLProjectNotFound, path);
        }
        return new XflProject(root, null, LoadDocument(Path.Combine(root, "DOMDocument.xml")));
    }

    public static XflProject LoadArchive(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException(LiarUtil.Core.Strings.ZIPXFLPathMustSpecified, nameof(path));
        }
        var source = Path.GetFullPath(path);
        if (!File.Exists(source))
        {
            throw new FileNotFoundException(LiarUtil.Core.Strings.ZIPXFLArchiveNotFound, path);
        }
        var archive = ReadArchiveXml(source);
        if (!archive.TryGetValue("domdocument.xml", out var xml))
        {
            throw new InvalidDataException(LiarUtil.Core.Strings.DOMDocumentXmlMissingFromZIPXFLArchive);
        }
        return new XflProject(Path.GetDirectoryName(source), archive, ParseDocument(xml));
    }

    public static bool IsZipXfl(string path)
    {
        if (!File.Exists(path))
        {
            return false;
        }
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, false);
            return archive.Entries.Any(entry => !string.IsNullOrEmpty(entry.Name)
                && string.Equals(entry.Name, "DOMDocument.xml", StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception exception) when (exception is InvalidDataException or IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    public XflLibraryItem? FindLibraryItem(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }
        if (_libraryItems.TryGetValue(name, out var item))
        {
            return item;
        }
        foreach (var href in _includes)
        {
            if (_loadedIncludes.Add(href))
            {
                var document = LoadXml("LIBRARY/" + href);
                if (document?.Root is not null)
                {
                    AddLibraryItem(new XflLibraryItem(document.Root));
                }
            }
            if (_libraryItems.TryGetValue(name, out item))
            {
                return item;
            }
        }
        return null;
    }

    private void AddLibraryItem(XflLibraryItem item)
    {
        if (!string.IsNullOrEmpty(item.Name))
        {
            _libraryItems[item.Name] = item;
        }
    }

    private XDocument? LoadXml(string relativePath)
    {
        if (_archiveXml is not null)
        {
            return _archiveXml.TryGetValue(ArchiveKey(relativePath), out var xml) ? ParseDocument(xml) : null;
        }
        if (_root is null)
        {
            return null;
        }
        var file = Path.Combine(_root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(file) ? LoadDocument(file) : null;
    }

    private static Dictionary<string, string> ReadArchiveXml(string path)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, false);
        var documentEntry = archive.Entries
            .Where(entry => !string.IsNullOrEmpty(entry.Name) && string.Equals(entry.Name, "DOMDocument.xml", StringComparison.OrdinalIgnoreCase))
            .OrderBy(entry => entry.FullName.Length)
            .FirstOrDefault();
        if (documentEntry is null)
        {
            return result;
        }
        var normalizedDocument = documentEntry.FullName.Replace('\\', '/');
        var slash = normalizedDocument.LastIndexOf('/');
        var prefix = slash < 0 ? "" : normalizedDocument[..(slash + 1)];
        foreach (var entry in archive.Entries)
        {
            var fullName = entry.FullName.Replace('\\', '/').TrimStart('/');
            if (string.IsNullOrEmpty(entry.Name)
                || !fullName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                || !fullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            var relative = fullName[prefix.Length..];
            using var reader = new StreamReader(entry.Open(), detectEncodingFromByteOrderMarks: true);
            result[ArchiveKey(relative)] = reader.ReadToEnd();
        }
        return result;
    }

    private static XDocument LoadDocument(string file)
    {
        using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
        return XDocument.Load(stream, LoadOptions.None);
    }

    private static XDocument ParseDocument(string xml) => XDocument.Parse(xml, LoadOptions.None);

    private static string ArchiveKey(string path)
    {
        var key = path.Replace('\\', '/');
        while (key.StartsWith("./", StringComparison.Ordinal))
        {
            key = key[2..];
        }
        return key.TrimStart('/').ToLowerInvariant();
    }
}
