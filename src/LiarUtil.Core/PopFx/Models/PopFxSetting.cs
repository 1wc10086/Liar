namespace LiarUtil.Core.PopFx.Models;

public sealed class PopFxSetting
{
    public uint Category { get; set; }
    public uint Type { get; set; }
    public List<PopFxAnnotation> Annotations { get; set; } = [];
    public List<PopFxValue> Values { get; set; } = [];
}
