using System.Text;
using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Rsb;

internal sealed class CompressStringList
{
    private readonly int _listType;
    private readonly List<CompressString> _list = [];

    public CompressStringList(int type)
    {
        _listType = type;
    }

    public int Length => _list.Count;

    public CompressString this[int index] => _list[index];

    public void Add(CompressString value) => _list.Add(value);

    public byte[] Write(bool bigEndian = false)
    {
        _list.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        var prefixList = new List<(byte[] Prefix, int Location)> { (Array.Empty<byte>(), -1) };
        var finished = new List<byte>(_list.Count * 256);

        foreach (var entry in _list)
        {
            if (entry.Name.Contains('\0'))
            {
                throw new InvalidDataException("RSB string contains a null character");
            }

            var thisRest = Encoding.UTF8.GetBytes(entry.Name).ToList();
            var awaitIndices = new List<int>(8);
            var removeStart = false;

            for (var i = 0; i < prefixList.Count; i++)
            {
                if (removeStart)
                {
                    WriteInt24(finished, prefixList[i].Location, 0);
                    prefixList.RemoveAt(i);
                    i--;
                    continue;
                }

                var prefix = prefixList[i].Prefix;
                var j = 0;
                while (j < prefix.Length && j < thisRest.Count && thisRest[j] == prefix[j])
                {
                    j++;
                }

                if (j == prefix.Length && prefix.Length > 0)
                {
                    awaitIndices.Add(i);
                    thisRest = thisRest.GetRange(j, thisRest.Count - j);
                }
                else if (j > 0)
                {
                    WriteInt24(finished, prefixList[i].Location + j * 4, finished.Count / 4);
                    prefixList[i] = (prefix[..j], prefixList[i].Location);
                    awaitIndices.Add(i);
                    thisRest = thisRest.GetRange(j, thisRest.Count - j);
                    removeStart = true;
                }
                else if (j == 0 && prefix.Length > 0)
                {
                    prefixList.RemoveAt(i);
                    i--;
                    removeStart = true;
                }
            }

            thisRest.Add(0);
            prefixList.Add(([.. thisRest], finished.Count));
            awaitIndices.Add(prefixList.Count - 1);

            var infoLength = _listType == 0 ? 4 : entry.Type == 1 ? 12 : 32;
            var thisFinished = new byte[thisRest.Count * 4 + infoLength];
            for (var i = 0; i < thisRest.Count; i++)
            {
                thisFinished[i * 4] = thisRest[i];
            }

            var info = new BufferWriter(infoLength, ByteOrder.Little);
            if (infoLength == 12)
            {
                info.WriteUInt32(0);
                info.WriteUInt32(entry.Offset);
                info.WriteUInt32(entry.Size);
            }
            else if (infoLength == 32)
            {
                info.WriteUInt32(1);
                info.WriteUInt32(entry.Offset);
                info.WriteUInt32(entry.Size);
                info.WriteUInt32(entry.Index2);
                info.WriteUInt32(unchecked((uint)entry.Empty1));
                info.WriteUInt32(unchecked((uint)entry.Empty2));
                info.WriteUInt32(entry.Width);
                info.WriteUInt32(entry.Height);
            }
            else
            {
                info.WriteUInt32(entry.Index);
            }

            info.ToArray().CopyTo(thisFinished, thisFinished.Length - infoLength);

            finished.AddRange(thisFinished);
            foreach (var index in awaitIndices)
            {
                WriteInt24(finished, prefixList[index].Location, finished.Count / 4);
            }
        }

        foreach (var (prefix, location) in prefixList)
        {
            if (prefix.Length > 0)
            {
                WriteInt24(finished, location, 0);
            }
        }

        var result = finished.ToArray();
        if (bigEndian)
        {
            for (var i = 0; i < result.Length; i += 4)
            {
                result.AsSpan(i, 4).Reverse();
            }
        }

        return result;
    }

    public void Read(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length % 4 != 0)
        {
            throw new InvalidDataException("RSB string table is not word aligned");
        }

        _list.Clear();
        var position = 0;
        var totalUnits = bytes.Length / 4;
        var defaults = new List<(string Name, int Offset)> { ("", totalUnits) };

        while (position < bytes.Length)
        {
            var tempHead = new StringBuilder();
            var temp = new StringBuilder();
            var positionUnit = position / 4;
            for (var i = 0; i < defaults.Count; i++)
            {
                if (positionUnit < defaults[i].Offset)
                {
                    tempHead.Append(defaults[i].Name);
                }
                else
                {
                    defaults.RemoveAt(i);
                    i--;
                }
            }

            var startIndex = 0;
            var tpendOffset = defaults.Count > 0 ? defaults[^1].Offset : totalUnits;
            while (true)
            {
                RsbValidation.Range(bytes.Length, position, 4, "string table character");
                if (bytes[position] == 0)
                {
                    break;
                }
                var ch = (char)bytes[position++];
                temp.Append(ch);
                var tmpEndOffset = ReadInt24(bytes, ref position);
                if (tmpEndOffset != 0)
                {
                    if (temp.Length > 1)
                    {
                        defaults.Add((temp.ToString(startIndex, temp.Length - 1 - startIndex), tpendOffset));
                    }

                    startIndex = temp.Length - 1;
                    tpendOffset = tmpEndOffset;
                }
            }

            position++;
            var finalOffset = ReadInt24(bytes, ref position);
            if (finalOffset != 0 && temp.Length > 0)
            {
                defaults.Add((temp.ToString(startIndex, temp.Length - startIndex), tpendOffset));
            }

            var fullName = Encoding.UTF8.GetString(Encoding.Latin1.GetBytes(tempHead.ToString() + temp));
            if (_listType == 0)
            {
                _list.Add(new CompressString(fullName, 0, ReadUInt32(bytes, ref position)));
            }
            else
            {
                var firstWord = ReadUInt32(bytes, ref position);
                if (firstWord == 0)
                {
                    var offset = ReadUInt32(bytes, ref position);
                    var size = ReadUInt32(bytes, ref position);
                    _list.Add(new CompressString(fullName, 1, offset: offset, size: size));
                }
                else
                {
                    if (firstWord != 1)
                    {
                        throw new InvalidDataException($"Unknown RSGP resource type: {firstWord}");
                    }

                    var type = 2;
                    var offset = ReadUInt32(bytes, ref position);
                    var size = ReadUInt32(bytes, ref position);
                    var index = ReadUInt32(bytes, ref position);
                    var empty1 = unchecked((int)ReadUInt32(bytes, ref position));
                    var empty2 = unchecked((int)ReadUInt32(bytes, ref position));
                    var width = ReadUInt32(bytes, ref position);
                    var height = ReadUInt32(bytes, ref position);
                    _list.Add(new CompressString(fullName, type, 0, offset, size)
                    {
                        Index2 = index,
                        Empty1 = empty1,
                        Empty2 = empty2,
                        Width = width,
                        Height = height,
                    });
                }
            }
        }
    }

    private static void WriteInt24(List<byte> bytes, int location, int value)
    {
        if ((uint)value > 0xFFFFFF)
        {
            throw new InvalidDataException("RSB string table exceeds the 24-bit address space");
        }

        bytes[location + 1] = (byte)(value & 0xFF);
        bytes[location + 2] = (byte)((value >> 8) & 0xFF);
        bytes[location + 3] = (byte)((value >> 16) & 0xFF);
    }



    private static int ReadInt24(ReadOnlySpan<byte> bytes, ref int position)
    {
        RsbValidation.Range(bytes.Length, position, 3, "string table pointer");
        var value = bytes[position] | (bytes[position + 1] << 8) | (bytes[position + 2] << 16);
        if (value != 0 && (value <= position / 4 || value > bytes.Length / 4))
        {
            throw new InvalidDataException("Invalid RSB string table pointer");
        }

        position += 3;
        return value;
    }

    private static uint ReadUInt32(ReadOnlySpan<byte> bytes, ref int position)
    {
        RsbValidation.Range(bytes.Length, position, 4, "string table metadata");
        var value = new BufferReader(bytes.Slice(position, 4).ToArray(), ByteOrder.Little).ReadUInt32();
        position += 4;
        return value;
    }
}
