namespace LiarUtil.Core.Pam;

public sealed class PamLayerRemove
{
    public int Index { get; set; }
}

public sealed class PamLayerAppend
{
    public int Index { get; set; }

    public int Resource { get; set; }

    public bool Sprite { get; set; }

    public bool Additive { get; set; }

    public int PreloadFrame { get; set; }

    public string? Name { get; set; }

    public float TimeScale { get; set; } = 1f;
}

public sealed class PamLayerChange
{
    public int Index { get; set; }

    public PamTransform Transform { get; set; } = new PamTranslateTransform();

    public PamColor? Color { get; set; }

    public int? SpriteFrameNumber { get; set; }

    public PamRectangle? SourceRectangle { get; set; }
}

public sealed class PamColor
{
    public float Red { get; set; }

    public float Green { get; set; }

    public float Blue { get; set; }

    public float Alpha { get; set; }
}

public sealed class PamRectangle
{
    public float X { get; set; }

    public float Y { get; set; }

    public float Width { get; set; }

    public float Height { get; set; }
}
