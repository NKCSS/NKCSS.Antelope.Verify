using Cryptography.ECDSA;

namespace NKCSS.Antelope.Verify.Anchor
{
    /// <summary>
    /// Anchor wallet signs a fake verification transaction that you can validate on the backend.
    /// This is part of the identity proof action, so the user won't have an additional interaction,
    /// similar to the <see cref="MyCloudWallet.TransparantVerifyMessage"/> except Anchor uses a full
    /// transaction where <see cref="MyCloudWallet.TransparantVerifyMessage" /> uses a custom string to sign.
    /// </summary>
    public class VerifyMessage
    {
        /// <summary>
        /// The fake verification action the Anchor Wallet signs, has an empty contract name, 
        /// so the value you see below is actually correct.
        /// </summary>
        const string ContractName = "";
        /// <summary>
        /// The name of the action that is signed to prove the identity proof.
        /// </summary>
        const string ActionName = "identity";
        /// <summary>
        /// Data length is 25 bytes;
        /// scope (UInt64: 8) + 1 for the length byte, wallet (UInt64: 8) + permission (UInt64: 8).
        /// So, 3 UInt64's (8 bytes each) = 1 == 25
        /// </summary>
        const int ActionDataLength = 25;
        /// <summary>
        /// When a Transaction signature is created, the signing data ends with <see cref="TrailingEmptyByteCount"/> 0-bytes
        /// </summary>
        const int TrailingEmptyByteCount = 32;
        // Cache the UInt64 reprsentations of the Contract and Action name since they never change.
        static readonly UInt64 ContractNameValue = ContractName.NameToLong();
        static readonly UInt64 ActionNameValue = ActionName.NameToLong();
        /// <summary>
        /// Transactions that are signed end with <see cref="TrailingEmptyByteCount"/> 0-bytes
        /// </summary>
        static readonly byte[] TrailingEmptyBytes = new byte[TrailingEmptyByteCount];
        /// <summary>
        /// The public Key used to sign the verfification transaction
        /// </summary>
        public string publickey { get; set; }
        /// <summary>
        /// The wallet's used permission (e.g. active)
        /// </summary>
        public string permission { get; set; }
        /// <summary>
        /// The expiration time that was set for the transaction
        /// </summary>
        public uint expiration { get; set; }
        /// <summary>
        /// The scope of the transaction
        /// </summary>
        public string scope { get; set; }
        /// <summary>
        /// The signature that we need to validate
        /// </summary>
        public string signature { get; set; }
        /// <summary>
        /// The wallet that we try to identify
        /// </summary>
        public string wallet { get; set; }
        /// <summary>
        /// Overload for <see cref="SerializeData(byte[])"/> that convers the hex string into bytes.
        /// </summary>
        /// <param name="chainIdHex">The chain id the verification was intended for</param>
        /// <returns>The bytes that should be used to validate the <see cref="signature"/></returns>
        public byte[] SerializeData(string chainIdHex) => SerializeData(Hex.HexToBytes(chainIdHex));
        /// <summary>
        /// Reproduces the data that was signed to produce the <see cref="signature"/>.
        /// </summary>
        /// <param name="chainId">The decoded bytes of the ChainId this message belongs to</param>
        /// <returns>The bytes that should be used to validate the <see cref="signature"/></returns>
        public byte[] SerializeData(byte[] chainId)
        {
            byte[] data;
            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms))
                {
                    // Chain Id
                    bw.Write(chainId);

                    //trx headers
                    bw.Write(expiration);
                    bw.Write((ushort)0); //ref_block_num
                    bw.Write((uint)0);// ref_block_prefix

                    //trx info
                    bw.Write((byte)0); //max_net_usage_words
                    bw.Write((byte)0); //max_cpu_usage_ms
                    bw.Write((byte)0); //delay_sec

                    // Context free #
                    bw.Write((byte)0);

                    // Action #
                    bw.Write((byte)1);

                    bw.Write(ContractNameValue);
                    bw.Write(ActionNameValue);

                    // Authorization #
                    bw.Write((byte)1);
                    // Permission
                    bw.Write(wallet.NameToLong());
                    bw.Write(permission.NameToLong());

                    bw.EncodeInt32(ActionDataLength);

                    // Action data
                    bw.Write(scope.NameToLong());
                    bw.Write((byte)1);
                    bw.Write(wallet.NameToLong());
                    bw.Write(permission.NameToLong());

                    // transaction_extensions #
                    bw.Write((byte)0);

                    // Trailing 32 empty bytes
                    bw.Write(TrailingEmptyBytes);
                    bw.Flush();
                    data = ms.ToArray();
                }
            }
            return data;
        }
    }
}