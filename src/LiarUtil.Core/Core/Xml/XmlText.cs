namespace LiarUtil.Core.Core.Xml;

public static class XmlText
{
    public static string? EmptyToNull(string? value) => string.IsNullOrEmpty(value) ? null : value;

    public static string Value(string? value) => value ?? "";

    public static string Trimmed(string? value) => value?.Trim() ?? "";
}
