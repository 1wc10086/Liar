using LiarUtil.Core.Core.Binary;
using LiarUtil.Core.Core.IO;

namespace LiarUtil.Core.Xpr;

internal sealed record XprUnpackOptions
{
    public required string InputPath { get; init; }

    public required string OutputFolder { get; init; }
}

internal static class XprUnpack
{
    public static XprPackInfo Unpack(XprUnpackOptions options)
    {
        var data = File.ReadAllBytes(options.InputPath);
        if (data.Length < XprFormat.HeaderSize)
        {
            throw new XprException(LiarUtil.Core.Strings.XPRFileLengthInsufficient);
        }

        var header = new BufferReader(data, ByteOrder.Big);
        var magic = header.ReadUInt32();
        if (magic != XprFormat.Signature)
        {
            throw new XprException(string.Format(LiarUtil.Core.Strings.XPRFileMarkerMismatchExpectedXPR2Got0x, magic));
        }

        var dataSize = header.ReadUInt32();
        if (dataSize < 8 || dataSize > (uint)(data.Length - XprFormat.HeaderSize))
        {
            throw new XprException(LiarUtil.Core.Strings.XPRDataRegionLengthInvalid);
        }

        var dataOffset = checked((uint)(data.Length - dataSize));
        var region = data.AsSpan(checked((int)dataOffset), checked((int)dataSize));
        var fileCount = new BufferReader(region, ByteOrder.Big).ReadUInt32();
        if (fileCount > ((long)dataSize - 8) / XprFormat.EntrySize)
        {
            throw new XprException(LiarUtil.Core.Strings.XPRIndexCountInvalid);
        }

        var poolOffset = checked(8u + (fileCount * XprFormat.EntrySize));
        var pool = region[checked((int)poolOffset)..];
        var resourceFolder = Path.Combine(options.OutputFolder, XprFormat.ResourceFolderName);
        Directory.CreateDirectory(resourceFolder);

        var records = new List<XprRecordFile>(checked((int)fileCount));
        var fileAligned = true;
        for (var i = 0u; i < fileCount; i++)
        {
            var entry = XprFileEntry.Read(region.Slice(checked((int)(4 + (i * XprFormat.EntrySize))), XprFormat.EntrySize));
            if (entry.FileOffset > dataSize || entry.FileSize > dataSize - entry.FileOffset)
            {
                throw new XprException(string.Format(LiarUtil.Core.Strings.XPREntry0BeyondDataRegion, i));
            }

            if (entry.PathOffset < poolOffset || entry.PathOffset >= dataSize)
            {
                throw new XprException(string.Format(LiarUtil.Core.Strings.XPRNameOffsetOutOfRange0, i));
            }

            var recordPath = XprText.ReadName(pool, checked((int)(entry.PathOffset - poolOffset)));
            var target = Path.Combine(resourceFolder, XprText.ToNativePath(recordPath));
            FileIO.WriteAllBytes(target, region.Slice(checked((int)entry.FileOffset), checked((int)entry.FileSize)));
            if ((entry.FileOffset & 0xFu) != 0u)
            {
                fileAligned = false;
            }

            records.Add(new XprRecordFile { Type = XprText.TypeToString(entry.Type), Path = recordPath });
        }

        var info = new XprPackInfo
        {
            XprDataOffset = dataOffset,
            XprDataFileAlign = fileAligned,
            RecordFiles = records,
        };
        XprDefinitionStore.Save(options.OutputFolder, info);
        return info;
    }
}
