using System.Xml.Linq;

namespace LiarUtil.Core.Reanim.Flash;

internal sealed class XflLibraryItem
{
    public XflLibraryItem(XElement node)
    {
        Node = node;
        Name = XflElement.Attr(node, "name");
        ItemType = node.Name.LocalName switch
        {
            "DOMSymbolItem" => XflElement.Attr(node, "symbolType", "movie clip"),
            "DOMBitmapItem" => "bitmap",
            "DOMSoundItem" => "sound",
            "DOMFolderItem" => "folder",
            _ => node.Name.LocalName,
        };
        var timeline = XflElement.Child(XflElement.Child(node, "timeline"), "DOMTimeline");
        Timeline = timeline is null ? null : new XflTimeline(timeline);
    }

    public XElement Node { get; }

    public string Name { get; }

    public string ItemType { get; }

    public XflTimeline? Timeline { get; }
}
