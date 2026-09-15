using System.Buffers.Binary;

namespace LiarUtil.Core.Core.Crypto;

public sealed class Cipher
{
    internal const int MaxRounds = 14;
    internal const int MaxWords = 8;

    public const int MinKeyBytes = 16;
    public const int MaxKeyBytes = 32;
    public const int MinBlockBytes = 16;
    public const int MaxBlockBytes = 32;
    public const int DefaultBlockBytes = 16;

    private const string MsgInvalidKeyLength = "invalid key length (16/24/32 bytes required)";
    private const string MsgInvalidBlockSize = "invalid block size (16/24/32 bytes required)";
    private const string MsgInvalidIvLength = "invalid IV length (must equal block size)";
    private const string MsgBadMode = "unknown operation mode";
    private const string MsgBadLength = "data length must be a non-zero multiple of block size";
    private const string MsgShortBuffer = "input/output shorter than one block";

    public int KeyLength { get; private set => field = value is 16 or 24 or 32
        ? value : throw new ArgumentException(MsgInvalidKeyLength, nameof(KeyLength)); }
    public int BlockSize { get; private set => field = value is 16 or 24 or 32
        ? value : throw new ArgumentException(MsgInvalidBlockSize, nameof(BlockSize)); }
    public int Rounds { get; private set => field = value; }

    private readonly uint[] ke = new uint[(MaxRounds + 1) * MaxWords];
    private readonly uint[] kd = new uint[(MaxRounds + 1) * MaxWords];
    private readonly byte[] chain0 = new byte[MaxBlockBytes];
    private readonly byte[] chain = new byte[MaxBlockBytes];

    public static Cipher Make(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, int blockSize = DefaultBlockBytes)
    {
        if (key.Length is not (16 or 24 or 32))
            throw new ArgumentException(MsgInvalidKeyLength);
        if (blockSize is not (16 or 24 or 32))
            throw new ArgumentException(MsgInvalidBlockSize);
        if (iv.Length != blockSize)
            throw new ArgumentException(MsgInvalidIvLength);
        var c = new Cipher
        {
            KeyLength = key.Length,
            BlockSize = blockSize,
        };
        c.ExpandKey(key, iv);
        return c;
    }

    public void ResetChain() => chain0.AsSpan(0, BlockSize).CopyTo(chain);

    public void EncryptBlock(ReadOnlySpan<byte> input, Span<byte> output)
    {
        if (input.Length < BlockSize || output.Length < BlockSize)
            throw new ArgumentException(MsgShortBuffer);
        int bc = BlockSize / 4;
        var (s1, s2, s3) = ShiftOffsets(bc, encrypt: true);
        Span<uint> t = stackalloc uint[MaxWords];
        Span<uint> a = stackalloc uint[MaxWords];
        for (int i = 0; i < bc; i++)
            t[i] = BinaryPrimitives.ReadUInt32BigEndian(input.Slice(i * 4, 4)) ^ Ke(0, i);
        for (int r = 1; r < Rounds; r++)
        {
            for (int i = 0; i < bc; i++)
                a[i] = Tables.T1[(t[i] >> 24) & 0xFF] ^ Tables.T2[(t[(i + s1) % bc] >> 16) & 0xFF]
                     ^ Tables.T3[(t[(i + s2) % bc] >> 8) & 0xFF] ^ Tables.T4[t[(i + s3) % bc] & 0xFF] ^ Ke(r, i);
            Span<uint> tmp = t;
            t = a;
            a = tmp;
        }
        for (int i = 0; i < bc; i++)
        {
            uint k = Ke(Rounds, i);
            output[i * 4 + 0] = (byte)(Tables.SBox[(t[i] >> 24) & 0xFF] ^ (byte)(k >> 24));
            output[i * 4 + 1] = (byte)(Tables.SBox[(t[(i + s1) % bc] >> 16) & 0xFF] ^ (byte)(k >> 16));
            output[i * 4 + 2] = (byte)(Tables.SBox[(t[(i + s2) % bc] >> 8) & 0xFF] ^ (byte)(k >> 8));
            output[i * 4 + 3] = (byte)(Tables.SBox[t[(i + s3) % bc] & 0xFF] ^ (byte)k);
        }
    }

    public void DecryptBlock(ReadOnlySpan<byte> input, Span<byte> output)
    {
        if (input.Length < BlockSize || output.Length < BlockSize)
            throw new ArgumentException(MsgShortBuffer);
        int bc = BlockSize / 4;
        var (s1, s2, s3) = ShiftOffsets(bc, encrypt: false);
        Span<uint> t = stackalloc uint[MaxWords];
        Span<uint> a = stackalloc uint[MaxWords];
        for (int i = 0; i < bc; i++)
            t[i] = BinaryPrimitives.ReadUInt32BigEndian(input.Slice(i * 4, 4)) ^ Kd(0, i);
        for (int r = 1; r < Rounds; r++)
        {
            for (int i = 0; i < bc; i++)
                a[i] = Tables.T5[(t[i] >> 24) & 0xFF] ^ Tables.T6[(t[(i + s1) % bc] >> 16) & 0xFF]
                     ^ Tables.T7[(t[(i + s2) % bc] >> 8) & 0xFF] ^ Tables.T8[t[(i + s3) % bc] & 0xFF] ^ Kd(r, i);
            Span<uint> tmp = t;
            t = a;
            a = tmp;
        }
        for (int i = 0; i < bc; i++)
        {
            uint k = Kd(Rounds, i);
            output[i * 4 + 0] = (byte)(Tables.InvSBox[(t[i] >> 24) & 0xFF] ^ (byte)(k >> 24));
            output[i * 4 + 1] = (byte)(Tables.InvSBox[(t[(i + s1) % bc] >> 16) & 0xFF] ^ (byte)(k >> 16));
            output[i * 4 + 2] = (byte)(Tables.InvSBox[(t[(i + s2) % bc] >> 8) & 0xFF] ^ (byte)(k >> 8));
            output[i * 4 + 3] = (byte)(Tables.InvSBox[t[(i + s3) % bc] & 0xFF] ^ (byte)k);
        }
    }

    public void Encrypt(ReadOnlySpan<byte> input, Span<byte> output, BlockCipherMode mode = BlockCipherMode.Ecb)
    {
        CheckInput(input, output);
        if (!mode.IsValid)
            throw new ArgumentException(MsgBadMode);
        int bs = BlockSize;
        switch (mode)
        {
            case BlockCipherMode.Cbc:
                for (int i = 0; i < input.Length; i += bs)
                {
                    for (int k = 0; k < bs; k++)
                        chain[k] ^= input[i + k];
                    EncryptBlock(chain, output.Slice(i, bs));
                    output.Slice(i, bs).CopyTo(chain);
                }
                break;
            case BlockCipherMode.Cfb:
                for (int i = 0; i < input.Length; i += bs)
                {
                    var outBlk = output.Slice(i, bs);
                    EncryptBlock(chain, outBlk);
                    for (int k = 0; k < bs; k++)
                        outBlk[k] ^= input[i + k];
                    outBlk.CopyTo(chain);
                }
                break;
            default:
                for (int i = 0; i < input.Length; i += bs)
                    EncryptBlock(input.Slice(i, bs), output.Slice(i, bs));
                break;
        }
    }

    public void Decrypt(ReadOnlySpan<byte> input, Span<byte> output, BlockCipherMode mode = BlockCipherMode.Ecb)
    {
        CheckInput(input, output);
        if (!mode.IsValid)
            throw new ArgumentException(MsgBadMode);
        int bs = BlockSize;
        switch (mode)
        {
            case BlockCipherMode.Cbc:
                for (int i = 0; i < input.Length; i += bs)
                {
                    var inBlk = input.Slice(i, bs);
                    var outBlk = output.Slice(i, bs);
                    DecryptBlock(inBlk, outBlk);
                    for (int k = 0; k < bs; k++)
                        outBlk[k] ^= chain[k];
                    inBlk.CopyTo(chain);
                }
                break;
            case BlockCipherMode.Cfb:
                for (int i = 0; i < input.Length; i += bs)
                {
                    var inBlk = input.Slice(i, bs);
                    var outBlk = output.Slice(i, bs);
                    EncryptBlock(chain, outBlk);
                    for (int k = 0; k < bs; k++)
                        outBlk[k] ^= inBlk[k];
                    inBlk.CopyTo(chain);
                }
                break;
            default:
                for (int i = 0; i < input.Length; i += bs)
                    DecryptBlock(input.Slice(i, bs), output.Slice(i, bs));
                break;
        }
    }

    private void CheckInput(ReadOnlySpan<byte> input, ReadOnlySpan<byte> output)
    {
        if (input.IsEmpty || input.Length % BlockSize != 0 || input.Length != output.Length)
            throw new ArgumentException(MsgBadLength);
    }

    private uint Ke(int r, int c) => ke[r * MaxWords + c];
    private uint Kd(int r, int c) => kd[r * MaxWords + c];

    private void ExpandKey(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv)
    {
        iv.CopyTo(chain0);
        iv.CopyTo(chain);

        Rounds = KeyLength switch
        {
            16 => BlockSize switch { 16 => 10, 24 => 12, _ => 14 },
            24 => BlockSize != 32 ? 12 : 14,
            _ => 14,
        };

        int bc = BlockSize / 4;
        int kc = KeyLength / 4;
        int roundKeyCount = (Rounds + 1) * bc;

        var tk = new uint[MaxWords];
        for (int i = 0; i < kc; i++)
            tk[i] = BinaryPrimitives.ReadUInt32BigEndian(key.Slice(i * 4, 4));

        int t = 0;
        void Put(int j)
        {
            ke[t / bc * MaxWords + t % bc] = tk[j];
            kd[(Rounds - t / bc) * MaxWords + t % bc] = tk[j];
            t++;
        }
        for (int j = 0; j < kc && t < roundKeyCount; j++)
            Put(j);

        int rconPointer = 0;
        while (t < roundKeyCount)
        {
            uint tt = tk[kc - 1];
            tk[0] ^= (uint)Tables.SBox[(tt >> 16) & 0xFF] << 24
                   ^ (uint)Tables.SBox[(tt >> 8) & 0xFF] << 16
                   ^ (uint)Tables.SBox[tt & 0xFF] << 8
                   ^ Tables.SBox[(tt >> 24) & 0xFF]
                   ^ (uint)Tables.Rcon[rconPointer++] << 24;
            if (kc != 8)
            {
                for (int i = 1; i < kc; i++)
                    tk[i] ^= tk[i - 1];
            }
            else
            {
                for (int i = 1; i < kc / 2; i++)
                    tk[i] ^= tk[i - 1];
                uint tt2 = tk[kc / 2 - 1];
                tk[kc / 2] ^= Tables.SBox[tt2 & 0xFF]
                            ^ (uint)Tables.SBox[(tt2 >> 8) & 0xFF] << 8
                            ^ (uint)Tables.SBox[(tt2 >> 16) & 0xFF] << 16
                            ^ (uint)Tables.SBox[(tt2 >> 24) & 0xFF] << 24;
                for (int i = kc / 2 + 1; i < kc; i++)
                    tk[i] ^= tk[i - 1];
            }
            for (int j = 0; j < kc && t < roundKeyCount; j++)
                Put(j);
        }

        for (int r = 1; r < Rounds; r++)
            for (int j = 0; j < bc; j++)
            {
                uint x = kd[r * MaxWords + j];
                kd[r * MaxWords + j] = Tables.U1[(x >> 24) & 0xFF] ^ Tables.U2[(x >> 16) & 0xFF]
                                     ^ Tables.U3[(x >> 8) & 0xFF] ^ Tables.U4[x & 0xFF];
            }
    }

    private static (int s1, int s2, int s3) ShiftOffsets(int bc, bool encrypt)
    {
        int sc = bc switch { 4 => 0, 6 => 1, _ => 2 };
        int col = encrypt ? 0 : 1;
        int baseIdx = sc * 8 + col;
        return (Tables.Shifts[baseIdx + 2], Tables.Shifts[baseIdx + 4], Tables.Shifts[baseIdx + 6]);
    }
}
