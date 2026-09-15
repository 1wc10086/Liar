namespace LiarUtil.Core.Bnk;

internal sealed partial class BankContext
{
    public void Id(ref long value)
    {
        if (_reader is not null)
        {
            value = _reader.ReadUInt32();
        }
        else
        {
            _writer!.WriteUInt32((uint)value);
        }
    }

    public void U8(ref long value)
    {
        if (_reader is not null)
        {
            value = _reader.ReadUInt8();
        }
        else
        {
            _writer!.WriteUInt8((byte)value);
        }
    }

    public void U16(ref long value)
    {
        if (_reader is not null)
        {
            value = _reader.ReadUInt16();
        }
        else
        {
            _writer!.WriteUInt16((ushort)value);
        }
    }

    public void U32(ref long value)
    {
        if (_reader is not null)
        {
            value = _reader.ReadUInt32();
        }
        else
        {
            _writer!.WriteUInt32((uint)value);
        }
    }

    public void S16(ref long value)
    {
        if (_reader is not null)
        {
            value = (short)_reader.ReadUInt16();
        }
        else
        {
            _writer!.WriteUInt16((ushort)(short)value);
        }
    }

    public void S32(ref long value)
    {
        if (_reader is not null)
        {
            value = _reader.ReadInt32();
        }
        else
        {
            _writer!.WriteInt32((int)value);
        }
    }

    public void F32(ref double value)
    {
        if (_reader is not null)
        {
            value = _reader.ReadSingle();
        }
        else
        {
            _writer!.WriteSingle((float)value);
        }
    }

    public void F64(ref double value)
    {
        if (_reader is not null)
        {
            value = _reader.ReadDouble();
        }
        else
        {
            _writer!.WriteDouble(value);
        }
    }

    public void Sz8(ref ulong value)
    {
        if (_reader is not null)
        {
            value = _reader.ReadUInt8();
        }
        else
        {
            _writer!.WriteUInt8((byte)value);
        }
    }

    public void Sz16(ref ulong value)
    {
        if (_reader is not null)
        {
            value = _reader.ReadUInt16();
        }
        else
        {
            _writer!.WriteUInt16((ushort)value);
        }
    }

    public void Sz32(ref ulong value)
    {
        if (_reader is not null)
        {
            value = _reader.ReadUInt32();
        }
        else
        {
            _writer!.WriteUInt32((uint)value);
        }
    }

    public void En8(ref byte value)
    {
        if (_reader is not null)
        {
            value = _reader.ReadUInt8();
        }
        else
        {
            _writer!.WriteUInt8(value);
        }
    }

    public void En32(ref byte value)
    {
        if (_reader is not null)
        {
            value = (byte)_reader.ReadUInt32();
        }
        else
        {
            _writer!.WriteUInt32(value);
        }
    }

    public void B8(ref bool value)
    {
        if (_reader is not null)
        {
            value = _reader.ReadUInt8() != 0;
        }
        else
        {
            _writer!.WriteUInt8(value ? (byte)1 : (byte)0);
        }
    }

    public void Const8(byte value)
    {
        if (_reader is not null)
        {
            _reader.ExpectConstUInt8(value);
        }
        else
        {
            _writer!.WriteUInt8(value);
        }
    }

    public void Const16(ushort value)
    {
        if (_reader is not null)
        {
            _reader.ExpectConstUInt16(value);
        }
        else
        {
            _writer!.WriteUInt16(value);
        }
    }

    public void Const32(uint value)
    {
        if (_reader is not null)
        {
            _reader.ExpectConstUInt32(value);
        }
        else
        {
            _writer!.WriteUInt32(value);
        }
    }

    public ulong ReadSize(SizeKind kind) => kind switch
    {
        SizeKind.U8 => _reader!.ReadUInt8(),
        SizeKind.U16 => _reader!.ReadUInt16(),
        SizeKind.U32 => _reader!.ReadUInt32(),
        _ => 0,
    };

    public void WriteSize(SizeKind kind, ulong value)
    {
        switch (kind)
        {
            case SizeKind.U8:
                _writer!.WriteUInt8((byte)value);
                break;
            case SizeKind.U16:
                _writer!.WriteUInt16((ushort)value);
                break;
            case SizeKind.U32:
                _writer!.WriteUInt32((uint)value);
                break;
        }
    }

    public void List<T>(List<T> list, SizeKind kind, ElementExchange<T> element) where T : new()
    {
        if (_reader is not null)
        {
            var count = (int)ReadSize(kind);
            list.Clear();
            for (var i = 0; i < count; i++)
            {
                var item = new T();
                element(this, ref item);
                list.Add(item);
            }
        }
        else
        {
            WriteSize(kind, (ulong)list.Count);
            for (var i = 0; i < list.Count; i++)
            {
                var item = list[i];
                element(this, ref item);
                list[i] = item;
            }
        }
    }

    public void Data(ref byte[] data, SizeKind kind)
    {
        if (_reader is not null)
        {
            data = _reader.ReadBytes((int)ReadSize(kind));
        }
        else
        {
            WriteSize(kind, (ulong)data.Length);
            _writer!.WriteBytes(data);
        }
    }
}
