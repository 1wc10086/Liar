using System.Text;

namespace LiarUtil.Core.RsbPatch;

internal sealed class RsbBundleHeader
{
    public uint InformationSectionSize;
    public uint ResourcePathSectionSize;
    public uint ResourcePathSectionOffset;
    public uint SubgroupIdSize;
    public uint SubgroupIdOffset;
    public uint SubgroupCount;
    public uint SubgroupInformationOffset;
    public uint SubgroupInformationBlockSize;
    public uint GroupCount;
    public uint GroupInformationOffset;
    public uint GroupInformationBlockSize;
    public uint GroupIdSize;
    public uint GroupIdOffset;
    public uint PoolCount;
    public uint PoolInformationOffset;
    public uint PoolInformationBlockSize;
    public uint TextureCount;
    public uint TextureInformationOffset;
    public uint TextureInformationBlockSize;
}

internal sealed class RsbSubgroupRecord
{
    public string Identifier = "";
    public uint Offset;
    public uint Size;
    public uint Pool;
    public uint Compression;
    public uint InformationSectionSize;
    public uint GeneralResourceDataSectionOffset;
    public uint GeneralResourceDataSectionSize;
    public uint GeneralResourceDataSectionSizeOriginal;
    public uint GeneralResourceDataSectionSizePool;
    public uint TextureResourceDataSectionOffset;
    public uint TextureResourceDataSectionSize;
    public uint TextureResourceDataSectionSizeOriginal;
    public uint TextureResourceDataSectionSizePool;
    public uint TextureResourceCount;
    public uint TextureResourceBegin;
}

internal sealed class RsbBundleLayout
{
    public RsbBundleHeader Header = new();

    public List<RsbSubgroupRecord> Subgroups = [];

    public RsbSubgroupRecord? Find(string identifier)
    {
        foreach (var record in Subgroups)
        {
            if (string.Equals(record.Identifier, identifier, StringComparison.Ordinal))
            {
                return record;
            }
        }

        return null;
    }
}

internal static class RsbBundleFormat
{
    public static RsbBundleLayout Read(byte[] data)
    {
        var reader = new RsbPatchReader(data);
        if (reader.ReadUInt32() != RsbPatchFormat.BundleMagic)
        {
            throw new RsbPatchException(LiarUtil.Core.Strings.NotValidRSBFile);
        }

        RsbPatchFormat.EnsureBundleVersion((int)reader.ReadUInt32());
        reader.Skip(sizeof(uint));

        var header = new RsbBundleHeader
        {
            InformationSectionSize = reader.ReadUInt32(),
            ResourcePathSectionSize = reader.ReadUInt32(),
            ResourcePathSectionOffset = reader.ReadUInt32(),
        };
        reader.Skip(2 * sizeof(uint));
        header.SubgroupIdSize = reader.ReadUInt32();
        header.SubgroupIdOffset = reader.ReadUInt32();
        header.SubgroupCount = reader.ReadUInt32();
        header.SubgroupInformationOffset = reader.ReadUInt32();
        header.SubgroupInformationBlockSize = reader.ReadUInt32();
        header.GroupCount = reader.ReadUInt32();
        header.GroupInformationOffset = reader.ReadUInt32();
        header.GroupInformationBlockSize = reader.ReadUInt32();
        header.GroupIdSize = reader.ReadUInt32();
        header.GroupIdOffset = reader.ReadUInt32();
        header.PoolCount = reader.ReadUInt32();
        header.PoolInformationOffset = reader.ReadUInt32();
        header.PoolInformationBlockSize = reader.ReadUInt32();
        header.TextureCount = reader.ReadUInt32();
        header.TextureInformationOffset = reader.ReadUInt32();
        header.TextureInformationBlockSize = reader.ReadUInt32();
        reader.Skip(4 * sizeof(uint));

        if (header.SubgroupInformationBlockSize != RsbPatchFormat.SubgroupRecordSize)
        {
            throw new RsbPatchException(string.Format(LiarUtil.Core.Strings.UnsupportedRSBPacketRecordSize0, header.SubgroupInformationBlockSize));
        }

        reader.Position = checked((int)header.SubgroupInformationOffset);
        var layout = new RsbBundleLayout { Header = header };
        layout.Subgroups.Capacity = checked((int)header.SubgroupCount);
        for (var index = 0u; index < header.SubgroupCount; index++)
        {
            layout.Subgroups.Add(ReadSubgroup(reader));
        }

        return layout;
    }

    public static void WriteSubgroups(byte[] data, RsbBundleLayout layout)
    {
        for (var index = 0; index < layout.Subgroups.Count; index++)
        {
            var position = checked((int)(layout.Header.SubgroupInformationOffset + (uint)index * RsbPatchFormat.SubgroupRecordSize));
            EncodeSubgroup(layout.Subgroups[index]).CopyTo(data, position);
        }
    }

    private static RsbSubgroupRecord ReadSubgroup(RsbPatchReader reader)
    {
        var record = new RsbSubgroupRecord
        {
            Identifier = reader.ReadFixedName(RsbPatchFormat.IdentifierSize),
            Offset = reader.ReadUInt32(),
            Size = reader.ReadUInt32(),
            Pool = reader.ReadUInt32(),
            Compression = reader.ReadUInt32(),
            InformationSectionSize = reader.ReadUInt32(),
            GeneralResourceDataSectionOffset = reader.ReadUInt32(),
            GeneralResourceDataSectionSize = reader.ReadUInt32(),
            GeneralResourceDataSectionSizeOriginal = reader.ReadUInt32(),
            GeneralResourceDataSectionSizePool = reader.ReadUInt32(),
            TextureResourceDataSectionOffset = reader.ReadUInt32(),
            TextureResourceDataSectionSize = reader.ReadUInt32(),
            TextureResourceDataSectionSizeOriginal = reader.ReadUInt32(),
            TextureResourceDataSectionSizePool = reader.ReadUInt32(),
        };
        reader.Skip(4 * sizeof(uint));
        record.TextureResourceCount = reader.ReadUInt32();
        record.TextureResourceBegin = reader.ReadUInt32();
        return record;
    }

    private static byte[] EncodeSubgroup(RsbSubgroupRecord record)
    {
        var writer = new RsbPatchWriter(RsbPatchFormat.SubgroupRecordSize);
        WriteIdentifier(writer, record.Identifier);
        writer.WriteUInt32(record.Offset);
        writer.WriteUInt32(record.Size);
        writer.WriteUInt32(record.Pool);
        writer.WriteUInt32(record.Compression);
        writer.WriteUInt32(record.InformationSectionSize);
        writer.WriteUInt32(record.GeneralResourceDataSectionOffset);
        writer.WriteUInt32(record.GeneralResourceDataSectionSize);
        writer.WriteUInt32(record.GeneralResourceDataSectionSizeOriginal);
        writer.WriteUInt32(record.GeneralResourceDataSectionSizePool);
        writer.WriteUInt32(record.TextureResourceDataSectionOffset);
        writer.WriteUInt32(record.TextureResourceDataSectionSize);
        writer.WriteUInt32(record.TextureResourceDataSectionSizeOriginal);
        writer.WriteUInt32(record.TextureResourceDataSectionSizePool);
        writer.WriteZeros(4 * sizeof(uint));
        writer.WriteUInt32(record.TextureResourceCount);
        writer.WriteUInt32(record.TextureResourceBegin);
        return writer.ToArray();
    }

    private static void WriteIdentifier(RsbPatchWriter writer, string value)
    {
        var bytes = Encoding.Latin1.GetBytes(value);
        if (bytes.Length >= RsbPatchFormat.IdentifierSize)
        {
            throw new RsbPatchException(string.Format(LiarUtil.Core.Strings.RSBIdentifierLengthExceeds0Bytes1, RsbPatchFormat.IdentifierSize - 1, value));
        }

        writer.WriteBytes(bytes);
        writer.WriteZeros(RsbPatchFormat.IdentifierSize - bytes.Length);
    }
}
