using System.Buffers.Binary;
using System.Text;

namespace LiarUtil.Core.Wem;

internal sealed class WemFile
{
    private const uint Missing = 0xFFFFFFFF;

    private readonly byte[] _source;
    private readonly uint _fmtOffset = Missing;
    private readonly uint _fmtSize = Missing;
    private readonly uint _vorbOffset = Missing;
    private readonly uint _vorbSize = Missing;
    private readonly uint _dataOffset = Missing;
    private readonly uint _dataSize = Missing;
    private readonly uint _setupPacketOffset;
    private readonly uint _firstAudioPacketOffset;
    private readonly uint _sampleCount;
    private readonly uint _loopCount;
    private readonly uint _loopStart;
    private readonly uint _loopEnd;
    private readonly bool _noGranule;
    private readonly bool _modPackets;
    private readonly bool _headerTriadPresent;
    private readonly bool _oldPacketHeaders;
    private readonly byte _blockSize0Pow;
    private readonly byte _blockSize1Pow;
    private readonly int _blockSize0;
    private readonly int _blockSize1;

    public WemFile(byte[] source, WemForcePacketFormat forcePacketFormat)
    {
        _source = source;
        if (source.Length < 12 || !Matches(0, "RIFF"))
        {
            throw new WemException(LiarUtil.Core.Strings.NotSupportedWEMFile);
        }

        if (!Matches(8, "WAVE"))
        {
            throw new WemException(LiarUtil.Core.Strings.MissingWAVEIdentifier);
        }

        var riffSize = Math.Min((long)BinaryPrimitives.ReadUInt32LittleEndian(source.AsSpan(4)) + 8, source.Length);
        var fmtOffset = Missing;
        var fmtSize = Missing;
        var vorbOffset = Missing;
        var vorbSize = Missing;
        var dataOffset = Missing;
        var dataSize = Missing;
        var cueOffset = Missing;
        var smplOffset = Missing;
        var chunkOffset = 12L;
        while (chunkOffset < riffSize)
        {
            if (chunkOffset + 8 > source.Length)
            {
                break;
            }

            var name = Encoding.ASCII.GetString(source, (int)chunkOffset, 4);
            var size = BinaryPrimitives.ReadUInt32LittleEndian(source.AsSpan((int)chunkOffset + 4));
            if ((int)chunkOffset + 8 + size > source.Length)
            {
                size = (uint)Math.Max(source.Length - (int)chunkOffset - 8, 0);
            }

            switch (name)
            {
                case var value when value.StartsWith("fmt", StringComparison.Ordinal):
                    fmtOffset = (uint)chunkOffset + 8;
                    fmtSize = size;
                    break;
                case "cue ":
                    cueOffset = (uint)chunkOffset + 8;
                    break;
                case "smpl":
                    smplOffset = (uint)chunkOffset + 8;
                    break;
                case "vorb":
                    vorbOffset = (uint)chunkOffset + 8;
                    vorbSize = size;
                    break;
                case "data":
                    dataOffset = (uint)chunkOffset + 8;
                    dataSize = size;
                    break;
            }

            chunkOffset += size + 8L;
        }

        if (chunkOffset > riffSize || (fmtOffset == Missing && dataOffset == Missing))
        {
            throw new WemException(LiarUtil.Core.Strings.ErrorReadingFile);
        }

        if (vorbOffset == Missing && fmtSize != 0x42)
        {
            throw new WemException(LiarUtil.Core.Strings.ErrorReadingFile);
        }

        if (vorbOffset != Missing && fmtSize != 0x28 && fmtSize != 0x18 && fmtSize != 0x12)
        {
            throw new WemException(LiarUtil.Core.Strings.ErrorReadingFile);
        }

        if (vorbOffset == Missing && fmtSize == 0x42)
        {
            vorbOffset = fmtOffset + 0x18;
            vorbSize = Missing;
        }

        _fmtOffset = fmtOffset;
        _fmtSize = fmtSize;
        _vorbOffset = vorbOffset;
        _vorbSize = vorbSize;
        _dataOffset = dataOffset;
        _dataSize = dataSize;
        ReadFormat();
        var loopCount = 0U;
        var loopStart = 0U;
        var loopEnd = 0U;
        if (cueOffset != Missing)
        {
            _ = BinaryPrimitives.ReadUInt32LittleEndian(source.AsSpan((int)cueOffset));
        }

        if (smplOffset != Missing)
        {
            loopCount = BinaryPrimitives.ReadUInt32LittleEndian(source.AsSpan((int)smplOffset));
            if (loopCount != 1)
            {
                throw new WemException(LiarUtil.Core.Strings.SMPLChunkLoopCountInvalid);
            }

            loopStart = BinaryPrimitives.ReadUInt32LittleEndian(source.AsSpan((int)smplOffset + 0x2C));
            loopEnd = BinaryPrimitives.ReadUInt32LittleEndian(source.AsSpan((int)smplOffset + 0x30));
        }

        var noGranule = false;
        var modPackets = false;
        var headerTriadPresent = false;
        var oldPacketHeaders = false;
        var blockSize0Pow = (byte)0;
        var blockSize1Pow = (byte)0;
        var offset = vorbOffset;
        switch (vorbSize)
        {
            case 0x28:
            case 0x2C:
                headerTriadPresent = true;
                oldPacketHeaders = true;
                break;
            case Missing:
            case 0x2A:
            case 0x32:
            case 0x34:
                break;
            default:
                throw new WemException(LiarUtil.Core.Strings.VORBChunkSizeInvalid);
        }

        _sampleCount = BinaryPrimitives.ReadUInt32LittleEndian(source.AsSpan((int)offset));
        switch (vorbSize)
        {
            case Missing:
            case 0x2A:
                noGranule = true;
                var modSignal = BinaryPrimitives.ReadUInt32LittleEndian(source.AsSpan((int)vorbOffset + 4));
                modPackets = modSignal is not (0x4A or 0x4B or 0x69 or 0x70);
                offset = vorbOffset + 0x10;
                break;
            default:
                offset = vorbOffset + 0x18;
                break;
        }

        if (forcePacketFormat == WemForcePacketFormat.ForceNoModPackets)
        {
            modPackets = false;
        }
        else if (forcePacketFormat == WemForcePacketFormat.ForceModPackets)
        {
            modPackets = true;
        }

        _modPackets = modPackets;
        _noGranule = noGranule;
        _headerTriadPresent = headerTriadPresent;
        _oldPacketHeaders = oldPacketHeaders;
        _setupPacketOffset = BinaryPrimitives.ReadUInt32LittleEndian(source.AsSpan((int)offset));
        _firstAudioPacketOffset = BinaryPrimitives.ReadUInt32LittleEndian(source.AsSpan((int)offset + 4));
        switch (vorbSize)
        {
            case Missing:
            case 0x2A:
                offset = vorbOffset + 0x24;
                break;
            case 0x32:
            case 0x34:
                offset = vorbOffset + 0x2C;
                break;
        }

        switch (vorbSize)
        {
            case 0x28:
            case 0x2C:
                break;
            case Missing:
            case 0x2A:
            case 0x32:
            case 0x34:
                _ = BinaryPrimitives.ReadUInt32LittleEndian(source.AsSpan((int)offset));
                blockSize0Pow = source[(int)offset + 4];
                blockSize1Pow = source[(int)offset + 5];
                break;
        }

        _blockSize0Pow = blockSize0Pow;
        _blockSize1Pow = blockSize1Pow;
        _blockSize0 = blockSize0Pow is 0 ? 0 : 1 << blockSize0Pow;
        _blockSize1 = blockSize1Pow is 0 ? 0 : 1 << blockSize1Pow;
        _loopCount = loopCount;
        _loopStart = loopStart;
        _loopEnd = loopEnd;
        if (loopCount != 0)
        {
            if (loopEnd == 0)
            {
                _loopEnd = _sampleCount;
            }
            else
            {
                _loopEnd = loopEnd + 1;
            }

            if (loopStart >= _sampleCount || _loopEnd > _sampleCount || loopStart > _loopEnd)
            {
                throw new WemException(LiarUtil.Core.Strings.LoopRangeExceedsLimit);
            }
        }
    }

    public ushort Channels { get; private set; }

    public uint SampleRate { get; private set; }

    public uint AverageBytesPerSecond { get; private set; }

    public void GenerateOgg(Stream output, byte[] packedCodebooks, bool fullSetup)
    {
        if (packedCodebooks.Length == 0)
        {
            throw new WemException(LiarUtil.Core.Strings.PackedCodebookUnavailable);
        }

        using var stream = new MemoryStream(_source, writable: false);
        var ogg = new OggPageWriter(output);
        bool[]? modeBlockFlag = null;
        var modeBits = 0U;
        var previousBlockFlag = false;
        if (_headerTriadPresent)
        {
            GenerateHeaderTriad(ogg, stream);
        }
        else
        {
            GenerateHeader(ogg, stream, packedCodebooks, fullSetup, ref modeBlockFlag, ref modeBits);
        }

        var offset = _dataOffset + _firstAudioPacketOffset;
        var dataEnd = _dataOffset + _dataSize;
        ulong granule = 0;
        var previousBlockSize = 0;
        while (offset < dataEnd)
        {
            uint size;
            uint packetHeaderSize;
            uint packetPayloadOffset;
            uint nextOffset;
            uint packetGranule;
            if (_oldPacketHeaders)
            {
                var packet = new WemPacket8(stream, offset);
                packetHeaderSize = packet.HeaderSize;
                size = packet.Size;
                packetPayloadOffset = packet.PayloadOffset;
                packetGranule = packet.Granule;
                nextOffset = packet.NextOffset;
            }
            else
            {
                var packet = new WemPacket(stream, offset, _noGranule);
                packetHeaderSize = packet.HeaderSize;
                size = packet.Size;
                packetPayloadOffset = packet.PayloadOffset;
                packetGranule = packet.Granule;
                nextOffset = packet.NextOffset;
            }

            if (offset + packetHeaderSize > _source.Length)
            {
                break;
            }

            offset = packetPayloadOffset;
            if (modeBlockFlag is null)
            {
                granule = packetGranule == Missing ? 1UL : packetGranule;
            }
            else
            {
                var modeNumber = WemModeReader.Read(_source, (int)offset, (int)modeBits, _modPackets);
                var blockSize = modeBlockFlag[modeNumber] ? _blockSize1 : _blockSize0;
                if (previousBlockSize != 0)
                {
                    granule += (ulong)((previousBlockSize + blockSize) / 4);
                }

                previousBlockSize = blockSize;
            }

            ogg.SetGranule(granule);
            stream.Seek(offset, SeekOrigin.Begin);
            if (_modPackets)
            {
                if (modeBlockFlag is null)
                {
                    throw new WemException(LiarUtil.Core.Strings.ErrorGeneratingVorbisPackets);
                }

                ogg.WriteBit(0);
                var bitReader = new WemBitReader(stream);
                var modeNumber = bitReader.Read((int)modeBits);
                ogg.WriteBits(modeNumber, (int)modeBits);
                var remainder = bitReader.Read(8 - (int)modeBits);
                if (modeBlockFlag[modeNumber])
                {
                    var nextBlockFlag = false;
                    if (nextOffset + packetHeaderSize <= dataEnd)
                    {
                        var nextPacket = new WemPacket(stream, nextOffset, _noGranule);
                        if (nextPacket.Size != 0xFFFF)
                        {
                            stream.Seek(nextPacket.PayloadOffset, SeekOrigin.Begin);
                            var nextMode = new WemBitReader(stream).Read((int)modeBits);
                            nextBlockFlag = modeBlockFlag[nextMode];
                        }
                    }

                    ogg.WriteBit(previousBlockFlag ? (byte)1 : (byte)0);
                    ogg.WriteBit(nextBlockFlag ? (byte)1 : (byte)0);
                    stream.Seek(offset + 1, SeekOrigin.Begin);
                }

                previousBlockFlag = modeBlockFlag[modeNumber];
                ogg.WriteBits(remainder, 8 - (int)modeBits);
            }
            else
            {
                var value = stream.ReadByte();
                if (value < 0)
                {
                    throw new WemException(LiarUtil.Core.Strings.ErrorGeneratingVorbisPackets);
                }

                ogg.WriteBits((byte)value, 8);
            }

            for (var index = 1U; index < size; index++)
            {
                var value = stream.ReadByte();
                if (value < 0)
                {
                    throw new WemException(LiarUtil.Core.Strings.ErrorGeneratingVorbisPackets);
                }

                ogg.WriteBits((byte)value, 8);
            }

            offset = nextOffset;
            ogg.FlushPage(false, offset == dataEnd);
        }

        if (offset > dataEnd)
        {
            throw new WemException(LiarUtil.Core.Strings.ErrorCreatingOGGFile);
        }
    }

    private void GenerateHeaderTriad(OggPageWriter ogg, MemoryStream stream)
    {
        var offset = _dataOffset + _setupPacketOffset;
        var information = new WemPacket8(stream, offset);
        if (information.Granule != 0)
        {
            throw new WemException(LiarUtil.Core.Strings.ErrorCreatingFileHeader);
        }

        stream.Seek(information.PayloadOffset, SeekOrigin.Begin);
        if (stream.ReadByte() != 1)
        {
            throw new WemException(LiarUtil.Core.Strings.ErrorCreatingFileHeader);
        }

        ogg.WriteBits(1, 8);
        for (var index = 0U; index < information.Size; index++)
        {
            ogg.WriteBits(stream.ReadByte(), 8);
        }

        ogg.FlushPage();
        offset = information.NextOffset;
        var comment = new WemPacket8(stream, offset);
        if (comment.Granule != 0)
        {
            throw new WemException(LiarUtil.Core.Strings.ErrorCreatingFileHeader);
        }

        stream.Seek(comment.PayloadOffset, SeekOrigin.Begin);
        if (stream.ReadByte() != 3)
        {
            throw new WemException(LiarUtil.Core.Strings.ErrorCreatingFileHeader);
        }

        ogg.WriteBits(3, 8);
        for (var index = 0U; index < comment.Size; index++)
        {
            ogg.WriteBits(stream.ReadByte(), 8);
        }

        ogg.FlushPage();
        offset = comment.NextOffset;
        var setup = new WemPacket8(stream, offset);
        stream.Seek(setup.PayloadOffset, SeekOrigin.Begin);
        if (setup.Granule != 0)
        {
            throw new WemException(LiarUtil.Core.Strings.ErrorCreatingFileHeader);
        }

        var reader = new WemBitReader(stream);
        if (reader.Read(8) != 5)
        {
            throw new WemException(LiarUtil.Core.Strings.ErrorCreatingFileHeader);
        }

        ogg.WriteBits(5, 8);
        for (var index = 0; index < 6; index++)
        {
            ogg.WriteBits(reader.Read(8), 8);
        }

        var codebookCount = (byte)reader.Read(8);
        ogg.WriteBits(codebookCount, 8);
        var count = codebookCount + 1;
        for (var index = 0; index < count; index++)
        {
            VorbisCodebookWriter.Copy(reader, ogg);
        }

        while (reader.TotalBitsRead < setup.Size * 8)
        {
            ogg.WriteBit((byte)reader.Read(1));
        }

        ogg.FlushPage();
        if (setup.NextOffset != _dataOffset + _firstAudioPacketOffset)
        {
            throw new WemException(LiarUtil.Core.Strings.ErrorCreatingFileHeader);
        }
    }

    private void GenerateHeader(OggPageWriter ogg, MemoryStream stream, byte[] packedCodebooks, bool fullSetup, ref bool[]? modeBlockFlag, ref uint modeBits)
    {
        ogg.WriteVorbisHeader(1);
        ogg.WriteBits(0, 32);
        ogg.WriteBits(Channels, 8);
        ogg.WriteBits(SampleRate, 32);
        ogg.WriteBits(0, 32);
        ogg.WriteBits(AverageBytesPerSecond * 8, 32);
        ogg.WriteBits(0, 32);
        ogg.WriteBits(_blockSize0Pow, 4);
        ogg.WriteBits(_blockSize1Pow, 4);
        ogg.WriteBit(1);
        ogg.FlushPage();
        ogg.WriteVorbisHeader(3);
        var vendor = Encoding.UTF8.GetBytes("Converted from Audiokinetic Wwise by WEMSharp");
        ogg.WriteBits(vendor.Length, 32);
        foreach (var item in vendor)
        {
            ogg.WriteBits(item, 8);
        }

        if (_loopCount == 0)
        {
            ogg.WriteBits(0, 32);
        }
        else
        {
            ogg.WriteBits(2, 32);
            var loopStart = Encoding.UTF8.GetBytes($"LoopStart={_loopStart}");
            ogg.WriteBits(loopStart.Length, 32);
            foreach (var item in loopStart)
            {
                ogg.WriteBits(item, 8);
            }

            var loopEnd = Encoding.UTF8.GetBytes($"LoopEnd={_loopEnd}");
            ogg.WriteBits(loopEnd.Length, 32);
            foreach (var item in loopEnd)
            {
                ogg.WriteBits(item, 8);
            }
        }

        ogg.WriteBit(1);
        ogg.FlushPage();
        ogg.WriteVorbisHeader(5);
        var setupPacket = new WemPacket(stream, _dataOffset + _setupPacketOffset, _noGranule);
        stream.Seek(setupPacket.PayloadOffset, SeekOrigin.Begin);
        if (setupPacket.Granule != 0)
        {
            throw new WemException(LiarUtil.Core.Strings.ErrorGeneratingVorbisPackets);
        }

        var reader = new WemBitReader(stream);
        var codebookCount = reader.Read(8);
        ogg.WriteBits(codebookCount, 8);
        var count = codebookCount + 1U;
        if (fullSetup)
        {
            for (var index = 0U; index < count; index++)
            {
                VorbisCodebookWriter.Copy(reader, ogg);
            }
        }
        else
        {
            var library = new PackedCodebookLibrary(packedCodebooks);
            for (var index = 0U; index < count; index++)
            {
                var codebookId = (uint)reader.Read(10);
                var data = library.GetCodebook(codebookId);
                using var codebookStream = new MemoryStream(data, writable: false);
                VorbisCodebookWriter.Rebuild(new WemBitReader(codebookStream), library.GetCodebookSize(codebookId), ogg);
            }
        }

        ogg.WriteBits(0, 6);
        ogg.WriteBits(0, 16);
        if (fullSetup)
        {
            while (reader.TotalBitsRead < setupPacket.Size * 8U)
            {
                ogg.WriteBit((byte)reader.Read(1));
            }

            ogg.FlushPage();
            return;
        }

        var floorCount = (byte)reader.Read(6);
        ogg.WriteBits(floorCount, 6);
        var floors = floorCount + 1;
        for (var index = 0; index < floors; index++)
        {
            ogg.WriteBits(1, 16);
            var partitionCount = (byte)reader.Read(5);
            ogg.WriteBits(partitionCount, 5);
            var partitionClasses = new uint[partitionCount];
            var maximumClass = 0U;
            for (var inner = 0; inner < partitionCount; inner++)
            {
                var partitionClass = (byte)reader.Read(4);
                ogg.WriteBits(partitionClass, 4);
                partitionClasses[inner] = partitionClass;
                maximumClass = Math.Max(maximumClass, partitionClass);
            }

            var classDimensions = new uint[maximumClass + 1];
            for (var inner = 0U; inner <= maximumClass; inner++)
            {
                var classDimension = (byte)reader.Read(3);
                ogg.WriteBits(classDimension, 3);
                classDimensions[inner] = classDimension + 1U;
                var classSubclasses = (byte)reader.Read(2);
                ogg.WriteBits(classSubclasses, 2);
                if (classSubclasses != 0)
                {
                    ogg.WriteBits(reader.Read(8), 8);
                    if (maximumClass >= count)
                    {
                        throw new WemException(LiarUtil.Core.Strings.ErrorGeneratingVorbisPackets);
                    }
                }

                for (var sub = 0; sub < 1 << classSubclasses; sub++)
                {
                    var subclassBook = (byte)reader.Read(8);
                    ogg.WriteBits(subclassBook, 8);
                    if (subclassBook - 1 >= 0 && subclassBook - 1 >= count)
                    {
                        throw new WemException(LiarUtil.Core.Strings.ErrorGeneratingVorbisPackets);
                    }
                }
            }

            ogg.WriteBits(reader.Read(2), 2);
            var rangeBits = (byte)reader.Read(4);
            ogg.WriteBits(rangeBits, 4);
            for (var inner = 0; inner < partitionCount; inner++)
            {
                for (var sub = 0U; sub < classDimensions[partitionClasses[inner]]; sub++)
                {
                    ogg.WriteBits(reader.Read(rangeBits), rangeBits);
                }
            }
        }

        var residueCount = (byte)reader.Read(6);
        ogg.WriteBits(residueCount, 6);
        var residues = residueCount + 1;
        for (var index = 0; index < residues; index++)
        {
            var residueType = (byte)reader.Read(2);
            ogg.WriteBits(residueType, 16);
            if (residueType > 2)
            {
                throw new WemException(LiarUtil.Core.Strings.ErrorGeneratingVorbisPackets);
            }

            ogg.WriteBits(reader.Read(24), 24);
            ogg.WriteBits(reader.Read(24), 24);
            ogg.WriteBits(reader.Read(24), 24);
            var classifications = (byte)reader.Read(6);
            var classbook = (byte)reader.Read(8);
            ogg.WriteBits(classifications, 6);
            ogg.WriteBits(classbook, 8);
            var classificationCount = classifications + 1;
            if (classbook >= count)
            {
                throw new WemException(LiarUtil.Core.Strings.ErrorGeneratingVorbisPackets);
            }

            var cascades = new uint[classificationCount];
            for (var inner = 0; inner < classificationCount; inner++)
            {
                var lowBits = (byte)reader.Read(3);
                ogg.WriteBits(lowBits, 3);
                var flag = (byte)reader.Read(1);
                ogg.WriteBit(flag);
                var highBits = flag == 1 ? (byte)reader.Read(5) : (byte)0;
                if (flag == 1)
                {
                    ogg.WriteBits(highBits, 5);
                }

                cascades[inner] = (uint)(highBits * 8 + lowBits);
            }

            for (var inner = 0; inner < classificationCount; inner++)
            {
                for (var bit = 0; bit < 8; bit++)
                {
                    if ((cascades[inner] & (1U << bit)) != 0)
                    {
                        var residueBook = (byte)reader.Read(8);
                        ogg.WriteBits(residueBook, 8);
                        if (residueBook >= count)
                        {
                            throw new WemException(LiarUtil.Core.Strings.ErrorGeneratingVorbisPackets);
                        }
                    }
                }
            }
        }

        var mappingCount = (byte)reader.Read(6);
        ogg.WriteBits(mappingCount, 6);
        var mappings = mappingCount + 1;
        for (var index = 0; index < mappings; index++)
        {
            ogg.WriteBits(0, 16);
            var submapsFlag = (byte)reader.Read(1);
            ogg.WriteBit(submapsFlag);
            var submaps = 1U;
            if (submapsFlag == 1)
            {
                var submapsLess = (byte)reader.Read(4);
                submaps = (uint)(submapsLess + 1);
                ogg.WriteBits(submapsLess, 4);
            }

            var squarePolarFlag = (byte)reader.Read(1);
            ogg.WriteBit(squarePolarFlag);
            if (squarePolarFlag == 1)
            {
                var couplingSteps = (byte)reader.Read(8);
                ogg.WriteBits(couplingSteps, 8);
                var couplings = couplingSteps + 1;
                var bits = (int)VorbisCodebookWriter.IntegerLog(Channels - 1U);
                for (var inner = 0; inner < couplings; inner++)
                {
                    var magnitude = reader.Read(bits);
                    var angle = reader.Read(bits);
                    ogg.WriteBits(magnitude, bits);
                    ogg.WriteBits(angle, bits);
                    if (angle == magnitude || magnitude >= Channels || angle >= Channels)
                    {
                        throw new WemException(LiarUtil.Core.Strings.ErrorGeneratingVorbisPackets);
                    }
                }
            }

            var mappingReserved = (byte)reader.Read(2);
            ogg.WriteBits(mappingReserved, 2);
            if (mappingReserved != 0)
            {
                throw new WemException(LiarUtil.Core.Strings.ErrorGeneratingVorbisPackets);
            }

            if (submaps > 1)
            {
                for (var channel = 0; channel < Channels; channel++)
                {
                    var mux = (byte)reader.Read(4);
                    ogg.WriteBits(mux, 4);
                    if (mux >= submaps)
                    {
                        throw new WemException(LiarUtil.Core.Strings.ErrorGeneratingVorbisPackets);
                    }
                }
            }

            for (var sub = 0U; sub < submaps; sub++)
            {
                ogg.WriteBits(reader.Read(8), 8);
                var floorNumber = (byte)reader.Read(8);
                ogg.WriteBits(floorNumber, 8);
                if (floorNumber >= floors)
                {
                    throw new WemException(LiarUtil.Core.Strings.ErrorGeneratingVorbisPackets);
                }

                var residueNumber = (byte)reader.Read(8);
                ogg.WriteBits(residueNumber, 8);
                if (residueNumber >= residues)
                {
                    throw new WemException(LiarUtil.Core.Strings.ErrorGeneratingVorbisPackets);
                }
            }
        }

        var modeCount = (byte)reader.Read(6);
        ogg.WriteBits(modeCount, 6);
        var modes = modeCount + 1;
        modeBlockFlag = new bool[modes];
        modeBits = VorbisCodebookWriter.IntegerLog((uint)(modes - 1));
        for (var index = 0; index < modes; index++)
        {
            var blockFlag = (byte)reader.Read(1);
            ogg.WriteBit(blockFlag);
            modeBlockFlag[index] = blockFlag == 1;
            ogg.WriteBits(0, 16);
            ogg.WriteBits(0, 16);
            var mapping = (byte)reader.Read(8);
            ogg.WriteBits(mapping, 8);
            if (mapping >= mappings)
            {
                throw new WemException(LiarUtil.Core.Strings.ErrorGeneratingVorbisPackets);
            }
        }

        ogg.WriteBit(1);
        ogg.FlushPage();
        if ((reader.TotalBitsRead + 7) / 8 != setupPacket.Size)
        {
            throw new WemException(LiarUtil.Core.Strings.ErrorGeneratingVorbisPackets);
        }

        if (setupPacket.NextOffset != _dataOffset + _firstAudioPacketOffset)
        {
            throw new WemException(LiarUtil.Core.Strings.ErrorGeneratingVorbisPackets);
        }
    }

    private void ReadFormat()
    {
        var offset = (int)_fmtOffset;
        if (BinaryPrimitives.ReadUInt16LittleEndian(_source.AsSpan(offset)) != 0xFFFF)
        {
            throw new WemException(LiarUtil.Core.Strings.FMTChunkCodecIdentifierError);
        }

        Channels = BinaryPrimitives.ReadUInt16LittleEndian(_source.AsSpan(offset + 2));
        SampleRate = BinaryPrimitives.ReadUInt32LittleEndian(_source.AsSpan(offset + 4));
        AverageBytesPerSecond = BinaryPrimitives.ReadUInt32LittleEndian(_source.AsSpan(offset + 8));
        if (BinaryPrimitives.ReadUInt16LittleEndian(_source.AsSpan(offset + 12)) != 0)
        {
            throw new WemException(LiarUtil.Core.Strings.FMTChunkBlockAlignmentError);
        }

        if (BinaryPrimitives.ReadUInt16LittleEndian(_source.AsSpan(offset + 14)) != 0)
        {
            throw new WemException(LiarUtil.Core.Strings.FMTChunkBitDepthError);
        }

        if (BinaryPrimitives.ReadUInt16LittleEndian(_source.AsSpan(offset + 16)) != _fmtSize - 0x12)
        {
            throw new WemException(LiarUtil.Core.Strings.FMTChunkExtensionLengthError);
        }

        if (_fmtSize != 0x28)
        {
            return;
        }

        ReadOnlySpan<byte> expected = [1, 0, 0, 0, 0, 0, 0x10, 0, 0x80, 0, 0, 0xAA, 0, 0x38, 0x9b, 0x71];
        if (!_source.AsSpan(offset + 0x18, 16).SequenceEqual(expected))
        {
            throw new WemException(LiarUtil.Core.Strings.FMTChunkUnknownBufferSignatureError);
        }
    }

    private bool Matches(int offset, string value) =>
        offset + value.Length <= _source.Length && Encoding.ASCII.GetString(_source, offset, value.Length) == value;
}
