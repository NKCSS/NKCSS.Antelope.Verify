namespace NKCSS.Antelope.Verify.MyCloudWallet
{
    public struct TransparantVerifyMessage
    {
        /// <summary>
        /// The way MyCloudWallet builds the data to sign, is to concat a big string using this separator.
        /// </summary>
        const string MessageFieldSeparator = "-";
        /// <summary>
        /// The message that gets signed, is prefixed with this fixed string.
        /// </summary>
        const string MessagePrefix = "cloudwallet-verification";
        /// <summary>
        /// The wallet that signed the verification
        /// </summary>
        public string userAccount { get; set; }
        /// <summary>
        /// The website the verification was triggered from
        /// </summary>
        public string referrer { get; set; }
        /// <summary>
        /// The signature that we need to validate
        /// </summary>
        public string signature { get; set; }
        /// <summary>
        /// The reported message that was signed
        /// </summary>
        /// <remarks>
        /// We check if this lines up with the rest of the data to prevent tampering
        /// </remarks>
        public string message { get; set; }
        /// <summary>
        /// The server-controlled nonce that pas passed to be part of the message that was signed.
        /// </summary>
        public string nonce { get; set; }
        /// <summary>
        /// We can rebuild the message based on the reported data fields.
        /// This lets us check for tampering as well.
        /// </summary>
        public string ExpectedMessage { get => string.Join(MessageFieldSeparator, new string[] { MessagePrefix, referrer, nonce, userAccount }); }
        /// <summary>
        /// Checks if the <see cref="message">signed string</see> matches with <see cref="ExpectedMessage"/>
        /// </summary>
        public bool Altered => message != ExpectedMessage;
    }
}
