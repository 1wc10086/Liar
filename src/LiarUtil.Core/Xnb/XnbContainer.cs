using System.Text;
using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Xnb;

internal static class XnbContainer
{
    private const int HeaderLength = 10;
    private const int MaxTypeReaderCount = 1024;

    public static XnbContent Read(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var reader = new BufferReader(data) { ErrorFactory = XnbErrors.Throw };
        if (reader.Length < HeaderLength
            || reader.ReadUInt8() != (byte)'X'
            || reader.ReadUInt8() != (byte)'N'
            || reader.ReadUInt8() != (byte)'B')
        {
            throw new XnbException(LiarUtil.Core.Strings.NotValidXNBFile);
        }

        var platform = (char)reader.ReadUInt8();
        var version = reader.ReadUInt8();
        var flags = (XnbFlags)reader.ReadUInt8();
        var fileSize = reader.ReadUInt32();
        if (version is not (4 or 5))
        {
            throw new XnbException(string.Format(LiarUtil.Core.Strings.UnsupportedXNBVersion0, version));
        }

        if (flags.HasFlag(XnbFlags.Compressed))
        {
            throw new XnbException(LiarUtil.Core.Strings.CompressedXNBFilesAreNotSupportedYet);
        }

        var readerCount = reader.ReadVarUInt32();
        if (readerCount is 0 or > MaxTypeReaderCount)
        {
            throw new XnbException(string.Format(LiarUtil.Core.Strings.XNBTypeReaderCountInvalid0, readerCount));
        }

        var typeReaders = new XnbTypeReader[readerCount];
        for (var index = 0; index < typeReaders.Length; index++)
        {
            var nameLength = reader.ReadVarUInt32();
            var name = Encoding.UTF8.GetString(reader.ReadSpan((int)nameLength));
            typeReaders[index] = new XnbTypeReader(name, reader.ReadInt32());
        }

        var sharedCount = reader.ReadVarUInt32();
        for (var index = 0u; index < sharedCount; index++)
        {
            reader.Skip(reader.ReadVarUInt32());
        }

        var primaryIndex = reader.ReadVarUInt32();
        if (primaryIndex is 0 || primaryIndex > typeReaders.Length)
        {
            throw new XnbException(string.Format(LiarUtil.Core.Strings.XNBPrimaryObjectTypeIndexInvalid0, primaryIndex));
        }

        var offset = reader.Position;
        return new XnbContent
        {
            Header = new XnbHeader
            {
                Platform = platform,
                Version = version,
                Flags = flags,
                FileSize = fileSize,
            },
            TypeReaders = typeReaders,
            PrimaryReader = typeReaders[primaryIndex - 1],
            Data = data,
            Offset = offset,
            Length = data.Length - offset,
        };
    }
}
