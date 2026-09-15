using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Rsb;

internal sealed class RsbGroupInfo
{
    public const int MaxSubgroups = 0x40;

    public string Identifier = "";
    public RsbSubgroupReference[] Subgroups = Create();
    public uint SubgroupCount;

    public static RsbGroupInfo Read(BufferReader reader, RsbVersion version)
    {
        var result = new RsbGroupInfo { Identifier = RsbText.ReadFixed(reader, RsbConstants.GroupIdentifierSize) };
        for (var i = 0; i < MaxSubgroups; i++)
        {
            var reference = result.Subgroups[i];
            reference.Index = reader.ReadUInt32();
            reference.Resolution = reader.ReadUInt32();
            if (version.HasSubgroupLocale())
            {
                reference.Locale = RsbText.ReadFourCharacterCode(reader);
                _ = reader.ReadUInt32();
            }
        }

        result.SubgroupCount = reader.ReadUInt32();
        if (result.SubgroupCount > MaxSubgroups)
        {
            throw new InvalidDataException("RSB group references more than 64 subgroups");
        }

        return result;
    }

    public void Write(BufferWriter writer, RsbVersion version)
    {
        if (SubgroupCount > MaxSubgroups)
        {
            throw new InvalidDataException("RSB group references more than 64 subgroups");
        }

        RsbText.WriteFixed(writer, Identifier, RsbConstants.GroupIdentifierSize);
        for (var i = 0; i < MaxSubgroups; i++)
        {
            var reference = Subgroups[i];
            writer.WriteUInt32(reference.Index);
            writer.WriteUInt32(reference.Resolution);
            if (version.HasSubgroupLocale())
            {
                RsbText.WriteFourCharacterCode(writer, reference.Locale);
                writer.WriteUInt32(0);
            }
        }

        writer.WriteUInt32(SubgroupCount);
    }

    private static RsbSubgroupReference[] Create()
    {
        var result = new RsbSubgroupReference[MaxSubgroups];
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = new RsbSubgroupReference();
        }

        return result;
    }
}

internal sealed class RsbSubgroupReference
{
    public uint Index;
    public uint Resolution;
    public string Locale = "";
}
