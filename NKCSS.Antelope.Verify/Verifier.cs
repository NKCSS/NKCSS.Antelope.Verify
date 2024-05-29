using Cryptography.ECDSA;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;
using System.Text;

namespace NKCSS.Antelope.Verify
{
    public static class Verifier
    {
        #region Constants
        const int RSSkip = 1;
        const int RSLength = 32;
        const int PublicKeyByteLength = 33;
        const int SignatureByteLength = 2 * RSLength + RSSkip;
        const string CurveName = "secp256k1";
        const string PublicKeyPrefix = "EOS";
        const string SignaturePrefix = "SIG_K1_";
        static readonly int SignaturePrefixLength = SignaturePrefix.Length;
        static readonly int PublicKeyPrefixLength = PublicKeyPrefix.Length;
        /// <summary>
        /// This is the public key that is linked to Proof.Wax on Wax Mainnet
        /// </summary>
        /// <remarks>
        /// New style key: PUB_K1_6VZ558SAYB41Q1QyBmjZJT4hYtdu2Dzucy3K9XF3paFjVGhnUs
        /// Old Style key: EOS6VZ558SAYB41Q1QyBmjZJT4hYtdu2Dzucy3K9XF3paFjTUTmgm
        /// Both contain the same data, so it doens't matter which one is used really.
        /// If this were to change at one point, this needs to update here but I don't see why they would do this.
        /// Still good to be aware of though.
        /// Active key can be found here: https://waxblock.io/account/proof.wax#keys
        /// </remarks>
        const string ProofDotWaxActivePublicKey = "EOS6VZ558SAYB41Q1QyBmjZJT4hYtdu2Dzucy3K9XF3paFjTUTmgm";
        #endregion
        #region Private Helpers
        /// <summary>
        /// Extract the two signature components (r &amp; s) from <paramref name="signatureData"/>
        /// </summary>
        /// <param name="signatureData">The raw signature data</param>
        /// <returns>The two components (r &amp; s) that comprises the signature</returns>
        static (BigInteger r, BigInteger s) ExtractRs(byte[] signatureData) =>
        (
            new BigInteger(signatureData.Skip(RSSkip).Take(RSLength).ToArray()),
            new BigInteger(signatureData.Skip(RSSkip + RSLength).Take(RSLength).ToArray())
        );
        static ECPublicKeyParameters AsPKP(ECDomainParameters p, byte[] publicKey) => new ECPublicKeyParameters(p.Curve.DecodePoint(publicKey), p);
        #endregion
        #region Helper overloads for various Base-58 encoded values
        /// <summary>
        /// hecks signing signature
        /// </summary>
        /// <param name="signature">EOS-style signature (SIG_K1_ prefix)</param>
        /// <param name="publicKey">EOS-style public key string</param>
        /// <param name="message"></param>
        /// <remarks>
        /// This is the most-likely version to use from <see cref="MyCloudWallet.TransparantVerifyMessage"/>
        /// </remarks>
        /// <returns>If the data matches up</returns>
        public static bool Verify(string signature, string publicKey, string message) => Verify(
            Base58.Decode(signature.Substring(SignaturePrefixLength)).Take(SignatureByteLength).ToArray(),
            new PublicKey(publicKey).KeyBytes(),
            Encoding.UTF8.GetBytes(message)
        );
        /// <summary>
        /// hecks signing signature
        /// </summary>
        /// <param name="signature">EOS-style signature (SIG_K1_ prefix)</param>
        /// <param name="publicKey">EOS-style public key string</param>
        /// <param name="message">bytes of the data</param>
        /// <remarks>
        /// This is the most-likely version to use from <see cref="Anchor.VerifyMessage"/>
        /// </remarks>
        /// <returns>If the data matches up</returns>
        public static bool Verify(string signature, string publicKey, byte[] data) => Verify(
            Base58.Decode(signature.Substring(SignaturePrefixLength)).Take(SignatureByteLength).ToArray(),
            new PublicKey(publicKey).KeyBytes(),
            data
        );
        #endregion
        /// <summary>
        /// Checks signing signature
        /// </summary>
        /// <param name="signature">Signature bytes; 65 bytes length, no checksum (lead byte + 2x 32 bytes)</param>
        /// <param name="publicKey">Public key without checksum (should be 33-bytes in length)</param>
        /// <param name="data">Un-hashed data that was signed</param>
        /// <returns>If the data matches up</returns>
        public static bool Verify(byte[] signature, byte[] publicKey, byte[] data)
        {
            if ((signature?.Length ?? 0) != SignatureByteLength) throw new ArgumentException($"Invalid signature length ({signature?.Length ?? 0}); this should be {SignatureByteLength}", nameof(signature));
            if ((publicKey?.Length ?? 0) != PublicKeyByteLength) throw new ArgumentException($"Invalid signature length ({publicKey?.Length ?? 0}); this should be {PublicKeyByteLength}", nameof(publicKey));
            var ec = ECNamedCurveTable.GetByName(CurveName);
            var rs = ExtractRs(signature);
            ECDsaSigner signer = new();
            var pkp = AsPKP(new ECDomainParameters(ec.Curve, ec.G, ec.N, ec.H), publicKey);
            signer.Init(false, pkp);
            var hash = Sha256Manager.GetHash(data);
            return signer.VerifySignature(hash, rs.r, rs.s);
        }
        public static bool Verify(MyCloudWallet.TransparantVerifyMessage msg, string nonce)
            => Verify(msg.signature, ProofDotWaxActivePublicKey, msg.message) && !msg.Altered && msg.nonce.Equals(nonce);
        public static bool Verify(MyCloudWallet.VerifyMessage msg, string nonce)
        {
            msg.nonce = nonce;
            return Verify(msg.signature, ProofDotWaxActivePublicKey, msg.message) && !msg.Altered;
        }
        public static bool Verify(Anchor.VerifyMessage msg, string chainId)
            => Verify(msg.signature, msg.publickey, msg.SerializeData(chainId));
        public static bool Verify(Anchor.VerifyMessage msg, byte[] chainId)
            => Verify(msg.signature, msg.publickey, msg.SerializeData(chainId));
    }
}
