namespace LiarUtil.Core.Core.Binary;

public static class BufferWriterExtensions
{
    extension(BufferWriter writer)
    {
        public void WriteOptionalSingle(float? value)
        {
            writer.WriteInt32(value.HasValue ? 1 : 0);
            if (value.HasValue)
            {
                writer.WriteSingle(value.Value);
            }
        }

        public void WriteOptionalDouble(double? value)
        {
            writer.WriteInt32(value.HasValue ? 1 : 0);
            if (value.HasValue)
            {
                writer.WriteDouble(value.Value);
            }
        }

        public void WriteOptionalInt32(int? value)
        {
            writer.WriteInt32(value.HasValue ? 1 : 0);
            if (value.HasValue)
            {
                writer.WriteInt32(value.Value);
            }
        }

        public void WriteInt32Array(ReadOnlySpan<int> values)
        {
            foreach (var value in values)
            {
                writer.WriteInt32(value);
            }
        }

        public void WriteSingleArray(ReadOnlySpan<float> values)
        {
            foreach (var value in values)
            {
                writer.WriteSingle(value);
            }
        }
    }
}
