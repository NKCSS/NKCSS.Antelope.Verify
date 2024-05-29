namespace NKCSS.Antelope.Verify
{
    public static class Extensions
    {
        #region Name-based constants
        const int NameMaxCharLength = 13;
        /// <summary>
        /// The amount of bits an Antelope name is encoded into.
        /// </summary>
        const int NameBitLength = 64;
        /// <summary>
        /// The amount of bits we can use per character.
        /// </summary>
        const int BitsPerNameValue = 5;
        /// <summary>
        /// The amount of charcters that can use the full bit length we need (12)
        /// </summary>
        const int NameFullBitCharCount = NameBitLength / BitsPerNameValue;
        /// <summary>
        /// The amount of bits that can have the full-length (60 in our case)
        /// </summary>
        const int NameBitsWithFullBitLength = NameFullBitCharCount * BitsPerNameValue;
        /// <summary>
        /// The amount of bits that remain for the last value (4)
        /// </summary>
        const int NameRestBits = NameBitLength - NameBitsWithFullBitLength;
        /// <summary>
        /// The last bit index that has <see cref="BitsPerNameValue"/> bits per encoded character.
        /// </summary>
        /// <remarks>
        /// Indexes are 0-based, so we take the amount of bits that are full-length values and substract 1.
        /// </remarks>
        const int LastFullLengthNameBitIndex = NameBitsWithFullBitLength - 1;
        /// <summary>
        /// The bitmask we use to extract the bits from the value. We shift by the bit length (e.g. overshoot), 
        /// then substract 1 to get a full set of binary 1 flags for our desired bit length.
        /// </summary>
        const int NameValueBitMask = (1 << BitsPerNameValue) - 1;
        /// <summary>
        /// The bitmask we use to extract the bits from the value. We shift by the bit length (e.g. overshoot), 
        /// then substract 1 to get a full set of binary 1 flags for our desired bit length.
        /// </summary>
        const int NameRestBitMask = (1 << NameRestBits) - 1;
        #endregion
        static Dictionary<char, byte> CharByteLookup;
        static Dictionary<byte, char> ByteCharLookup;
        static Extensions()
        {
            CharByteLookup = new Dictionary<char, byte>();
            ByteCharLookup = new Dictionary<byte, char>();
            CharByteLookup.Add('.', 0);
            ByteCharLookup.Add(0, '.');
            for (byte i = 1; i <= 5; ++i)
            {
                CharByteLookup.Add(i.ToString()[0], i);
                ByteCharLookup.Add(i, i.ToString()[0]);
            }
            byte offset = 'a' - 6;
            for (char c = 'a'; c <= 'z'; ++c)
            {
                CharByteLookup.Add(c, (byte)((byte)c - offset));
                ByteCharLookup.Add((byte)((byte)c - offset), c);
            }
        }
        public static string ToName(this ulong value)
        {
            char[] result = new char[NameMaxCharLength];
            byte v;
            char c;
            int resultIndex = 0;
            // The first 60 bits are 5-bits per value; 
            for (int i = 0; i < NameBitsWithFullBitLength; i += BitsPerNameValue)
            {
                v = (byte)((value >> LastFullLengthNameBitIndex - i) & NameValueBitMask);
                c = ByteCharLookup[v];
                result[resultIndex++] = c;
            }
            v = (byte)(value & NameRestBitMask);
            c = ByteCharLookup[v];
            result[resultIndex] = c;
            // Strip any trailing 0-values (e.g. '.')
            return new string(result).TrimEnd(ByteCharLookup[0]);
        }
        public static ulong NameToLong(this string name)
        {
            ulong result = 0L;
            int bitIndex = 0, i;
            byte c;
            // Process the full-bit-length characters
            for (i = 0; i < NameFullBitCharCount; i++)
            {
                c = i < name.Length ? CharByteLookup[name[i]] : (byte)0;
                if ((c & 0b00001) == 0b00001) result += 1UL << (59 - bitIndex);
                if ((c & 0b00010) == 0b00010) result += 1UL << (60 - bitIndex);
                if ((c & 0b00100) == 0b00100) result += 1UL << (61 - bitIndex);
                if ((c & 0b01000) == 0b01000) result += 1UL << (62 - bitIndex);
                if ((c & 0b10000) == 0b10000) result += 1UL << (63 - bitIndex);
                bitIndex += 5;
            }
            // Process the last 4 bits
            c = i < name.Length ? CharByteLookup[name[i]] : (byte)0;
            if ((c & 0b0001) == 0b0001) result += 1UL;
            if ((c & 0b0010) == 0b0010) result += 1UL << 1;
            if ((c & 0b0100) == 0b0100) result += 1UL << 2;
            if ((c & 0b1000) == 0b1000) result += 1UL << 3;
            return result;
        }
    }
}
