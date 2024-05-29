namespace NKCSS.Antelope
{
    public static class VariableLengthInteger
    {
        const int BitsPerByte = 8;
        const int DataBits = BitsPerByte - 1;
        const int DataBitMask = (1 << DataBits) - 1;
        const int ContinuationBit = 1 << DataBits;
        public static void EncodeInt32(this BinaryWriter writer, int value)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(value)} must be 0 or greater");
            foreach (byte val in value.EncodeInt32())
                writer.Write(val);
        }
        public static IEnumerable<byte> EncodeInt32(this uint value)
        {
            do
            {
                // Grab the lowest 7-bits of the value
                byte lower7bits = (byte)(value & DataBitMask);
                // Then shift the value by 7 and check if there is any value left.
                value >>= DataBits;
                if (value > 0) // If anything remains, ensure the continuation bit is set by OR-ing with 10000000 (1 + 7 bits from the data)
                    lower7bits |= ContinuationBit;
                yield return lower7bits;
            } while (value > 0);
        }
        public static IEnumerable<byte> EncodeInt32(this int value)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(value)} must be 0 or greater");
            foreach (var b in EncodeInt32((uint)value))
                yield return b;
        }
        public static int DecodeInt32(this BinaryReader reader)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            bool more = true;
            int value = 0, shift = 0;
            while (more)
            {
                byte lower7bits = reader.ReadByte();
                more = (lower7bits & ContinuationBit) != 0;
                value |= (lower7bits & DataBitMask) << shift;
                shift += DataBits;
            }
            return value;
        }
        public static int DecodeInt32(this IEnumerable<byte> bytes, bool breakOnNoMore = false)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            bool more = true;
            int value = 0, shift = 0;
            foreach (byte lower7bits in bytes)
            {
                more = (lower7bits & ContinuationBit) != 0;
                value |= (lower7bits & DataBitMask) << shift;
                shift += DataBits;
                if (breakOnNoMore && !more) break;
            }
            if (more) throw new ArgumentException("Last byte still had the 'more' flag set!", nameof(bytes));
            return value;
        }
        public static int DecodeInt32(this MemoryStream ms)
        {
            if (ms == null) throw new ArgumentNullException(nameof(ms));
            bool more = true;
            int value = 0, shift = 0;
            while (more)
            {
                byte lower7bits = (byte)ms.ReadByte();
                more = (lower7bits & ContinuationBit) != 0;
                value |= (lower7bits & DataBitMask) << shift;
                shift += DataBits;
            }
            return value;
        }
    }
}