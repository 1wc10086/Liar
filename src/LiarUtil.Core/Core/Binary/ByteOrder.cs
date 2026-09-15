namespace LiarUtil.Core.Core.Binary;

public enum ByteOrder
{
    Little = 0,
    Big = 1,
}

public static class ByteOrderExtensions
{
    extension(ByteOrder order)
    {
        public bool IsBigEndian => order is ByteOrder.Big;

        public ByteOrder Flipped => order is ByteOrder.Big ? ByteOrder.Little : ByteOrder.Big;

        public string DisplayName => order is ByteOrder.Big ? "BigEndian" : "LittleEndian";
    }
}
