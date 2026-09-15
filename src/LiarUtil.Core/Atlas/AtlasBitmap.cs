using LiarUtil.Core.Core.IO;
using SkiaSharp;

namespace LiarUtil.Core.Atlas;

internal sealed class AtlasBitmap : IDisposable
{
    private byte[] _pixels;

    public AtlasBitmap(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        Width = width;
        Height = height;
        _pixels = new byte[checked(width * height * 4)];
    }

    public int Width { get; }

    public int Height { get; }

    public Span<byte> Pixels => _pixels;

    public void Dispose() => _pixels = [];

    public static AtlasBitmap Load(string path)
    {
        using var codec = SKCodec.Create(path) ?? throw new AtlasException(string.Format(LiarUtil.Core.Strings.CannotCreateImageDecoder0, path));
        var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var bitmap = new SKBitmap(info);
        if (codec.GetPixels(info, bitmap.GetPixels()) != SKCodecResult.Success)
        {
            throw new AtlasException(string.Format(LiarUtil.Core.Strings.ImageDecodingFailed0, path));
        }

        var image = new AtlasBitmap(info.Width, info.Height);
        bitmap.GetPixelSpan().CopyTo(image.Pixels);
        return image;
    }

    public void Save(string path)
    {
        var info = new SKImageInfo(Width, Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var bitmap = new SKBitmap(info);
        _pixels.CopyTo(bitmap.GetPixelSpan());
        using var pixmap = bitmap.PeekPixels() ?? throw new AtlasException(LiarUtil.Core.Strings.ImageReadingFailed);
        using var data = pixmap.Encode(new SKPngEncoderOptions(SKPngEncoderFilterFlags.NoFilters, 1))
            ?? throw new AtlasException(LiarUtil.Core.Strings.PNGEncodingFailed);
        FileIO.WriteAllBytes(path, data.ToArray());
    }

    public void Blit(AtlasBitmap source, int x, int y)
    {
        var firstColumn = Math.Max(0, -x);
        var lastColumn = Math.Min(source.Width, Width - x);
        var firstRow = Math.Max(0, -y);
        var lastRow = Math.Min(source.Height, Height - y);
        var copyWidth = lastColumn - firstColumn;
        for (var row = firstRow; row < lastRow; row++)
        {
            source.Pixels.Slice(((row * source.Width) + firstColumn) * 4, copyWidth * 4)
                .CopyTo(Pixels.Slice((((y + row) * Width) + x + firstColumn) * 4, copyWidth * 4));
        }
    }

    public AtlasBitmap Cut(int x, int y, int width, int height)
    {
        var result = new AtlasBitmap(width, height);
        for (var row = 0; row < height; row++)
        {
            var sourceY = y + row;
            if (sourceY < 0 || sourceY >= Height)
            {
                continue;
            }

            for (var column = 0; column < width; column++)
            {
                var sourceX = x + column;
                if (sourceX < 0 || sourceX >= Width)
                {
                    continue;
                }

                Pixels.Slice(((sourceY * Width) + sourceX) * 4, 4)
                    .CopyTo(result.Pixels.Slice(((row * width) + column) * 4, 4));
            }
        }

        return result;
    }

    public AtlasBitmap Rotate270()
    {
        var result = new AtlasBitmap(Height, Width);
        var stride = result.Width;
        for (var row = 0; row < Height; row++)
        {
            for (var column = 0; column < Width; column++)
            {
                Pixels.Slice(((row * Width) + column) * 4, 4)
                    .CopyTo(result.Pixels.Slice((((Width - 1 - column) * stride) + row) * 4, 4));
            }
        }

        return result;
    }
}
