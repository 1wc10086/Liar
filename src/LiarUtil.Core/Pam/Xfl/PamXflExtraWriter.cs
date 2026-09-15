namespace LiarUtil.Core.Pam.Xfl;

internal static class PamXflExtraWriter
{
    public static PamXflExtra Create(PamAnimation animation) => new()
    {
        Version = animation.Version,
        FrameRate = PamXflConstants.DefaultFrameRate,
        Position = [animation.PositionX, animation.PositionY],
        Image = [.. animation.Images.Select(image => new PamXflExtraImage
        {
            Name = image.Name,
            Size = image.Width is null && image.Height is null ? null : new[] { image.Width ?? -1, image.Height ?? -1 },
        })],
        Sprite = [.. animation.Sprites.Select(CreateSprite)],
        MainSprite = animation.MainSprite is { } main ? CreateSprite(main) : null,
    };

    private static PamXflExtraSprite CreateSprite(PamSprite sprite) => new()
    {
        Name = sprite.Name,
        Description = sprite.Description,
        FrameRate = 0,
        WorkArea = null,
    };
}
