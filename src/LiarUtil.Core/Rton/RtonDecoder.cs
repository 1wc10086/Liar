using System.Globalization;
using System.Text;
using LiarUtil.Core.Core.Binary;
using LiarUtil.Core.Rton.Json;

namespace LiarUtil.Core.Rton;

public static class RtonDecoder
{
    public static byte[] Decode(ReadOnlySpan<byte> bytes, StringEncoding encoding = StringEncoding.Utf8)
    {
        if (bytes.Length < 8 || !bytes[..4].SequenceEqual("RTON"u8))
        {
            return [];
        }

        var reader = new BufferReader(bytes[8..]) { Lenient = true };
        var state = new DecodeState(reader, encoding);
        var root = new JsonObject();
        state.ReadObject(root);
        return reader.HasOverflow ? [] : JsonWriter.WritePretty(root);
    }

    private sealed class DecodeState(BufferReader reader, StringEncoding encoding)
    {
        private readonly List<byte[]> _nativeIndex = [];
        private readonly List<byte[]> _unicodeIndex = [];

        public void ReadObject(JsonObject obj)
        {
            while (!reader.AtEnd && reader.PeekUInt8() != 0xFF)
            {
                var key = ReadValue();
                if (reader.AtEnd || reader.PeekUInt8() == 0xFF)
                {
                    break;
                }

                var value = ReadValue();
                if (key is JsonString keyString)
                {
                    obj.Entries.Add((keyString.Bytes, value));
                }

                if (reader.HasOverflow)
                {
                    break;
                }
            }

            if (!reader.AtEnd && reader.PeekUInt8() == 0xFF)
            {
                reader.Skip(1);
            }
        }

        public void ReadArray(JsonArray array)
        {
            if (reader.AtEnd || reader.PeekUInt8() != 0xFD)
            {
                return;
            }

            reader.Skip(1);
            var count = reader.ReadVarUInt64();
            for (ulong index = 0; index < count; index++)
            {
                if (reader.AtEnd)
                {
                    break;
                }

                array.Items.Add(ReadValue());
                if (reader.HasOverflow)
                {
                    break;
                }
            }

            if (!reader.AtEnd && reader.PeekUInt8() == 0xFE)
            {
                reader.Skip(1);
            }
        }

        private JsonValue ReadValue()
        {
            if (reader.AtEnd)
            {
                return new JsonNull();
            }

            return reader.ReadUInt8() switch
            {
                0x00 => new JsonBool(false),
                0x01 => new JsonBool(true),
                0x02 => new JsonString("*"u8),
                0x85 => ReadObjectValue(),
                0x86 => ReadArrayValue(),
                0x08 => new JsonNumber((sbyte)reader.ReadUInt8()),
                0x09 => new JsonNumber(0L),
                0x0A => new JsonNumber((long)reader.ReadUInt8()),
                0x0B => new JsonNumber(0L),
                0x10 => new JsonNumber(reader.ReadInt16()),
                0x11 => new JsonNumber(0L),
                0x12 => new JsonNumber((ulong)reader.ReadUInt16()),
                0x13 => new JsonNumber(0L),
                0x20 => new JsonNumber(reader.ReadInt32()),
                0x21 => new JsonNumber(0L),
                0x22 => new JsonNumber((double)reader.ReadSingle()),
                0x23 => new JsonNumber(0.0),
                0x24 => new JsonNumber((long)(int)reader.ReadVarUInt32()),
                0x28 => new JsonNumber(reader.ReadVarUInt32()),
                0x25 => new JsonNumber(reader.ReadZigZag32()),
                0x26 => new JsonNumber((ulong)reader.ReadUInt32()),
                0x27 => new JsonNumber(0UL),
                0x40 => new JsonNumber(reader.ReadInt64()),
                0x41 => new JsonNumber(0L),
                0x42 => new JsonNumber(reader.ReadDouble()),
                0x43 => new JsonNumber(0.0),
                0x44 => new JsonNumber((long)reader.ReadVarUInt64()),
                0x48 => new JsonNumber(reader.ReadVarUInt64()),
                0x45 => new JsonNumber(reader.ReadZigZag64()),
                0x46 => new JsonNumber(reader.ReadUInt64()),
                0x47 => new JsonNumber(0UL),
                0x81 => ReadRawString(),
                0x82 => ReadPlainString(),
                0x90 => ReadNativeString(),
                0x91 => ReadIndexedString(_nativeIndex),
                0x92 => ReadUnicodeString(),
                0x93 => ReadIndexedString(_unicodeIndex),
                0x83 => ReadRtid(),
                0x84 => new JsonString("RTID(0)"u8),
                0x87 => ReadBinary(),
                0xBC => new JsonBool(reader.ReadUInt8() != 0),
                _ => new JsonString("UNKNOWN_TYPE"u8),
            };
        }

        private JsonValue ReadObjectValue()
        {
            var obj = new JsonObject();
            ReadObject(obj);
            return obj;
        }

        private JsonValue ReadArrayValue()
        {
            var array = new JsonArray();
            ReadArray(array);
            return array;
        }

        private JsonValue ReadRawString()
        {
            var length = reader.ReadVarUInt64();
            if (reader.RequireLength(length))
            {
                return new JsonString([]);
            }

            if (length == 0)
            {
                return new JsonString([]);
            }

            var span = reader.ReadSpan((int)length);
            return new JsonString(DecodeStringBytes(span));
        }

        private JsonValue ReadPlainString()
        {
            _ = reader.ReadVarUInt64();
            var length = reader.ReadVarUInt64();
            if (reader.RequireLength(length))
            {
                return new JsonString([]);
            }

            return length == 0
                ? new JsonString([])
                : new JsonString(reader.ReadSpan((int)length).ToArray());
        }

        private JsonValue ReadNativeString()
        {
            var length = reader.ReadVarUInt64();
            if (reader.RequireLength(length))
            {
                _nativeIndex.Add([]);
                return new JsonString([]);
            }

            if (length == 0)
            {
                _nativeIndex.Add([]);
                return new JsonString([]);
            }

            var decoded = DecodeStringBytes(reader.ReadSpan((int)length));
            _nativeIndex.Add(decoded);
            return new JsonString(decoded);
        }

        private JsonValue ReadUnicodeString()
        {
            _ = reader.ReadVarUInt64();
            var length = reader.ReadVarUInt64();
            if (reader.RequireLength(length))
            {
                _unicodeIndex.Add([]);
                return new JsonString([]);
            }

            if (length == 0)
            {
                _unicodeIndex.Add([]);
                return new JsonString([]);
            }

            var bytes = reader.ReadSpan((int)length).ToArray();
            _unicodeIndex.Add(bytes);
            return new JsonString(bytes);
        }

        private JsonValue ReadIndexedString(List<byte[]> index)
        {
            var id = reader.ReadVarUInt64();
            return id < (ulong)index.Count ? new JsonString(index[(int)id]) : new JsonString([]);
        }

        private JsonValue ReadRtid() => reader.ReadUInt8() switch
        {
            0x01 => ReadRtidWithName(),
            0x02 => ReadRtidWithSheet(),
            0x03 => ReadRtidWithAlias(),
            _ => new JsonString("RTID(0)"u8),
        };

        private JsonValue ReadRtidWithName()
        {
            var middle = reader.ReadVarUInt64();
            var first = reader.ReadVarUInt64();
            var last = reader.ReadUInt32();
            var text = FormattableString.Invariant($"RTID({first}.{middle}.{last:x8}@");
            return new JsonString(Encoding.ASCII.GetBytes(text));
        }

        private JsonValue ReadRtidWithSheet()
        {
            _ = reader.ReadVarUInt64();
            var sheetLength = reader.ReadVarUInt64();
            if (reader.RequireLength(sheetLength))
            {
                return new JsonString("RTID(0)"u8);
            }

            var sheet = reader.ReadSpan((int)sheetLength);
            var middle = reader.ReadVarUInt64();
            var first = reader.ReadVarUInt64();
            var last = reader.ReadUInt32();

            var buffer = new List<byte>((int)sheetLength + 24);
            buffer.AddRange(Encoding.ASCII.GetBytes(
                FormattableString.Invariant($"RTID({first}.{middle}.{last:x8}@")));
            buffer.AddRange(sheet);
            buffer.Add((byte)')');
            return new JsonString([.. buffer]);
        }

        private JsonValue ReadRtidWithAlias()
        {
            _ = reader.ReadVarUInt64();
            var sheetLength = reader.ReadVarUInt64();
            if (reader.RequireLength(sheetLength))
            {
                return new JsonString("RTID(0)"u8);
            }

            var sheet = reader.ReadSpan((int)sheetLength).ToArray();

            _ = reader.ReadVarUInt64();
            var aliasLength = reader.ReadVarUInt64();
            if (reader.RequireLength(aliasLength))
            {
                return new JsonString("RTID(0)"u8);
            }

            var alias = reader.ReadSpan((int)aliasLength);

            var buffer = new List<byte>((int)aliasLength + (int)sheetLength + 8);
            buffer.AddRange(Encoding.ASCII.GetBytes("RTID("));
            buffer.AddRange(alias);
            buffer.Add((byte)'@');
            buffer.AddRange(sheet);
            buffer.Add((byte)')');
            return new JsonString([.. buffer]);
        }

        private JsonValue ReadBinary()
        {
            if (reader.RequireLength(1))
            {
                return new JsonString([]);
            }

            reader.Skip(1);
            var length = reader.ReadVarUInt64();
            if (reader.RequireLength(length))
            {
                return new JsonString([]);
            }

            var bytes = reader.ReadSpan((int)length);
            var id = reader.ReadVarUInt64();

            var buffer = new List<byte>((int)length + 24);
            buffer.AddRange(Encoding.ASCII.GetBytes("$BINARY(\""));
            buffer.AddRange(bytes);
            buffer.AddRange(Encoding.ASCII.GetBytes("\", "));
            buffer.AddRange(Encoding.ASCII.GetBytes(id.ToString(CultureInfo.InvariantCulture)));
            buffer.Add((byte)')');
            return new JsonString([.. buffer]);
        }

        private byte[] DecodeStringBytes(ReadOnlySpan<byte> bytes) =>
            encoding != StringEncoding.Eascii || !HasHighByte(bytes)
                ? bytes.ToArray()
                : TextCodec.Latin1ToUtf8(bytes);

        private static bool HasHighByte(ReadOnlySpan<byte> bytes)
        {
            foreach (var value in bytes)
            {
                if (value >= 0x80)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
