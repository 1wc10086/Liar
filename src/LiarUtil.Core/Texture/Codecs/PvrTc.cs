using System.Buffers.Binary;

namespace LiarUtil.Core.Texture.Codecs;

public static class PvrTc
{
    private struct PvrTcPacket
    {
        public uint ModulationData;
        public uint Flags;

        public int UsePunchthroughAlpha => (int)(Flags & 1u);
        public int ColorA => (int)((Flags >> 1) & 0x3FFFu);
        public int ColorAIsOpaque => (int)((Flags >> 15) & 1u);
        public int ColorB => (int)((Flags >> 16) & 0x7FFFu);
        public int ColorBIsOpaque => (int)(Flags >> 31);
    }

    private static readonly byte[] BITSCALE_5_TO_8 =
    {
        0, 8, 16, 24, 32, 41, 49, 57, 65, 74, 82, 90, 98, 106, 115, 123,
        131, 139, 148, 156, 164, 172, 180, 189, 197, 205, 213, 222, 230, 238, 246, 255
    };

    private static readonly byte[] BITSCALE_4_TO_8 =
    {
        0, 17, 34, 51, 68, 85, 102, 119, 136, 153, 170, 187, 204, 221, 238, 255
    };

    private static readonly byte[] BITSCALE_3_TO_8 =
    {
        0, 36, 72, 109, 145, 182, 218, 255
    };

    private static readonly byte[] BITSCALE_8_TO_5_FLOOR =
    {
        0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1,
        1, 2, 2, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3,
        3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5,
        5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7, 7,
        7, 7, 8, 8, 8, 8, 8, 8, 8, 8, 8, 9, 9, 9, 9, 9,
        9, 9, 9, 10, 10, 10, 10, 10, 10, 10, 10, 11, 11, 11, 11, 11,
        11, 11, 11, 12, 12, 12, 12, 12, 12, 12, 12, 13, 13, 13, 13, 13,
        13, 13, 13, 13, 14, 14, 14, 14, 14, 14, 14, 14, 15, 15, 15, 15,
        15, 15, 15, 15, 16, 16, 16, 16, 16, 16, 16, 16, 17, 17, 17, 17,
        17, 17, 17, 17, 17, 18, 18, 18, 18, 18, 18, 18, 18, 19, 19, 19,
        19, 19, 19, 19, 19, 20, 20, 20, 20, 20, 20, 20, 20, 21, 21, 21,
        21, 21, 21, 21, 21, 22, 22, 22, 22, 22, 22, 22, 22, 22, 23, 23,
        23, 23, 23, 23, 23, 23, 24, 24, 24, 24, 24, 24, 24, 24, 25, 25,
        25, 25, 25, 25, 25, 25, 26, 26, 26, 26, 26, 26, 26, 26, 26, 27,
        27, 27, 27, 27, 27, 27, 27, 28, 28, 28, 28, 28, 28, 28, 28, 29,
        29, 29, 29, 29, 29, 29, 29, 30, 30, 30, 30, 30, 30, 30, 30, 31
    };

    private static readonly byte[] BITSCALE_8_TO_4_FLOOR =
    {
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
        2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
        3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
        4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
        5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
        6, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7, 7, 7, 7, 7,
        7, 7, 7, 7, 7, 7, 7, 7, 8, 8, 8, 8, 8, 8, 8, 8,
        8, 8, 8, 8, 8, 8, 8, 8, 8, 9, 9, 9, 9, 9, 9, 9,
        9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 10, 10, 10, 10, 10, 10,
        10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 11, 11, 11, 11, 11,
        11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 12, 12, 12, 12,
        12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 13, 13, 13,
        13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 14, 14,
        14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 15
    };

    private static readonly byte[] BITSCALE_8_TO_3_FLOOR =
    {
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2,
        2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
        2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 3, 3,
        3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
        3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
        3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
        4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
        4, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5,
        5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
        5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6,
        6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
        6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 7
    };

    private static readonly byte[] BITSCALE_8_TO_5_CEIL =
    {
        0, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2,
        2, 3, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4,
        4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6,
        6, 6, 7, 7, 7, 7, 7, 7, 7, 7, 8, 8, 8, 8, 8, 8,
        8, 8, 9, 9, 9, 9, 9, 9, 9, 9, 9, 10, 10, 10, 10, 10,
        10, 10, 10, 11, 11, 11, 11, 11, 11, 11, 11, 12, 12, 12, 12, 12,
        12, 12, 12, 13, 13, 13, 13, 13, 13, 13, 13, 14, 14, 14, 14, 14,
        14, 14, 14, 14, 15, 15, 15, 15, 15, 15, 15, 15, 16, 16, 16, 16,
        16, 16, 16, 16, 17, 17, 17, 17, 17, 17, 17, 17, 18, 18, 18, 18,
        18, 18, 18, 18, 18, 19, 19, 19, 19, 19, 19, 19, 19, 20, 20, 20,
        20, 20, 20, 20, 20, 21, 21, 21, 21, 21, 21, 21, 21, 22, 22, 22,
        22, 22, 22, 22, 22, 23, 23, 23, 23, 23, 23, 23, 23, 23, 24, 24,
        24, 24, 24, 24, 24, 24, 25, 25, 25, 25, 25, 25, 25, 25, 26, 26,
        26, 26, 26, 26, 26, 26, 27, 27, 27, 27, 27, 27, 27, 27, 27, 28,
        28, 28, 28, 28, 28, 28, 28, 29, 29, 29, 29, 29, 29, 29, 29, 30,
        30, 30, 30, 30, 30, 30, 30, 31, 31, 31, 31, 31, 31, 31, 31, 31
    };

    private static readonly byte[] BITSCALE_8_TO_4_CEIL =
    {
        0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
        2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
        3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
        4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
        5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
        6, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7, 7, 7, 7, 7,
        7, 7, 7, 7, 7, 7, 7, 7, 8, 8, 8, 8, 8, 8, 8, 8,
        8, 8, 8, 8, 8, 8, 8, 8, 8, 9, 9, 9, 9, 9, 9, 9,
        9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 10, 10, 10, 10, 10, 10,
        10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 11, 11, 11, 11, 11,
        11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 12, 12, 12, 12,
        12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 13, 13, 13,
        13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 14, 14,
        14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 15,
        15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15
    };

    private static readonly byte[] BITSCALE_8_TO_3_CEIL =
    {
        0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
        2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
        2, 2, 2, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3,
        3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
        3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 4, 4,
        4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
        4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
        4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
        5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
        5, 5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6,
        6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
        6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7,
        7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
        7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7
    };

    private static readonly ushort[] MORTON_TABLE =
    {
        0, 1, 4, 5, 16, 17, 20, 21, 64, 65, 68, 69, 80, 81, 84, 85,
        256, 257, 260, 261, 272, 273, 276, 277, 320, 321, 324, 325, 336, 337, 340, 341,
        1024, 1025, 1028, 1029, 1040, 1041, 1044, 1045, 1088, 1089, 1092, 1093, 1104, 1105, 1108, 1109,
        1280, 1281, 1284, 1285, 1296, 1297, 1300, 1301, 1344, 1345, 1348, 1349, 1360, 1361, 1364, 1365,
        4096, 4097, 4100, 4101, 4112, 4113, 4116, 4117, 4160, 4161, 4164, 4165, 4176, 4177, 4180, 4181,
        4352, 4353, 4356, 4357, 4368, 4369, 4372, 4373, 4416, 4417, 4420, 4421, 4432, 4433, 4436, 4437,
        5120, 5121, 5124, 5125, 5136, 5137, 5140, 5141, 5184, 5185, 5188, 5189, 5200, 5201, 5204, 5205,
        5376, 5377, 5380, 5381, 5392, 5393, 5396, 5397, 5440, 5441, 5444, 5445, 5456, 5457, 5460, 5461,
        16384, 16385, 16388, 16389, 16400, 16401, 16404, 16405, 16448, 16449, 16452, 16453, 16464, 16465, 16468, 16469,
        16640, 16641, 16644, 16645, 16656, 16657, 16660, 16661, 16704, 16705, 16708, 16709, 16720, 16721, 16724, 16725,
        17408, 17409, 17412, 17413, 17424, 17425, 17428, 17429, 17472, 17473, 17476, 17477, 17488, 17489, 17492, 17493,
        17664, 17665, 17668, 17669, 17680, 17681, 17684, 17685, 17728, 17729, 17732, 17733, 17744, 17745, 17748, 17749,
        20480, 20481, 20484, 20485, 20496, 20497, 20500, 20501, 20544, 20545, 20548, 20549, 20560, 20561, 20564, 20565,
        20736, 20737, 20740, 20741, 20752, 20753, 20756, 20757, 20800, 20801, 20804, 20805, 20816, 20817, 20820, 20821,
        21504, 21505, 21508, 21509, 21520, 21521, 21524, 21525, 21568, 21569, 21572, 21573, 21584, 21585, 21588, 21589,
        21760, 21761, 21764, 21765, 21776, 21777, 21780, 21781, 21824, 21825, 21828, 21829, 21840, 21841, 21844, 21845
    };

    private static readonly byte[] BILINEAR_FACTORS =
    {
        4, 4, 4, 4,
        2, 6, 2, 6,
        8, 0, 8, 0,
        6, 2, 6, 2,
        2, 2, 6, 6,
        1, 3, 3, 9,
        4, 0, 12, 0,
        3, 1, 9, 3,
        8, 8, 0, 0,
        4, 12, 0, 0,
        16, 0, 0, 0,
        12, 4, 0, 0,
        6, 6, 2, 2,
        3, 9, 1, 3,
        12, 0, 4, 0,
        9, 3, 3, 1
    };

    private static readonly byte[] WEIGHTS =
    {
        8, 0, 8, 0,
        5, 3, 5, 3,
        3, 5, 3, 5,
        0, 8, 0, 8,
        8, 0, 8, 0,
        4, 4, 4, 4,
        4, 4, 0, 0,
        0, 8, 0, 8
    };

    private static PvrTcPacket ReadPacket(ReadOnlySpan<byte> data, int index)
    {
        int offset = index * 8;
        PvrTcPacket packet;
        packet.ModulationData = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset, 4));
        packet.Flags = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset + 4, 4));
        return packet;
    }

    private static void WritePacket(Span<byte> data, int index, PvrTcPacket packet)
    {
        int offset = index * 8;
        BinaryPrimitives.WriteUInt32LittleEndian(data.Slice(offset, 4), packet.ModulationData);
        BinaryPrimitives.WriteUInt32LittleEndian(data.Slice(offset + 4, 4), packet.Flags);
    }

    private static int GetMortonNumber(int x, int y)
    {
        unchecked
        {
            return (MORTON_TABLE[x >> 8] << 17) | (MORTON_TABLE[y >> 8] << 16) | (MORTON_TABLE[x & 0xFF] << 1) | MORTON_TABLE[y & 0xFF];
        }
    }

    private static uint RotateRight(uint value, int shift)
    {
        shift &= 31;
        if (shift == 0)
        {
            return value;
        }
        return (value >> shift) | (value << (32 - shift));
    }

    private static (int R, int G, int B) GetColorRgbA(in PvrTcPacket packet)
    {
        if (packet.ColorAIsOpaque != 0)
        {
            int r = packet.ColorA >> 9;
            int g = (packet.ColorA >> 4) & 0x1F;
            int b = packet.ColorA & 0xF;
            return (BITSCALE_5_TO_8[r], BITSCALE_5_TO_8[g], BITSCALE_4_TO_8[b]);
        }
        int rr = (packet.ColorA >> 7) & 0xF;
        int gg = (packet.ColorA >> 3) & 0xF;
        int bb = packet.ColorA & 7;
        return (BITSCALE_4_TO_8[rr], BITSCALE_4_TO_8[gg], BITSCALE_3_TO_8[bb]);
    }

    private static (int R, int G, int B) GetColorRgbB(in PvrTcPacket packet)
    {
        if (packet.ColorBIsOpaque != 0)
        {
            int r = packet.ColorB >> 10;
            int g = (packet.ColorB >> 5) & 0x1F;
            int b = packet.ColorB & 0x1F;
            return (BITSCALE_5_TO_8[r], BITSCALE_5_TO_8[g], BITSCALE_5_TO_8[b]);
        }
        int rr = (packet.ColorB >> 8) & 0xF;
        int gg = (packet.ColorB >> 4) & 0xF;
        int bb = packet.ColorB & 0xF;
        return (BITSCALE_4_TO_8[rr], BITSCALE_4_TO_8[gg], BITSCALE_4_TO_8[bb]);
    }

    private static (int R, int G, int B, int A) GetColorRgbaA(in PvrTcPacket packet)
    {
        if (packet.ColorAIsOpaque != 0)
        {
            int r = packet.ColorA >> 9;
            int g = (packet.ColorA >> 4) & 0x1F;
            int b = packet.ColorA & 0xF;
            return (BITSCALE_5_TO_8[r], BITSCALE_5_TO_8[g], BITSCALE_4_TO_8[b], 255);
        }
        int a = (packet.ColorA >> 11) & 7;
        int rr = (packet.ColorA >> 7) & 0xF;
        int gg = (packet.ColorA >> 3) & 0xF;
        int bb = packet.ColorA & 7;
        return (BITSCALE_4_TO_8[rr], BITSCALE_4_TO_8[gg], BITSCALE_3_TO_8[bb], BITSCALE_3_TO_8[a]);
    }

    private static (int R, int G, int B, int A) GetColorRgbaB(in PvrTcPacket packet)
    {
        if (packet.ColorBIsOpaque != 0)
        {
            int r = packet.ColorB >> 10;
            int g = (packet.ColorB >> 5) & 0x1F;
            int b = packet.ColorB & 0x1F;
            return (BITSCALE_5_TO_8[r], BITSCALE_5_TO_8[g], BITSCALE_5_TO_8[b], 255);
        }
        int a = (packet.ColorB >> 12) & 7;
        int rr = (packet.ColorB >> 8) & 0xF;
        int gg = (packet.ColorB >> 4) & 0xF;
        int bb = packet.ColorB & 0xF;
        return (BITSCALE_4_TO_8[rr], BITSCALE_4_TO_8[gg], BITSCALE_4_TO_8[bb], BITSCALE_3_TO_8[a]);
    }

    private static void SetColorA(ref PvrTcPacket packet, int r, int g, int b)
    {
        int rr = BITSCALE_8_TO_5_FLOOR[r];
        int gg = BITSCALE_8_TO_5_FLOOR[g];
        int bb = BITSCALE_8_TO_4_FLOOR[b];
        packet.Flags = (packet.Flags & 0xFFFF0000u) | ((uint)((rr << 9) | (gg << 4) | bb) << 1) | (1u << 15);
    }

    private static void SetColorB(ref PvrTcPacket packet, int r, int g, int b)
    {
        int rr = BITSCALE_8_TO_5_CEIL[r];
        int gg = BITSCALE_8_TO_5_CEIL[g];
        int bb = BITSCALE_8_TO_5_CEIL[b];
        packet.Flags = (packet.Flags & 0xFFFFu) | ((uint)((rr << 10) | (gg << 5) | bb) << 16) | (1u << 31);
    }

    private static void SetColorA(ref PvrTcPacket packet, int r, int g, int b, int a)
    {
        int aa = BITSCALE_8_TO_3_FLOOR[a];
        if (aa == 7)
        {
            int rr = BITSCALE_8_TO_5_FLOOR[r];
            int gg = BITSCALE_8_TO_5_FLOOR[g];
            int bb = BITSCALE_8_TO_4_FLOOR[b];
            packet.Flags = (packet.Flags & 0xFFFF0000u) | ((uint)((rr << 9) | (gg << 4) | bb) << 1) | (1u << 15);
        }
        else
        {
            int rr = BITSCALE_8_TO_4_FLOOR[r];
            int gg = BITSCALE_8_TO_4_FLOOR[g];
            int bb = BITSCALE_8_TO_3_FLOOR[b];
            packet.Flags = (packet.Flags & 0xFFFF0000u) | ((uint)((aa << 11) | (rr << 7) | (gg << 3) | bb) << 1);
        }
    }

    private static void SetColorB(ref PvrTcPacket packet, int r, int g, int b, int a)
    {
        int aa = BITSCALE_8_TO_3_CEIL[a];
        if (aa == 7)
        {
            int rr = BITSCALE_8_TO_5_CEIL[r];
            int gg = BITSCALE_8_TO_5_CEIL[g];
            int bb = BITSCALE_8_TO_5_CEIL[b];
            packet.Flags = (packet.Flags & 0xFFFFu) | ((uint)((rr << 10) | (gg << 5) | bb) << 16) | (1u << 31);
        }
        else
        {
            int rr = BITSCALE_8_TO_4_CEIL[r];
            int gg = BITSCALE_8_TO_4_CEIL[g];
            int bb = BITSCALE_8_TO_4_CEIL[b];
            packet.Flags = (packet.Flags & 0xFFFFu) | ((uint)((aa << 12) | (rr << 8) | (gg << 4) | bb) << 16);
        }
    }

    private static void CalculateBoundingBoxRgb(ReadOnlySpan<byte> data, int size, int blockX, int blockY, out byte minR, out byte minG, out byte minB, out byte maxR, out byte maxG, out byte maxB)
    {
        int basePixel = blockY * 4 * size + blockX * 4;
        int r = data[basePixel * 3];
        int g = data[basePixel * 3 + 1];
        int b = data[basePixel * 3 + 2];
        int mnR = r, mnG = g, mnB = b, mxR = r, mxG = g, mxB = b;
        for (int row = 0; row < 4; ++row)
        {
            for (int col = 0; col < 4; ++col)
            {
                int pixel = basePixel + row * size + col;
                if (col == 0 && row == 0)
                {
                    continue;
                }
                int pr = data[pixel * 3];
                int pg = data[pixel * 3 + 1];
                int pb = data[pixel * 3 + 2];
                if (pr < mnR) mnR = pr;
                if (pg < mnG) mnG = pg;
                if (pb < mnB) mnB = pb;
                if (pr > mxR) mxR = pr;
                if (pg > mxG) mxG = pg;
                if (pb > mxB) mxB = pb;
            }
        }
        minR = (byte)mnR;
        minG = (byte)mnG;
        minB = (byte)mnB;
        maxR = (byte)mxR;
        maxG = (byte)mxG;
        maxB = (byte)mxB;
    }

    private static void CalculateBoundingBoxRgba(ReadOnlySpan<byte> data, int size, int blockX, int blockY, out byte minR, out byte minG, out byte minB, out byte minA, out byte maxR, out byte maxG, out byte maxB, out byte maxA)
    {
        int basePixel = blockY * 4 * size + blockX * 4;
        int r = data[basePixel * 4];
        int g = data[basePixel * 4 + 1];
        int b = data[basePixel * 4 + 2];
        int a = data[basePixel * 4 + 3];
        int mnR = r, mnG = g, mnB = b, mnA = a, mxR = r, mxG = g, mxB = b, mxA = a;
        for (int row = 0; row < 4; ++row)
        {
            for (int col = 0; col < 4; ++col)
            {
                int pixel = basePixel + row * size + col;
                if (col == 0 && row == 0)
                {
                    continue;
                }
                int pr = data[pixel * 4];
                int pg = data[pixel * 4 + 1];
                int pb = data[pixel * 4 + 2];
                int pa = data[pixel * 4 + 3];
                if (pr < mnR) mnR = pr;
                if (pg < mnG) mnG = pg;
                if (pb < mnB) mnB = pb;
                if (pa < mnA) mnA = pa;
                if (pr > mxR) mxR = pr;
                if (pg > mxG) mxG = pg;
                if (pb > mxB) mxB = pb;
                if (pa > mxA) mxA = pa;
            }
        }
        minR = (byte)mnR;
        minG = (byte)mnG;
        minB = (byte)mnB;
        minA = (byte)mnA;
        maxR = (byte)mxR;
        maxG = (byte)mxG;
        maxB = (byte)mxB;
        maxA = (byte)mxA;
    }

    public static void DecodeRgba4Bpp(ReadOnlySpan<byte> compressed, int width, int height, Span<byte> rgbaOut)
    {
        int blocks = width / 4;
        int blockMask = blocks - 1;
        for (int y = 0; y < blocks; ++y)
        {
            for (int x = 0; x < blocks; ++x)
            {
                PvrTcPacket packet = ReadPacket(compressed, GetMortonNumber(x, y));
                uint mod = packet.ModulationData;
                int weightsBase = packet.UsePunchthroughAlpha * 16;
                int factor = 0;
                for (int py = 0; py < 4; ++py)
                {
                    int yOffset = py < 2 ? -1 : 0;
                    int y0 = (y + yOffset) & blockMask;
                    int y1 = (y0 + 1) & blockMask;
                    for (int px = 0; px < 4; ++px)
                    {
                        int xOffset = px < 2 ? -1 : 0;
                        int x0 = (x + xOffset) & blockMask;
                        int x1 = (x0 + 1) & blockMask;
                        PvrTcPacket p0 = ReadPacket(compressed, GetMortonNumber(x0, y0));
                        PvrTcPacket p1 = ReadPacket(compressed, GetMortonNumber(x1, y0));
                        PvrTcPacket p2 = ReadPacket(compressed, GetMortonNumber(x0, y1));
                        PvrTcPacket p3 = ReadPacket(compressed, GetMortonNumber(x1, y1));
                        (int r0, int g0, int b0, int a0) = GetColorRgbaA(p0);
                        (int r1, int g1, int b1, int a1) = GetColorRgbaA(p1);
                        (int r2, int g2, int b2, int a2) = GetColorRgbaA(p2);
                        (int r3, int g3, int b3, int a3) = GetColorRgbaA(p3);
                        (int r4, int g4, int b4, int a4) = GetColorRgbaB(p0);
                        (int r5, int g5, int b5, int a5) = GetColorRgbaB(p1);
                        (int r6, int g6, int b6, int a6) = GetColorRgbaB(p2);
                        (int r7, int g7, int b7, int a7) = GetColorRgbaB(p3);
                        int f0 = BILINEAR_FACTORS[factor * 4];
                        int f1 = BILINEAR_FACTORS[factor * 4 + 1];
                        int f2 = BILINEAR_FACTORS[factor * 4 + 2];
                        int f3 = BILINEAR_FACTORS[factor * 4 + 3];
                        int caR = r0 * f0 + r1 * f1 + r2 * f2 + r3 * f3;
                        int caG = g0 * f0 + g1 * f1 + g2 * f2 + g3 * f3;
                        int caB = b0 * f0 + b1 * f1 + b2 * f2 + b3 * f3;
                        int caA = a0 * f0 + a1 * f1 + a2 * f2 + a3 * f3;
                        int cbR = r4 * f0 + r5 * f1 + r6 * f2 + r7 * f3;
                        int cbG = g4 * f0 + g5 * f1 + g6 * f2 + g7 * f3;
                        int cbB = b4 * f0 + b5 * f1 + b6 * f2 + b7 * f3;
                        int cbA = a4 * f0 + a5 * f1 + a6 * f2 + a7 * f3;
                        int wBase = weightsBase + (int)(mod & 3) * 4;
                        int output = ((py + y * 4) * width + (px + x * 4)) * 4;
                        rgbaOut[output] = (byte)((caR * WEIGHTS[wBase] + cbR * WEIGHTS[wBase + 1]) >> 7);
                        rgbaOut[output + 1] = (byte)((caG * WEIGHTS[wBase] + cbG * WEIGHTS[wBase + 1]) >> 7);
                        rgbaOut[output + 2] = (byte)((caB * WEIGHTS[wBase] + cbB * WEIGHTS[wBase + 1]) >> 7);
                        rgbaOut[output + 3] = (byte)((caA * WEIGHTS[wBase + 2] + cbA * WEIGHTS[wBase + 3]) >> 7);
                        mod >>= 2;
                        ++factor;
                    }
                }
            }
        }
    }

    public static void DecodeRgb4Bpp(ReadOnlySpan<byte> compressed, int width, int height, Span<byte> rgbaOut)
    {
        int blocks = width / 4;
        int blockMask = blocks - 1;
        for (int y = 0; y < blocks; ++y)
        {
            for (int x = 0; x < blocks; ++x)
            {
                PvrTcPacket packet = ReadPacket(compressed, GetMortonNumber(x, y));
                uint mod = packet.ModulationData;
                int weightsBase = packet.UsePunchthroughAlpha * 16;
                int factor = 0;
                for (int py = 0; py < 4; ++py)
                {
                    int yOffset = py < 2 ? -1 : 0;
                    int y0 = (y + yOffset) & blockMask;
                    int y1 = (y0 + 1) & blockMask;
                    for (int px = 0; px < 4; ++px)
                    {
                        int xOffset = px < 2 ? -1 : 0;
                        int x0 = (x + xOffset) & blockMask;
                        int x1 = (x0 + 1) & blockMask;
                        PvrTcPacket p0 = ReadPacket(compressed, GetMortonNumber(x0, y0));
                        PvrTcPacket p1 = ReadPacket(compressed, GetMortonNumber(x1, y0));
                        PvrTcPacket p2 = ReadPacket(compressed, GetMortonNumber(x0, y1));
                        PvrTcPacket p3 = ReadPacket(compressed, GetMortonNumber(x1, y1));
                        (int r0, int g0, int b0) = GetColorRgbA(p0);
                        (int r1, int g1, int b1) = GetColorRgbA(p1);
                        (int r2, int g2, int b2) = GetColorRgbA(p2);
                        (int r3, int g3, int b3) = GetColorRgbA(p3);
                        (int r4, int g4, int b4) = GetColorRgbB(p0);
                        (int r5, int g5, int b5) = GetColorRgbB(p1);
                        (int r6, int g6, int b6) = GetColorRgbB(p2);
                        (int r7, int g7, int b7) = GetColorRgbB(p3);
                        int f0 = BILINEAR_FACTORS[factor * 4];
                        int f1 = BILINEAR_FACTORS[factor * 4 + 1];
                        int f2 = BILINEAR_FACTORS[factor * 4 + 2];
                        int f3 = BILINEAR_FACTORS[factor * 4 + 3];
                        int caR = r0 * f0 + r1 * f1 + r2 * f2 + r3 * f3;
                        int caG = g0 * f0 + g1 * f1 + g2 * f2 + g3 * f3;
                        int caB = b0 * f0 + b1 * f1 + b2 * f2 + b3 * f3;
                        int cbR = r4 * f0 + r5 * f1 + r6 * f2 + r7 * f3;
                        int cbG = g4 * f0 + g5 * f1 + g6 * f2 + g7 * f3;
                        int cbB = b4 * f0 + b5 * f1 + b6 * f2 + b7 * f3;
                        int wBase = weightsBase + (int)(mod & 3) * 4;
                        int output = ((py + y * 4) * width + (px + x * 4)) * 4;
                        rgbaOut[output] = (byte)((caR * WEIGHTS[wBase] + cbR * WEIGHTS[wBase + 1]) >> 7);
                        rgbaOut[output + 1] = (byte)((caG * WEIGHTS[wBase] + cbG * WEIGHTS[wBase + 1]) >> 7);
                        rgbaOut[output + 2] = (byte)((caB * WEIGHTS[wBase] + cbB * WEIGHTS[wBase + 1]) >> 7);
                        rgbaOut[output + 3] = 255;
                        mod >>= 2;
                        ++factor;
                    }
                }
            }
        }
    }

    public static void EncodeRgba4Bpp(ReadOnlySpan<byte> rgba, int width, int height, Span<byte> compressedOut)
    {
        int size = width;
        int blocks = size / 4;
        int blockMask = blocks - 1;
        PvrTcPacket[] packets = new PvrTcPacket[blocks * blocks];
        for (int y = 0; y < blocks; ++y)
        {
            for (int x = 0; x < blocks; ++x)
            {
                CalculateBoundingBoxRgba(rgba, size, x, y, out byte minR, out byte minG, out byte minB, out byte minA, out byte maxR, out byte maxG, out byte maxB, out byte maxA);
                PvrTcPacket packet = packets[GetMortonNumber(x, y)];
                packet.Flags &= ~1u;
                SetColorA(ref packet, minR, minG, minB, minA);
                SetColorB(ref packet, maxR, maxG, maxB, maxA);
                packets[GetMortonNumber(x, y)] = packet;
            }
        }
        for (int y = 0; y < blocks; ++y)
        {
            for (int x = 0; x < blocks; ++x)
            {
                int factor = 0;
                uint modulationData = 0;
                for (int py = 0; py < 4; ++py)
                {
                    int yOffset = py < 2 ? -1 : 0;
                    int y0 = (y + yOffset) & blockMask;
                    int y1 = (y0 + 1) & blockMask;
                    for (int px = 0; px < 4; ++px)
                    {
                        int xOffset = px < 2 ? -1 : 0;
                        int x0 = (x + xOffset) & blockMask;
                        int x1 = (x0 + 1) & blockMask;
                        PvrTcPacket p0 = packets[GetMortonNumber(x0, y0)];
                        PvrTcPacket p1 = packets[GetMortonNumber(x1, y0)];
                        PvrTcPacket p2 = packets[GetMortonNumber(x0, y1)];
                        PvrTcPacket p3 = packets[GetMortonNumber(x1, y1)];
                        (int r0, int g0, int b0, int a0) = GetColorRgbaA(p0);
                        (int r1, int g1, int b1, int a1) = GetColorRgbaA(p1);
                        (int r2, int g2, int b2, int a2) = GetColorRgbaA(p2);
                        (int r3, int g3, int b3, int a3) = GetColorRgbaA(p3);
                        (int r4, int g4, int b4, int a4) = GetColorRgbaB(p0);
                        (int r5, int g5, int b5, int a5) = GetColorRgbaB(p1);
                        (int r6, int g6, int b6, int a6) = GetColorRgbaB(p2);
                        (int r7, int g7, int b7, int a7) = GetColorRgbaB(p3);
                        int f0 = BILINEAR_FACTORS[factor * 4];
                        int f1 = BILINEAR_FACTORS[factor * 4 + 1];
                        int f2 = BILINEAR_FACTORS[factor * 4 + 2];
                        int f3 = BILINEAR_FACTORS[factor * 4 + 3];
                        int caR = r0 * f0 + r1 * f1 + r2 * f2 + r3 * f3;
                        int caG = g0 * f0 + g1 * f1 + g2 * f2 + g3 * f3;
                        int caB = b0 * f0 + b1 * f1 + b2 * f2 + b3 * f3;
                        int caA = a0 * f0 + a1 * f1 + a2 * f2 + a3 * f3;
                        int cbR = r4 * f0 + r5 * f1 + r6 * f2 + r7 * f3;
                        int cbG = g4 * f0 + g5 * f1 + g6 * f2 + g7 * f3;
                        int cbB = b4 * f0 + b5 * f1 + b6 * f2 + b7 * f3;
                        int cbA = a4 * f0 + a5 * f1 + a6 * f2 + a7 * f3;
                        int pixelOffset = ((y * 4 + py) * size + (x * 4 + px)) * 4;
                        int pixelR = rgba[pixelOffset];
                        int pixelG = rgba[pixelOffset + 1];
                        int pixelB = rgba[pixelOffset + 2];
                        int pixelA = rgba[pixelOffset + 3];
                        int dR = cbR - caR;
                        int dG = cbG - caG;
                        int dB = cbB - caB;
                        int dA = cbA - caA;
                        int pR = pixelR * 16;
                        int pG = pixelG * 16;
                        int pB = pixelB * 16;
                        int pA = pixelA * 16;
                        int vR = pR - caR;
                        int vG = pG - caG;
                        int vB = pB - caB;
                        int vA = pA - caA;
                        int projection = (vR * dR + vG * dG + vB * dB + vA * dA) * 16;
                        int lengthSquared = dR * dR + dG * dG + dB * dB + dA * dA;
                        if (projection > 3 * lengthSquared)
                        {
                            ++modulationData;
                        }
                        if (projection > 8 * lengthSquared)
                        {
                            ++modulationData;
                        }
                        if (projection > 13 * lengthSquared)
                        {
                            ++modulationData;
                        }
                        modulationData = RotateRight(modulationData, 2);
                        ++factor;
                    }
                }
                packets[GetMortonNumber(x, y)].ModulationData = modulationData;
            }
        }
        for (int i = 0; i < packets.Length; ++i)
        {
            WritePacket(compressedOut, i, packets[i]);
        }
    }

    public static void EncodeRgb4Bpp(ReadOnlySpan<byte> rgb, int width, int height, Span<byte> compressedOut)
    {
        int size = width;
        int blocks = size / 4;
        int blockMask = blocks - 1;
        PvrTcPacket[] packets = new PvrTcPacket[blocks * blocks];
        for (int y = 0; y < blocks; ++y)
        {
            for (int x = 0; x < blocks; ++x)
            {
                CalculateBoundingBoxRgb(rgb, size, x, y, out byte minR, out byte minG, out byte minB, out byte maxR, out byte maxG, out byte maxB);
                PvrTcPacket packet = packets[GetMortonNumber(x, y)];
                packet.Flags &= ~1u;
                SetColorA(ref packet, minR, minG, minB);
                SetColorB(ref packet, maxR, maxG, maxB);
                packets[GetMortonNumber(x, y)] = packet;
            }
        }
        for (int y = 0; y < blocks; ++y)
        {
            for (int x = 0; x < blocks; ++x)
            {
                int factor = 0;
                uint modulationData = 0;
                for (int py = 0; py < 4; ++py)
                {
                    int yOffset = py < 2 ? -1 : 0;
                    int y0 = (y + yOffset) & blockMask;
                    int y1 = (y0 + 1) & blockMask;
                    for (int px = 0; px < 4; ++px)
                    {
                        int xOffset = px < 2 ? -1 : 0;
                        int x0 = (x + xOffset) & blockMask;
                        int x1 = (x0 + 1) & blockMask;
                        PvrTcPacket p0 = packets[GetMortonNumber(x0, y0)];
                        PvrTcPacket p1 = packets[GetMortonNumber(x1, y0)];
                        PvrTcPacket p2 = packets[GetMortonNumber(x0, y1)];
                        PvrTcPacket p3 = packets[GetMortonNumber(x1, y1)];
                        (int r0, int g0, int b0) = GetColorRgbA(p0);
                        (int r1, int g1, int b1) = GetColorRgbA(p1);
                        (int r2, int g2, int b2) = GetColorRgbA(p2);
                        (int r3, int g3, int b3) = GetColorRgbA(p3);
                        (int r4, int g4, int b4) = GetColorRgbB(p0);
                        (int r5, int g5, int b5) = GetColorRgbB(p1);
                        (int r6, int g6, int b6) = GetColorRgbB(p2);
                        (int r7, int g7, int b7) = GetColorRgbB(p3);
                        int f0 = BILINEAR_FACTORS[factor * 4];
                        int f1 = BILINEAR_FACTORS[factor * 4 + 1];
                        int f2 = BILINEAR_FACTORS[factor * 4 + 2];
                        int f3 = BILINEAR_FACTORS[factor * 4 + 3];
                        int caR = r0 * f0 + r1 * f1 + r2 * f2 + r3 * f3;
                        int caG = g0 * f0 + g1 * f1 + g2 * f2 + g3 * f3;
                        int caB = b0 * f0 + b1 * f1 + b2 * f2 + b3 * f3;
                        int cbR = r4 * f0 + r5 * f1 + r6 * f2 + r7 * f3;
                        int cbG = g4 * f0 + g5 * f1 + g6 * f2 + g7 * f3;
                        int cbB = b4 * f0 + b5 * f1 + b6 * f2 + b7 * f3;
                        int pixelOffset = ((y * 4 + py) * size + (x * 4 + px)) * 3;
                        int pixelR = rgb[pixelOffset];
                        int pixelG = rgb[pixelOffset + 1];
                        int pixelB = rgb[pixelOffset + 2];
                        int dR = cbR - caR;
                        int dG = cbG - caG;
                        int dB = cbB - caB;
                        int pR = pixelR * 16;
                        int pG = pixelG * 16;
                        int pB = pixelB * 16;
                        int vR = pR - caR;
                        int vG = pG - caG;
                        int vB = pB - caB;
                        int projection = (vR * dR + vG * dG + vB * dB) * 16;
                        int lengthSquared = dR * dR + dG * dG + dB * dB;
                        if (projection > 3 * lengthSquared)
                        {
                            ++modulationData;
                        }
                        if (projection > 8 * lengthSquared)
                        {
                            ++modulationData;
                        }
                        if (projection > 13 * lengthSquared)
                        {
                            ++modulationData;
                        }
                        modulationData = RotateRight(modulationData, 2);
                        ++factor;
                    }
                }
                packets[GetMortonNumber(x, y)].ModulationData = modulationData;
            }
        }
        for (int i = 0; i < packets.Length; ++i)
        {
            WritePacket(compressedOut, i, packets[i]);
        }
    }
}
