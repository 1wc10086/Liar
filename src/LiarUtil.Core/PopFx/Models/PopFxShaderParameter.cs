namespace LiarUtil.Core.PopFx.Models;

public sealed class PopFxShaderParameter
{
    public string Name { get; set; } = "";
    public PopFxValue Register { get; set; } = new();
}
