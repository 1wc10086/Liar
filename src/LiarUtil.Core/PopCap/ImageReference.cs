using System.Text.Json.Serialization;

namespace LiarUtil.Core.PopCap;

[JsonConverter(typeof(ImageReferenceJsonConverter))]
public sealed class ImageReference
{
    public string? Name { get; set; }

    public int? Id { get; set; }

    public static ImageReference? FromName(string? name) =>
        string.IsNullOrEmpty(name) ? null : new ImageReference { Name = name };

    public static ImageReference? FromId(int id) => id < 0 ? null : new ImageReference { Id = id };

    public static ImageReference? FromValue(string? name, int? id) =>
        !string.IsNullOrEmpty(name) ? new ImageReference { Name = name } : FromId(id ?? -1);

    public static ImageReference? FromId(int id, ImageIdMap? images)
    {
        if (id < 0)
        {
            return null;
        }

        return images?.FindName(id) is { } name ? new ImageReference { Name = name } : FromId(id);
    }

    public static int ResolveId(ImageReference? image, ImageIdMap? images = null) =>
        ImageNameResolver.ResolveId(image?.Name, image?.Id, images);

    public static string? ResolveName(ImageReference? image, ImageIdMap? images = null) =>
        ImageNameResolver.ResolveName(image?.Name, image?.Id, images);

    public override string ToString() => ResolveName(this) ?? "";
}
