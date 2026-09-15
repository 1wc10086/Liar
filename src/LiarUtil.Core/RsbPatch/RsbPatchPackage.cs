namespace LiarUtil.Core.RsbPatch;

internal sealed class RsbPatchPackage
{
    public uint AllAfterSize;

    public byte[] BeforeHash = new byte[RsbPatchFormat.HashSize];

    public uint PacketCount;

    public uint PatchExist;

    public uint PatchSize;

    public static RsbPatchPackage Read(RsbPatchReader reader)
    {
        var package = new RsbPatchPackage();
        reader.Skip(sizeof(uint));
        package.AllAfterSize = reader.ReadUInt32();
        reader.Skip(sizeof(uint));
        package.PatchSize = reader.ReadUInt32();
        package.BeforeHash = reader.ReadBytes(RsbPatchFormat.HashSize);
        package.PacketCount = reader.ReadUInt32();
        package.PatchExist = reader.ReadUInt32();
        return package;
    }

    public byte[] Encode()
    {
        var writer = new RsbPatchWriter(RsbPatchFormat.PackageInformationSize);
        writer.WriteUInt32(2);
        writer.WriteUInt32(AllAfterSize);
        writer.WriteUInt32(0);
        writer.WriteUInt32(PatchSize);
        writer.WriteBytes(BeforeHash);
        writer.WriteUInt32(PacketCount);
        writer.WriteUInt32(PatchExist);
        return writer.ToArray();
    }
}

internal sealed class RsbPatchPacket
{
    public uint PatchExist;

    public uint PatchSize;

    public string Name = "";

    public byte[] BeforeHash = new byte[RsbPatchFormat.HashSize];

    public static RsbPatchPacket Read(RsbPatchReader reader)
    {
        var packet = new RsbPatchPacket
        {
            PatchExist = reader.ReadUInt32(),
            PatchSize = reader.ReadUInt32(),
            Name = reader.ReadFixedName(RsbPatchFormat.PacketNameSize),
            BeforeHash = reader.ReadBytes(RsbPatchFormat.HashSize),
        };
        return packet;
    }

    public byte[] Encode()
    {
        var writer = new RsbPatchWriter(RsbPatchFormat.PacketInformationSize);
        writer.WriteUInt32(PatchExist);
        writer.WriteUInt32(PatchSize);
        writer.WriteName(Name, RsbPatchFormat.PacketNameSize);
        writer.WriteBytes(BeforeHash);
        return writer.ToArray();
    }
}
