namespace LiarUtil.Core.RsbPatch;

internal sealed record RsbPatchEncodeOptions
{
    public required byte[] Before { get; init; }

    public required byte[] After { get; init; }

    public bool UseRawPacket { get; init; }
}

internal sealed record RsbPatchDecodeOptions
{
    public required byte[] Before { get; init; }

    public required byte[] Patch { get; init; }

    public bool UseRawPacket { get; init; }
}

internal static class RsbPatchProcessor
{
    public static byte[] Encode(RsbPatchEncodeOptions options)
    {
        var before = options.Before;
        var after = options.After;
        var beforeLayout = RsbBundleFormat.Read(before);
        var afterLayout = RsbBundleFormat.Read(after);

        var informationBefore = before.AsSpan(0, (int)beforeLayout.Header.InformationSectionSize);
        var informationAfter = after.AsSpan(0, (int)afterLayout.Header.InformationSectionSize);

        var writer = new RsbPatchWriter(4096);
        writer.WriteUInt32(RsbPatchFormat.PatchMagic);
        writer.WriteUInt32(RsbPatchFormat.PatchVersion);
        var packagePosition = writer.Reserve(RsbPatchFormat.PackageInformationSize);

        var informationPatch = informationBefore.SequenceEqual(informationAfter)
            ? Array.Empty<byte>()
            : VcdiffEncoder.Encode(informationBefore.ToArray(), informationAfter.ToArray(), interleaved: true);

        writer.WriteBytes(informationPatch);

        var afterEnd = afterLayout.Header.InformationSectionSize;
        foreach (var afterRecord in afterLayout.Subgroups)
        {
            var beforeRecord = beforeLayout.Find(afterRecord.Identifier);
            var beforePacket = beforeRecord is null ? Array.Empty<byte>() : Slice(before, beforeRecord.Offset, beforeRecord.Size);
            if (options.UseRawPacket && beforePacket.Length > 0)
            {
                beforePacket = RsbPacketFormat.Uncompress(beforePacket);
            }

            var afterPacket = Slice(after, afterRecord.Offset, afterRecord.Size);
            if (options.UseRawPacket && afterPacket.Length > 0)
            {
                afterPacket = RsbPacketFormat.Uncompress(afterPacket);
            }

            afterEnd = Math.Max(afterEnd, afterRecord.Offset + afterRecord.Size);

            var patch = afterPacket.AsSpan().SequenceEqual(beforePacket)
                ? Array.Empty<byte>()
                : VcdiffEncoder.Encode(beforePacket, afterPacket, interleaved: true);

            writer.WriteBytes(new RsbPatchPacket
            {
                PatchExist = patch.Length > 0 ? 1u : 0u,
                PatchSize = (uint)patch.Length,
                Name = afterRecord.Identifier,
                BeforeHash = RsbPatchCompression.Hash(beforePacket),
            }.Encode());
            writer.WriteBytes(patch);
        }

        writer.Overwrite(packagePosition, new RsbPatchPackage
        {
            AllAfterSize = afterEnd,
            BeforeHash = RsbPatchCompression.Hash(informationBefore),
            PacketCount = (uint)afterLayout.Subgroups.Count,
            PatchExist = informationPatch.Length > 0 ? 1u : 0u,
            PatchSize = (uint)informationPatch.Length,
        }.Encode());
        return writer.ToArray();
    }

    public static byte[] Decode(RsbPatchDecodeOptions options)
    {
        var before = options.Before;
        var reader = new RsbPatchReader(options.Patch);
        if (reader.ReadUInt32() != RsbPatchFormat.PatchMagic)
        {
            throw new RsbPatchException(LiarUtil.Core.Strings.NotValidRsbPatchFile);
        }

        RsbPatchFormat.EnsurePatchVersion((int)reader.ReadUInt32());
        var package = RsbPatchPackage.Read(reader);

        var beforeLayout = RsbBundleFormat.Read(before);
        var informationBefore = before.AsSpan(0, (int)beforeLayout.Header.InformationSectionSize);
        if (!RsbPatchCompression.Hash(informationBefore).AsSpan().SequenceEqual(package.BeforeHash))
        {
            throw new RsbPatchException(LiarUtil.Core.Strings.OriginalFileDoesNotMatchPatch);
        }

        var informationAfter = package.PatchExist != 0
            ? VcdiffDeltaDecoder.Decode(informationBefore.ToArray(), reader.ReadBytes((int)package.PatchSize))
            : informationBefore.ToArray();

        var afterLayout = RsbBundleFormat.Read(informationAfter);
        if (afterLayout.Header.InformationSectionSize > informationAfter.Length)
        {
            throw new RsbPatchException(LiarUtil.Core.Strings.InfoRegionLengthRestoredFromPatchInconsistent);
        }

        if (package.PacketCount != afterLayout.Subgroups.Count)
        {
            throw new RsbPatchException(LiarUtil.Core.Strings.PatchPacketCountInconsistentWithRSB);
        }

        var writer = new RsbPatchWriter(informationAfter.Length + 1024);
        writer.WriteBytes(informationAfter);

        for (var index = 0; index < afterLayout.Subgroups.Count; index++)
        {
            var afterRecord = afterLayout.Subgroups[index];
            var packet = RsbPatchPacket.Read(reader);
            if (!string.Equals(packet.Name, afterRecord.Identifier, StringComparison.Ordinal))
            {
                throw new RsbPatchException(string.Format(LiarUtil.Core.Strings.PatchPacketNameMismatch0, packet.Name));
            }

            var beforeRecord = beforeLayout.Find(packet.Name);
            var beforePacket = beforeRecord is null ? Array.Empty<byte>() : Slice(before, beforeRecord.Offset, beforeRecord.Size);
            if (options.UseRawPacket && beforePacket.Length > 0)
            {
                beforePacket = RsbPacketFormat.Uncompress(beforePacket);
            }

            if (!RsbPatchCompression.Hash(beforePacket).AsSpan().SequenceEqual(packet.BeforeHash))
            {
                throw new RsbPatchException(string.Format(LiarUtil.Core.Strings.OriginalPacketDoesNotMatchPatch0, packet.Name));
            }

            var afterPacket = packet.PatchExist != 0
                ? VcdiffDeltaDecoder.Decode(beforePacket, reader.ReadBytes((int)packet.PatchSize))
                : beforePacket;

            if (!options.UseRawPacket)
            {
                writer.WriteBytes(afterPacket);
                continue;
            }

            var compressed = RsbPacketFormat.Compress(afterPacket);
            var header = RsbPacketFormat.ReadHeader(compressed);
            afterRecord.Offset = (uint)writer.Length;
            afterRecord.Size = (uint)Math.Max(
                header.InformationSectionSize,
                Math.Max(
                    header.GeneralResourceDataSectionOffset + header.GeneralResourceDataSectionSize,
                    header.TextureResourceDataSectionOffset + header.TextureResourceDataSectionSize));
            afterRecord.GeneralResourceDataSectionOffset = header.GeneralResourceDataSectionOffset;
            afterRecord.GeneralResourceDataSectionSize = header.GeneralResourceDataSectionSize;
            afterRecord.TextureResourceDataSectionOffset = header.TextureResourceDataSectionOffset;
            afterRecord.TextureResourceDataSectionSize = header.TextureResourceDataSectionSize;
            writer.WriteBytes(compressed);
        }

        var result = writer.ToArray();
        if (options.UseRawPacket)
        {
            RsbBundleFormat.WriteSubgroups(result, afterLayout);
        }

        return result;
    }

    private static byte[] Slice(byte[] data, uint offset, uint size)
    {
        if (offset > data.Length || size > data.Length - offset)
        {
            throw new RsbPatchException(string.Format(LiarUtil.Core.Strings.RSBPacketOutOfRange012, offset, size, data.Length));
        }

        return data.AsSpan((int)offset, (int)size).ToArray();
    }
}
