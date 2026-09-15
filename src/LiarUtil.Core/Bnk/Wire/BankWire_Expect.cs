using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Bnk;

internal static class BankWireExpect
{
    extension(BufferReader reader)
    {
        public void ExpectConstUInt8(byte expected)
        {
            var actual = reader.ReadUInt8();
            if (actual != expected)
            {
                throw BnkException.Invalid(LiarUtil.Core.Strings.Constant, reader.Position - 1, string.Format(LiarUtil.Core.Strings.Expected01, expected, actual));
            }
        }

        public void ExpectConstUInt16(ushort expected)
        {
            var actual = reader.ReadUInt16();
            if (actual != expected)
            {
                throw BnkException.Invalid(LiarUtil.Core.Strings.Constant, reader.Position - 2, string.Format(LiarUtil.Core.Strings.Expected01, expected, actual));
            }
        }

        public void ExpectConstUInt32(uint expected)
        {
            var actual = reader.ReadUInt32();
            if (actual != expected)
            {
                throw BnkException.Invalid(LiarUtil.Core.Strings.Constant, reader.Position - 4, string.Format(LiarUtil.Core.Strings.Expected01, expected, actual));
            }
        }
    }
}
