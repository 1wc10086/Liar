namespace LiarUtil.Core.Core.Crypto;

internal static partial class Tables
{
    internal static readonly uint[] T1 = new uint[256];
    internal static readonly uint[] T2 = new uint[256];
    internal static readonly uint[] T3 = new uint[256];
    internal static readonly uint[] T4 = new uint[256];
    internal static readonly uint[] T5 = new uint[256];
    internal static readonly uint[] T6 = new uint[256];
    internal static readonly uint[] T7 = new uint[256];
    internal static readonly uint[] T8 = new uint[256];
    internal static readonly uint[] U1 = new uint[256];
    internal static readonly uint[] U2 = new uint[256];
    internal static readonly uint[] U3 = new uint[256];
    internal static readonly uint[] U4 = new uint[256];

    static Tables()
    {
        for (int i = 0; i < 256; i++)
        {
            byte x = (byte)i;
            T1[i] = T4x(SBox[i], 2, 1, 1, 3);
            T2[i] = T4x(SBox[i], 3, 2, 1, 1);
            T3[i] = T4x(SBox[i], 1, 3, 2, 1);
            T4[i] = T4x(SBox[i], 1, 1, 3, 2);
            T5[i] = T4x(InvSBox[i], 14, 9, 13, 11);
            T6[i] = T4x(InvSBox[i], 11, 14, 9, 13);
            T7[i] = T4x(InvSBox[i], 13, 11, 14, 9);
            T8[i] = T4x(InvSBox[i], 9, 13, 11, 14);
            U1[i] = T4x(x, 14, 9, 13, 11);
            U2[i] = T4x(x, 11, 14, 9, 13);
            U3[i] = T4x(x, 13, 11, 14, 9);
            U4[i] = T4x(x, 9, 13, 11, 14);
        }
        Check(T1[0] == 0xC66363A5, "t1[0]");
        Check(T1[255] == 0x2C16163A, "t1[255]");
        Check(T2[1] == 0x84F87C7C, "t2[1]");
        Check(T4[7] == 0xC5C55491, "t4[7]");
        Check(T5[0] == 0x51F4A750, "t5[0]");
        Check(T6[3] == 0x963A275E, "t6[3]");
        Check(T8[255] == 0xB85742D0, "t8[255]");
        Check(U1[0] == 0, "u1[0]");
        Check(U2[12] == 0x74486C5C, "u2[12]");
        Check(U4[199] == 0xA59430C6, "u4[199]");
        Check(SBox[0] == 0x63, "sbox[0]");
        Check(InvSBox[0] == 0x52, "invSbox[0]");
        Check(Log[255] == 0x07, "log[255]");
        Check(ALog[254] == 0xF6, "alog[254]");
        Check(Rcon[7] == 0x80 && Rcon[18] == 0x63 && Rcon[29] == 0x91, "rcon");
    }

    private static uint T4x(byte b, byte c0, byte c1, byte c2, byte c3) =>
        (uint)GfMul(b, c0) << 24 ^ (uint)GfMul(b, c1) << 16
      ^ (uint)GfMul(b, c2) << 8 ^ GfMul(b, c3);

    private static byte GfMul(byte a, byte b)
    {
        if (a != 0 && b != 0)
            return ALog[(Log[a] + Log[b]) % 255];
        return 0;
    }

    private static void Check(bool ok, string what)
    {
        if (!ok)
            throw new InvalidOperationException("rijndael: table derivation mismatch at " + what);
    }
}
