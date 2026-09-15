namespace LiarUtil.Core.Wem;

internal static class VorbisCodebookWriter
{
    public static void Copy(WemBitReader reader, OggPageWriter ogg)
    {
        var id0 = reader.Read(8);
        var id1 = reader.Read(8);
        var id2 = reader.Read(8);
        var dimensions = (ushort)reader.Read(16);
        var entries = reader.Read(24);
        if (id0 != 0x42 || id1 != 0x43 || id2 != 0x56)
        {
            throw new WemException(LiarUtil.Core.Strings.CodebookSyncModeInvalid);
        }

        ogg.WriteBits(id0, 8);
        ogg.WriteBits(id1, 8);
        ogg.WriteBits(id2, 8);
        ogg.WriteBits(dimensions, 16);
        ogg.WriteBits(entries, 24);
        var orderedFlag = (byte)reader.Read(1);
        ogg.WriteBit(orderedFlag);
        if (orderedFlag == 1)
        {
            ogg.WriteBits(reader.Read(5), 5);
            var currentEntry = 0U;
            while (currentEntry < entries)
            {
                var bitCount = IntegerLog(entries - currentEntry);
                var number = reader.Read((int)bitCount);
                ogg.WriteBits(number, (int)bitCount);
                currentEntry += number;
            }

            if (currentEntry > entries)
            {
                throw new WemException(LiarUtil.Core.Strings.ErrorCopyingCodebook);
            }
        }
        else
        {
            var sparseFlag = (byte)reader.Read(1);
            ogg.WriteBit(sparseFlag);
            for (var index = 0U; index < entries; index++)
            {
                if (sparseFlag == 1)
                {
                    ogg.WriteBit((byte)reader.Read(1));
                }
                else
                {
                    ogg.WriteBits(reader.Read(5), 5);
                }
            }
        }

        var lookupType = (byte)reader.Read(4);
        ogg.WriteBits(lookupType, 4);
        if (lookupType != 1)
        {
            throw new WemException(LiarUtil.Core.Strings.ErrorCopyingCodebook);
        }

        ogg.WriteBits(reader.Read(32), 32);
        ogg.WriteBits(reader.Read(32), 32);
        var valueLength = (byte)reader.Read(4);
        ogg.WriteBits(valueLength, 4);
        ogg.WriteBit((byte)reader.Read(1));
        var quantValues = QuantValues(entries, dimensions);
        for (var index = 0U; index < quantValues; index++)
        {
            ogg.WriteBits(reader.Read(valueLength + 1), valueLength + 1);
        }
    }

    public static void Rebuild(WemBitReader reader, uint codebookSize, OggPageWriter ogg)
    {
        var dimensions = (byte)reader.Read(4);
        var entries = (ushort)reader.Read(14);
        ogg.WriteBits(0x564342, 24);
        ogg.WriteBits(dimensions, 16);
        ogg.WriteBits(entries, 24);
        var orderedFlag = (byte)reader.Read(1);
        ogg.WriteBit(orderedFlag);
        if (orderedFlag == 1)
        {
            ogg.WriteBits(reader.Read(5), 5);
            var currentEntry = 0U;
            while (currentEntry < entries)
            {
                var bitCount = IntegerLog(entries - currentEntry);
                var number = reader.Read((int)bitCount);
                ogg.WriteBits(number, (int)bitCount);
                currentEntry += number;
            }

            if (currentEntry > entries)
            {
                throw new WemException(LiarUtil.Core.Strings.ErrorRebuildingCodebook);
            }
        }
        else
        {
            var codewordLengthLength = (byte)reader.Read(3);
            var sparseFlag = (byte)reader.Read(1);
            ogg.WriteBit(sparseFlag);
            if (codewordLengthLength == 0 || codewordLengthLength > 5)
            {
                throw new WemException(LiarUtil.Core.Strings.ErrorRebuildingCodebook);
            }

            for (var index = 0; index < entries; index++)
            {
                var present = true;
                if (sparseFlag == 1)
                {
                    var flag = (byte)reader.Read(1);
                    ogg.WriteBit(flag);
                    present = flag == 1;
                }

                if (present)
                {
                    ogg.WriteBits(reader.Read(codewordLengthLength), 5);
                }
            }
        }

        var lookupType = (byte)reader.Read(1);
        ogg.WriteBits(lookupType, 4);
        if (lookupType == 1)
        {
            ogg.WriteBits(reader.Read(32), 32);
            ogg.WriteBits(reader.Read(32), 32);
            var valueLength = (byte)reader.Read(4);
            ogg.WriteBits(valueLength, 4);
            ogg.WriteBit((byte)reader.Read(1));
            var quantValues = QuantValues(entries, dimensions);
            for (var index = 0U; index < quantValues; index++)
            {
                ogg.WriteBits(reader.Read(valueLength + 1), valueLength + 1);
            }
        }
        else if (lookupType != 0)
        {
            throw new WemException(LiarUtil.Core.Strings.ErrorRebuildingCodebook);
        }

        if (codebookSize != 0 && reader.TotalBitsRead / 8 + 1 != codebookSize)
        {
            throw new WemException(LiarUtil.Core.Strings.ErrorRebuildingCodebook);
        }
    }

    public static uint IntegerLog(uint value)
    {
        var result = 0U;
        while (value != 0)
        {
            result++;
            value >>= 1;
        }

        return result;
    }

    public static uint QuantValues(uint entries, uint dimensions)
    {
        var bits = IntegerLog(entries);
        var values = entries >> (int)((bits - 1) * (dimensions - 1) / dimensions);
        while (true)
        {
            ulong product = 1;
            ulong next = 1;
            for (var index = 0; index < dimensions; index++)
            {
                product *= values;
                next *= values + 1;
            }

            if (product <= entries && next > entries)
            {
                return values;
            }

            if (product > entries)
            {
                values--;
            }
            else
            {
                values++;
            }
        }
    }
}
