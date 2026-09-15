namespace LiarUtil.Core.PopFx.Models;

public sealed class PopFxPass
{
    public string Name { get; set; } = "";
    public PopFxShader? VertexShader { get; set; }
    public PopFxShader? PixelShader { get; set; }
    public List<PopFxAnnotation> VertexAnnotations { get; set; } = [];
    public List<PopFxAnnotation> PixelAnnotations { get; set; } = [];
    public List<PopFxSetting> Settings { get; set; } = [];
}
