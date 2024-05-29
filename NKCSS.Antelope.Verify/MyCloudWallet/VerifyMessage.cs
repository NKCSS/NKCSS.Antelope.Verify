namespace NKCSS.Antelope.Verify.MyCloudWallet
{
    /// <summary>
    /// The return format when you manually request for a validation message to be signed 
    /// (does not match <see cref="TransparantVerifyMessage"/>)
    /// </summary>
    /// <remarks>
    /// Make sure you manually set the <see cref="nonce"/> that you passed to the validation function
    /// before checking the altered state, because this does not get returned with the message.
    /// </remarks>
    public struct VerifyMessage
    {
        const string MessageFieldSeparator = "-";
        const string MessagePrefix = "cloudwallet-verification";
        public string type { get; set; }
        public string accountName { get; set; }
        public string referer { get; set; }
        public string signature { get; set; }
        public string message { get; set; }
        /// <summary>
        /// This is not returned; this should be the field you passed to the verification, 
        /// so set it before you check <see cref="Altered"/>
        /// </summary>
        public string nonce { get; set; }
        /// <summary>
        /// This is the message that should have been signed, which you'll test against the reported signature
        /// </summary>
        public string ExpectedMessage { get => string.Join(MessageFieldSeparator, new string[] { MessagePrefix, referer, nonce, accountName }); }
        public bool Altered => message != ExpectedMessage;
    }
}
