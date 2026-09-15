namespace LiarUtil.Core.RsbPatch;

internal static class VcdiffEncoder
{
    private const int BlockSize = 16;

    private const int MinimumMatchSize = 32;

    public static byte[] Encode(byte[] before, byte[] after, bool interleaved)
    {
        if (after.Length == 0)
        {
            return VcdiffFormat.DeltaHeader(interleaved);
        }

        var writer = new VcdiffWriter(before.Length, interleaved);
        if (after.Length < BlockSize)
        {
            writer.Add(after);
            return Assemble(writer.Finish(), interleaved);
        }

        var dictionaryHash = new VcdiffBlockHash(before, 0, BlockSize, populate: true);
        var targetHash = new VcdiffBlockHash(after, before.Length, BlockSize, populate: false);
        var hasher = new VcdiffRollingHash(BlockSize);

        var targetEnd = after.Length;
        var startOfLastBlock = targetEnd - BlockSize;
        var nextEncode = 0;
        var candidate = 0;
        var hash = VcdiffRollingHash.Compute(after, 0, BlockSize);

        while (true)
        {
            var match = new VcdiffMatch();
            dictionaryHash.FindBestMatch(hash, candidate, nextEncode, targetEnd - nextEncode, after, ref match);
            targetHash.FindBestMatch(hash, candidate, nextEncode, targetEnd - nextEncode, after, ref match);

            if (match.Size >= MinimumMatchSize)
            {
                if (match.TargetOffset > 0)
                {
                    writer.Add(after.AsSpan(nextEncode, match.TargetOffset));
                }

                writer.Copy(match.SourceOffset, match.Size);
                nextEncode += match.TargetOffset + match.Size;
                candidate = nextEncode;
                if (candidate > startOfLastBlock)
                {
                    break;
                }

                hash = VcdiffRollingHash.Compute(after, candidate, BlockSize);
                targetHash.AddAllBlocksThrough(nextEncode);
                continue;
            }

            if (candidate + 1 > startOfLastBlock)
            {
                break;
            }

            targetHash.AddOneIndexHash(candidate, hash);
            hash = hasher.Update(hash, after[candidate], after[candidate + BlockSize]);
            candidate++;
        }

        if (nextEncode < targetEnd)
        {
            writer.Add(after.AsSpan(nextEncode));
        }

        return Assemble(writer.Finish(), interleaved);
    }

    private static byte[] Assemble(byte[] body, bool interleaved)
    {
        var header = VcdiffFormat.DeltaHeader(interleaved);
        var result = new byte[header.Length + body.Length];
        header.CopyTo(result);
        body.CopyTo(result, header.Length);
        return result;
    }
}
