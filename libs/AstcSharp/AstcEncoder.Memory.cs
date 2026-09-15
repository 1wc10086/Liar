using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using AstcSharp.Core;

namespace AstcSharp;

public static partial class AstcEncoder
{
    public static byte[] CompressImage(ReadOnlyMemory<byte> pixels, int width, int height, Footprint footprint, AstcEncoderOptions? options = null) =>
        CompressMemory(pixels, width, height, footprint, false, options);

    public static byte[] CompressHdrImage(ReadOnlyMemory<byte> pixels, int width, int height, Footprint footprint, AstcEncoderOptions? options = null) =>
        CompressMemory(pixels, width, height, footprint, true, options);

    private static byte[] CompressMemory(ReadOnlyMemory<byte> pixels, int width, int height, Footprint footprint, bool hdr, AstcEncoderOptions? options)
    {
        var bytesPerPixel = hdr ? 8 : 4;
        ValidateStreamEncodeArgs(width, height, footprint, bytesPerPixel);
        if (pixels.Length != checked(width * height * bytesPerPixel))
        {
            throw new ArgumentException("Pixel buffer length does not match the image dimensions", nameof(pixels));
        }

        options ??= new AstcEncoderOptions();
        if (options.MaxDegreeOfParallelism is 0 or < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(options.MaxDegreeOfParallelism));
        }

        options.CancellationToken.ThrowIfCancellationRequested();
        var blocksWide = footprint.BlocksWide(width);
        var blockCount = checked(blocksWide * footprint.BlocksHigh(height));
        var output = new byte[checked(blockCount * 16)];
        var threads = options.MaxDegreeOfParallelism == -1 ? Environment.ProcessorCount : options.MaxDegreeOfParallelism;
        void EncodeRange(int start, int end)
        {
            if (hdr)
            {
                EncodeHdrRange(pixels.Span, output, width, height, footprint, blocksWide, start, end, options.CancellationToken);
            }
            else
            {
                EncodeLdrRange(pixels.Span, output, width, height, footprint, blocksWide, start, end, options.CancellationToken);
            }
        }

        if (threads == 1 || blockCount < 32)
        {
            EncodeRange(0, blockCount);
            return output;
        }

        var chunkSize = Math.Max(16, blockCount / Math.Max(1L, (long)threads * 8));
        try
        {
            Parallel.ForEach(Partitioner.Create(0, blockCount, checked((int)chunkSize)),
                new ParallelOptions { MaxDegreeOfParallelism = threads, CancellationToken = options.CancellationToken },
                range => EncodeRange(range.Item1, range.Item2));
        }
        catch (AggregateException ex) when (ex.InnerExceptions.Count == 1)
        {
            ExceptionDispatchInfo.Throw(ex.InnerException!);
        }

        return output;
    }

    private static void EncodeLdrRange(ReadOnlySpan<byte> pixels, Span<byte> output, int width, int height, Footprint footprint, int blocksWide, int start, int end, CancellationToken cancellationToken)
    {
        Span<RgbaColor> texels = stackalloc RgbaColor[footprint.PixelCount];
        Span<RgbaColor> previous = stackalloc RgbaColor[footprint.PixelCount];
        UInt128 last = 0;
        for (var blockIndex = start; blockIndex < end; blockIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var x = blockIndex % blocksWide * footprint.Width;
            var y = blockIndex / blocksWide * footprint.Height;
            for (var row = 0; row < footprint.Height; row++)
            {
                var src = pixels.Slice(checked((Math.Min(y + row, height - 1) * width + x) * 4));
                var valid = Math.Min(footprint.Width, width - x);
                var target = texels.Slice(row * footprint.Width, footprint.Width);
                MemoryMarshal.Cast<byte, RgbaColor>(src[..(valid * 4)]).CopyTo(target);
                target[valid..].Fill(target[valid - 1]);
            }

            if (blockIndex == start || !texels.SequenceEqual(previous))
            {
                last = EncodeTexels(texels, footprint);
                texels.CopyTo(previous);
            }

            BinaryPrimitives.WriteUInt128LittleEndian(output.Slice(blockIndex * 16, 16), last);
        }
    }

    private static void EncodeHdrRange(ReadOnlySpan<byte> pixels, Span<byte> output, int width, int height, Footprint footprint, int blocksWide, int start, int end, CancellationToken cancellationToken)
    {
        Span<RgbaHdrColor> texels = stackalloc RgbaHdrColor[footprint.PixelCount];
        Span<RgbaHdrColor> previous = stackalloc RgbaHdrColor[footprint.PixelCount];
        UInt128 last = 0;
        for (var blockIndex = start; blockIndex < end; blockIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var baseX = blockIndex % blocksWide * footprint.Width;
            var baseY = blockIndex / blocksWide * footprint.Height;
            for (var y = 0; y < footprint.Height; y++)
            {
                for (var x = 0; x < footprint.Width; x++)
                {
                    var offset = checked((Math.Min(baseY + y, height - 1) * width + Math.Min(baseX + x, width - 1)) * 8);
                    var pixel = pixels.Slice(offset, 8);
                    texels[y * footprint.Width + x] = new RgbaHdrColor(
                        BinaryPrimitives.ReadUInt16LittleEndian(pixel), BinaryPrimitives.ReadUInt16LittleEndian(pixel[2..]),
                        BinaryPrimitives.ReadUInt16LittleEndian(pixel[4..]), BinaryPrimitives.ReadUInt16LittleEndian(pixel[6..]));
                }
            }

            if (blockIndex == start || !texels.SequenceEqual(previous))
            {
                last = EncodeHdrTexels(texels, footprint);
                texels.CopyTo(previous);
            }

            BinaryPrimitives.WriteUInt128LittleEndian(output.Slice(blockIndex * 16, 16), last);
        }
    }
}
