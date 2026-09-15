namespace LiarUtil.Core.Pam;

public sealed class PamAnimation
{
    public int Version { get; set; } = 6;

    public int FrameRate { get; set; }

    public float PositionX { get; set; }

    public float PositionY { get; set; }

    public float Width { get; set; }

    public float Height { get; set; }

    public List<PamImage> Images { get; set; } = [];

    public List<PamSprite> Sprites { get; set; } = [];

    public PamSprite? MainSprite { get; set; }
}

public sealed class PamImage
{
    public string Name { get; set; } = "";

    public int? Width { get; set; }

    public int? Height { get; set; }

    public PamTransform Transform { get; set; } = new PamMatrixTransform();
}

public sealed class PamSprite
{
    public string? Name { get; set; }

    public string? Description { get; set; }

    public float? FrameRate { get; set; }

    public int WorkAreaStart { get; set; }

    public int WorkAreaDuration { get; set; }

    public List<PamFrame> Frames { get; set; } = [];
}

public sealed class PamFrame
{
    public string? Label { get; set; }

    public bool Stop { get; set; }

    public List<PamCommand> Commands { get; set; } = [];

    public List<PamLayerRemove> Removes { get; set; } = [];

    public List<PamLayerAppend> Appends { get; set; } = [];

    public List<PamLayerChange> Changes { get; set; } = [];
}

public sealed class PamCommand
{
    public string Command { get; set; } = "";

    public string Argument { get; set; } = "";
}
