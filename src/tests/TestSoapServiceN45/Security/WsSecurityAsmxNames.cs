namespace TestSoapServiceN45.Security
{
    public static class WsSecurityAsmxNames
    {
        public const string WsseNamespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";

        public const string WsuNamespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";

        public const string DsNamespace = "http://www.w3.org/2000/09/xmldsig#";

        public const string PasswordTextType = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordText";

        public const string PasswordDigestType = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordDigest";

        public const string Base64BinaryEncodingType = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary";

        public const string X509TokenValueType = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-x509-token-profile-1.0#X509v3";

        public const string InvalidSecurityFault = "InvalidSecurity";

        public const string FailedAuthenticationFault = "FailedAuthentication";

        public const string FailedCheckFault = "FailedCheck";
    }
}
