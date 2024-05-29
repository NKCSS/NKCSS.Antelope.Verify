using Cryptography.ECDSA;
using System.Text;

namespace NKCSS.Antelope

{
    public class PublicKey
    {
        const int CheckSumLength = 4;
        const string NewKeyType = "K1";
        public const string NewPrefix = "PUB_" + NewKeyType + "_";
        public const string OldPrefix = "EOS";
        byte[] bytes, bytesNoCheckSum, checkSum, newCheckSum, oldCheckSum;
        string oldKey, newKey;
        public string OldKey { get => oldKey; }
        public string NewKey { get => newKey; }
        /// <summary>
        /// Key Bytes without checksum
        /// </summary>
        public byte[] KeyBytes() => bytesNoCheckSum.ToArray();
        public PublicKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key was not specified", nameof(key));
            if (key.StartsWith(NewPrefix)) bytes = Base58.Decode(key.Substring(NewPrefix.Length));
            else if (key.StartsWith(OldPrefix)) bytes = Base58.Decode(key.Substring(OldPrefix.Length));
            else throw new ArgumentException($"Unknown key prefix used in '{key}'", nameof(key));
            bytesNoCheckSum = bytes.Take(bytes.Length - CheckSumLength).ToArray();
            checkSum = bytes.Skip(bytesNoCheckSum.Length).ToArray();
            oldCheckSum = Ripemd160Manager.GetHash(bytesNoCheckSum).Take(CheckSumLength).ToArray();
            newCheckSum = Ripemd160Manager.GetHash(bytesNoCheckSum.Concat(Encoding.UTF8.GetBytes(NewKeyType)).ToArray()).Take(CheckSumLength).ToArray();
            oldKey = $"{OldPrefix}{Base58.Encode(bytesNoCheckSum.Concat(oldCheckSum).ToArray())}";
            newKey = $"{NewPrefix}{Base58.Encode(bytesNoCheckSum.Concat(newCheckSum).ToArray())}";
            if (key != oldKey && key != newKey) throw new ArgumentException($"Supplied key ({key}) doesn't match expected old or new style; is your checksum wrong? Old: {oldKey}, New: {newKey}", nameof(key));
        }
        public static implicit operator PublicKey(string value) => new PublicKey(value);
    }
}