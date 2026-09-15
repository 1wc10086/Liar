namespace LiarUtil.Core.Core.Binary;

public static class BufferReaderExtensions
{
    extension(BufferReader reader)
    {
        public float? ReadOptionalSingle() => reader.ReadInt32() != 0 ? reader.ReadSingle() : null;

        public double? ReadOptionalDouble() => reader.ReadInt32() != 0 ? reader.ReadDouble() : null;

        public int? ReadOptionalInt32() => reader.ReadInt32() != 0 ? reader.ReadInt32() : null;

        public string? ReadOptionalStringByInt32Head()
        {
            var present = reader.ReadInt32();
            return present == 0 ? null : reader.ReadStringByInt32Head();
        }

        public string ReadStringOrEmptyByInt32Head()
        {
            var length = reader.ReadInt32();
            return length > 0 ? reader.ReadString(length)! : "";
        }

        public string ReadStringOrEmptyByVarInt32Head()
        {
            var length = reader.ReadVarInt32();
            return length > 0 ? reader.ReadString(length)! : "";
        }

        public string ReadStringOrEmptyByUInt16Head()
        {
            var length = reader.ReadUInt16();
            return length > 0 ? reader.ReadString(length)! : "";
        }

        public int[] ReadInt32Array(int count)
        {
            var result = new int[count];
            for (var index = 0; index < count; index++)
            {
                result[index] = reader.ReadInt32();
            }

            return result;
        }

        public float[] ReadSingleArray(int count)
        {
            var result = new float[count];
            for (var index = 0; index < count; index++)
            {
                result[index] = reader.ReadSingle();
            }

            return result;
        }
    }
}
