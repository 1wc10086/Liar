using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Rsb;

internal sealed class RsgpPacket
{
    public RsgpHeadInfo Head = new();
    public CompressStringList FileList = new(1);
    public byte[] FileListData = [];

    public static RsgpPacket Read(BufferReader reader)
    {
        var result = new RsgpPacket();
        var origin = reader.Position;
        result.Head.Read(reader);
        if (result.Head.Magic != RsbConstants.RsgpMagic || result.Head.Flags > 3 || result.Head.FileListLength % 4 != 0)
        {
            throw new InvalidDataException("Invalid RSGP header");
        }

        var listBegin = checked(origin + (int)result.Head.FileListBegin);
        RsbValidation.Range(reader.Length, listBegin, result.Head.FileListLength, "RSGP string table");
        var length = (int)result.Head.FileListLength;
        if (length == 0)
        {
            return result;
        }

        reader.Position = listBegin;
        result.FileListData = reader.ReadBytes(length);
        reader.Position = listBegin;
        var canonical = new BufferWriter(length);
        for (var i = 0; i < length; i += 4)
        {
            canonical.WriteUInt32(reader.ReadUInt32());
        }

        result.FileList.Read(canonical.ToArray());
        return result;
    }

    public static RsgpPacket Read(byte[] data, bool bigEndian) => Read(new BufferReader(data, bigEndian));

    public int Count => FileList.Length;

    public CompressString this[int index] => FileList[index];
}
