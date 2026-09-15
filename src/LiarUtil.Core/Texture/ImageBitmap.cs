using SkiaSharp;

namespace LiarUtil.Core.Texture;

public sealed class ImageBitmap : IDisposable
{
    private byte[] _pixels;
    private bool _disposed;

    public ImageBitmap(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        Width = width;
        Height = height;
        _pixels = new byte[checked(width * height * 4)];
    }

    public int Width { get; }

    public int Height { get; }

    public int Size => Width * Height;

    public Span<byte> Pixels
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _pixels;
        }
    }

    public ReadOnlyMemory<byte> PixelMemory
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _pixels;
        }
    }

    public void Dispose()
    {
        _disposed = true;
        _pixels = [];
    }

    public static ImageBitmap Load(string path)
    {
        using var codec = SKCodec.Create(path) ?? throw new InvalidDataException("Image codec is unavailable");
        var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var rgba = new SKBitmap(info);
        var result = codec.GetPixels(info, rgba.GetPixels());
        if (result != SKCodecResult.Success)
        {
            throw new InvalidDataException($"Image decode failed: {result}");
        }

        var bitmap = new ImageBitmap(info.Width, info.Height);
        rgba.GetPixelSpan().CopyTo(bitmap._pixels);
        return bitmap;
    }

    public void Save(string path)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var info = new SKImageInfo(Width, Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var bitmap = new SKBitmap(info);
        _pixels.CopyTo(bitmap.GetPixelSpan());
        using var image = SKImage.FromBitmap(bitmap) ?? throw new InvalidDataException("Image allocation failed");
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100) ?? throw new InvalidDataException("PNG encode failed");
        File.WriteAllBytes(path, encoded.ToArray());
    }
}
