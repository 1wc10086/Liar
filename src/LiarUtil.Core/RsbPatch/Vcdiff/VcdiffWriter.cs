namespace LiarUtil.Core.RsbPatch;

internal sealed class VcdiffWriter(int dictionarySize, bool interleaved)
{
    private readonly VcdiffBuffer _instructions = new(1024);

    private readonly VcdiffBuffer _data = new(4096);

    private readonly VcdiffBuffer _addresses = new(1024);

    private readonly VcdiffAddressCache _cache = new();

    private int _targetLength;

    private int _lastOpcodeIndex = -1;

    private VcdiffBuffer DataBuffer => interleaved ? _instructions : _data;

    private VcdiffBuffer AddressBuffer => interleaved ? _instructions : _addresses;

    public void Add(ReadOnlySpan<byte> data)
    {
        EncodeInstruction(VcdiffCodeTable.Add, data.Length);
        DataBuffer.AddRange(data);
        _targetLength += data.Length;
    }

    public void Copy(int offset, int size)
    {
        var mode = _cache.EncodeAddress(offset, dictionarySize + _targetLength, out var encoded);
        EncodeInstruction(VcdiffCodeTable.Copy, size, mode);
        if (VcdiffAddressCache.WriteAddressAsVarInt(mode))
        {
            AddressBuffer.WriteVarInt((int)encoded);
        }
        else
        {
            AddressBuffer.Add((byte)encoded);
        }

        _targetLength += size;
    }

    public byte[] Finish()
    {
        var deltaLength = ComputeDeltaLength();
        var output = new VcdiffBuffer(deltaLength + 16);
        output.Add(VcdiffFormat.SourceFlag);
        output.WriteVarInt(dictionarySize);
        output.WriteVarInt(0);
        output.WriteVarInt(deltaLength);
        var deltaStart = output.Count;
        output.WriteVarInt(_targetLength);
        output.Add(0);
        if (interleaved)
        {
            output.WriteVarInt(0);
            output.WriteVarInt(_instructions.Count);
            output.WriteVarInt(0);
            output.AddBuffer(_instructions);
        }
        else
        {
            output.WriteVarInt(_data.Count);
            output.WriteVarInt(_instructions.Count);
            output.WriteVarInt(_addresses.Count);
            output.AddBuffer(_data);
            output.AddBuffer(_instructions);
            output.AddBuffer(_addresses);
        }

        if (output.Count - deltaStart != deltaLength)
        {
            throw new RsbPatchException(LiarUtil.Core.Strings.VCDiffWindowLengthDoesNotMatchActualOutput);
        }

        return output.ToArray();
    }

    private void EncodeInstruction(byte instruction, int size, byte mode = 0)
    {
        if (_lastOpcodeIndex >= 0)
        {
            var lastOpcode = _instructions[_lastOpcodeIndex];
            if (size <= byte.MaxValue)
            {
                var compound = VcdiffInstructionMap.LookupSecond(lastOpcode, instruction, (byte)size, mode);
                if (compound != VcdiffCodeTable.NoOpcode)
                {
                    _instructions[_lastOpcodeIndex] = (byte)compound;
                    _lastOpcodeIndex = -1;
                    return;
                }
            }

            var compoundZero = VcdiffInstructionMap.LookupSecond(lastOpcode, instruction, 0, mode);
            if (compoundZero != VcdiffCodeTable.NoOpcode)
            {
                _instructions[_lastOpcodeIndex] = (byte)compoundZero;
                _lastOpcodeIndex = -1;
                _instructions.WriteVarInt(size);
                return;
            }
        }

        if (size <= byte.MaxValue)
        {
            var opcode = VcdiffInstructionMap.LookupFirst(instruction, (byte)size, mode);
            if (opcode != VcdiffCodeTable.NoOpcode)
            {
                _instructions.Add((byte)opcode);
                _lastOpcodeIndex = _instructions.Count - 1;
                return;
            }
        }

        var zeroOpcode = VcdiffInstructionMap.LookupFirst(instruction, 0, mode);
        if (zeroOpcode == VcdiffCodeTable.NoOpcode)
        {
            throw new RsbPatchException(LiarUtil.Core.Strings.VCDiffCodeTableLacksUsableOpcodes);
        }

        _instructions.Add((byte)zeroOpcode);
        _lastOpcodeIndex = _instructions.Count - 1;
        _instructions.WriteVarInt(size);
    }

    private int ComputeDeltaLength()
    {
        if (interleaved)
        {
            return VcdiffVarInt.Size(_targetLength) + 1 + VcdiffVarInt.Size(0) + VcdiffVarInt.Size(_instructions.Count) + VcdiffVarInt.Size(0) + _instructions.Count;
        }

        return VcdiffVarInt.Size(_targetLength) + 1 + VcdiffVarInt.Size(_data.Count) + VcdiffVarInt.Size(_instructions.Count)
            + VcdiffVarInt.Size(_addresses.Count) + _data.Count + _instructions.Count + _addresses.Count;
    }
}
