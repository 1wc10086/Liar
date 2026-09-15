using System.Globalization;
using System.Text;
using System.Text.Json;
using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Rton;

public static class RtonEncoder
{
    public static byte[] Encode(string json, StringEncoding encoding = StringEncoding.Utf8)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return [];
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return [];
            }

            var buffer = new BufferWriter();
            buffer.WriteBytes(RtonConstants.RtonHeader);
            var encoder = new JsonEncoder(buffer, encoding);
            encoder.WriteObject(document.RootElement);
            buffer.WriteBytes(RtonConstants.DoneFooter);
            return buffer.ToArray();
        }
    }

    private sealed class JsonEncoder
    {
        private readonly BufferWriter _buffer;
        private readonly StringEncoding _encoding;
        private readonly Dictionary<string, uint> _nativeIndex = [];
        private readonly Dictionary<string, uint> _unicodeIndex = [];
        private uint _nativeCount;
        private uint _unicodeCount;

        public JsonEncoder(BufferWriter buffer, StringEncoding encoding)
        {
            _buffer = buffer;
            _encoding = encoding;
        }

        public void WriteObject(JsonElement obj)
        {
            foreach (var property in obj.EnumerateObject())
            {
                WriteString(property.Name);
                WriteValue(property.Value);
            }

            _buffer.WriteUInt8(0xFF);
        }

        public void WriteArray(JsonElement array)
        {
            _buffer.WriteUInt8(0xFD);
            _buffer.WriteVarUInt64((ulong)array.GetArrayLength());
            foreach (var item in array.EnumerateArray())
            {
                WriteValue(item);
            }

            _buffer.WriteUInt8(0xFE);
        }

        private void WriteValue(JsonElement value)
        {
            switch (value.ValueKind)
            {
                case JsonValueKind.Object:
                    _buffer.WriteUInt8(0x85);
                    WriteObject(value);
                    break;
                case JsonValueKind.Array:
                    _buffer.WriteUInt8(0x86);
                    WriteArray(value);
                    break;
                case JsonValueKind.String:
                    WriteString(value.GetString() ?? "");
                    break;
                case JsonValueKind.Number:
                    WriteNumber(value);
                    break;
                case JsonValueKind.True:
                    _buffer.WriteUInt8(0x01);
                    break;
                case JsonValueKind.False:
                    _buffer.WriteUInt8(0x00);
                    break;
                case JsonValueKind.Null:
                    _buffer.WriteUInt8(0x84);
                    break;
            }
        }

        private void WriteString(string value)
        {
            if (value.Length > 0)
            {
                switch (value[0])
                {
                    case '*':
                        if (value.Length == 1)
                        {
                            _buffer.WriteUInt8(0x02);
                            return;
                        }
                        break;
                    case 'R':
                        if (WriteRtid(value))
                        {
                            return;
                        }
                        break;
                    case '$':
                        if (WriteBinary(value))
                        {
                            return;
                        }
                        break;
                }
            }

            if (!TextCodec.IsAscii(value))
            {
                if (_encoding == StringEncoding.Eascii)
                {
                    WriteEasciiString(value);
                }
                else
                {
                    WriteUnicodeString(value);
                }
                return;
            }

            WriteAsciiString(value);
        }

        private void WriteAsciiString(string value)
        {
            if (_nativeIndex.TryGetValue(value, out var foundId))
            {
                _buffer.WriteUInt8(0x91);
                _buffer.WriteVarUInt32(foundId);
                return;
            }

            _nativeIndex[value] = _nativeCount;
            _buffer.WriteUInt8(0x90);
            _buffer.WriteVarUInt64((ulong)value.Length);
            _buffer.WriteBytes(Encoding.UTF8.GetBytes(value));
            _nativeCount++;
        }

        private void WriteUnicodeString(string value)
        {
            if (_unicodeIndex.TryGetValue(value, out var foundId))
            {
                _buffer.WriteUInt8(0x93);
                _buffer.WriteVarUInt32(foundId);
                return;
            }

            var bytes = Encoding.UTF8.GetBytes(value);
            _unicodeIndex[value] = _unicodeCount;
            _buffer.WriteUInt8(0x92);
            _buffer.WriteVarInt32(TextCodec.Utf16Length(value));
            _buffer.WriteVarUInt64((ulong)bytes.Length);
            _buffer.WriteBytes(bytes);
            _unicodeCount++;
        }

        private void WriteEasciiString(string value)
        {
            var bytes = TextCodec.Utf8ToLatin1(Encoding.UTF8.GetBytes(value));
            var key = Encoding.Latin1.GetString(bytes);
            if (_nativeIndex.TryGetValue(key, out var foundId))
            {
                _buffer.WriteUInt8(0x91);
                _buffer.WriteVarUInt32(foundId);
                return;
            }

            _nativeIndex[key] = _nativeCount;
            _buffer.WriteUInt8(0x90);
            _buffer.WriteVarUInt64((ulong)bytes.Length);
            _buffer.WriteBytes(bytes);
            _nativeCount++;
        }

        private void WriteNumber(JsonElement value)
        {
            if (value.TryGetUInt64(out var unsigned))
            {
                if (unsigned <= int.MaxValue)
                {
                    if (unsigned == 0)
                    {
                        _buffer.WriteUInt8(0x21);
                    }
                    else
                    {
                        _buffer.WriteUInt8(0x24);
                        _buffer.WriteVarUInt32((uint)unsigned);
                    }
                }
                else if (unsigned <= long.MaxValue)
                {
                    _buffer.WriteUInt8(0x44);
                    _buffer.WriteVarUInt64(unsigned);
                }
                else
                {
                    _buffer.WriteUInt8(0x46);
                    _buffer.WriteUInt64(unsigned);
                }
                return;
            }

            if (value.TryGetInt64(out var signed))
            {
                if (signed == 0)
                {
                    _buffer.WriteUInt8(0x21);
                }
                else if (signed > 0)
                {
                    if (signed <= int.MaxValue)
                    {
                        _buffer.WriteUInt8(0x24);
                        _buffer.WriteVarUInt32((uint)signed);
                    }
                    else
                    {
                        _buffer.WriteUInt8(0x44);
                        _buffer.WriteVarUInt64((ulong)signed);
                    }
                }
                else if (signed + 0x40000000L >= 0)
                {
                    _buffer.WriteUInt8(0x25);
                    _buffer.WriteZigZag32((int)signed);
                }
                else
                {
                    _buffer.WriteUInt8(0x45);
                    _buffer.WriteZigZag64(signed);
                }
                return;
            }

            var real = value.GetDouble();
            if (real == 0.0)
            {
                _buffer.WriteUInt8(0x23);
                return;
            }

            var single = (float)real;
            if ((double)single == real)
            {
                _buffer.WriteUInt8(0x22);
                _buffer.WriteSingle(single);
            }
            else
            {
                _buffer.WriteUInt8(0x42);
                _buffer.WriteDouble(real);
            }
        }

        private bool WriteBinary(string value)
        {
            if (value.Length < 13 || !value.StartsWith("$BINARY(\"", StringComparison.Ordinal) ||
                !value.EndsWith(')'))
            {
                return false;
            }

            var separator = value.LastIndexOf("\", ", StringComparison.Ordinal);
            if (separator < 9 || separator + 3 > value.Length - 1)
            {
                return false;
            }

            var binary = value[9..separator];
            var number = value[(separator + 3)..(value.Length - 1)];
            if (int.TryParse(number, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            {
                var bytes = Encoding.UTF8.GetBytes(binary);
                _buffer.WriteUInt8(0x87);
                _buffer.WriteUInt8(0x00);
                _buffer.WriteVarUInt64((ulong)bytes.Length);
                _buffer.WriteBytes(bytes);
                _buffer.WriteVarUInt32((uint)id);
                return true;
            }

            return false;
        }

        private bool WriteRtid(string value)
        {
            if (value.Length < 7 || !value.StartsWith("RTID(", StringComparison.Ordinal) ||
                !value.EndsWith(')'))
            {
                return false;
            }

            if (value == "RTID(0)")
            {
                _buffer.WriteUInt8(0x84);
                return true;
            }

            var content = value[5..^1];
            var at = content.IndexOf('@');
            if (at < 0)
            {
                _buffer.WriteUInt8(0x84);
                return true;
            }

            var id = content[..at];
            var name = content[(at + 1)..];
            var dot1 = id.IndexOf('.');
            var dot2 = dot1 >= 0 ? id.IndexOf('.', dot1 + 1) : -1;

            if (dot1 >= 0 && dot2 >= 0 &&
                int.TryParse(id[..dot1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var first) &&
                int.TryParse(id[(dot1 + 1)..dot2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var middle) &&
                uint.TryParse(id[(dot2 + 1)..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var last))
            {
                var nameBytes = Encoding.UTF8.GetBytes(name);
                _buffer.WriteUInt8(0x83);
                _buffer.WriteUInt8(0x02);
                _buffer.WriteVarUInt64((ulong)TextCodec.Utf16Length(name));
                _buffer.WriteVarUInt64((ulong)nameBytes.Length);
                _buffer.WriteBytes(nameBytes);
                _buffer.WriteVarUInt32((uint)middle);
                _buffer.WriteVarUInt32((uint)first);
                _buffer.WriteUInt32(last);
                return true;
            }

            var aliasBytes = Encoding.UTF8.GetBytes(id);
            _buffer.WriteUInt8(0x83);
            _buffer.WriteUInt8(0x03);
            _buffer.WriteVarUInt64((ulong)TextCodec.Utf16Length(name));
            _buffer.WriteVarUInt64((ulong)Encoding.UTF8.GetByteCount(name));
            _buffer.WriteBytes(Encoding.UTF8.GetBytes(name));
            _buffer.WriteVarUInt64((ulong)TextCodec.Utf16Length(id));
            _buffer.WriteVarUInt64((ulong)aliasBytes.Length);
            _buffer.WriteBytes(aliasBytes);
            return true;
        }

    }
}
