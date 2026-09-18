using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Web.Services.Protocols;
using System.Xml;
using TestSoapServiceN45.Dto;

namespace TestSoapServiceN45.Security
{
    public sealed class WsSecurityAsmxAuthenticator
    {
        public static readonly TimeSpan FreshnessWindow = TimeSpan.FromMinutes(5);

        public const string PasswordTextMode = "UsernameToken/PasswordText";

        public const string PasswordDigestMode = "UsernameToken/PasswordDigest";

        public const string X509Mode = "X509";

        private readonly SoapProtocolVersion _version;

        private readonly bool _requireSignature;

        public WsSecurityAsmxAuthenticator(SoapProtocolVersion version, bool requireSignature)
        {
            _version = version;
            _requireSignature = requireSignature;
        }

        public AsmxCallerIdentity Authenticate(XmlDocument envelope)
        {
            var now = DateTime.UtcNow;
            var security = FindSecurityHeader(envelope);

            var usernameTokens = ChildElements(security, WsSecurityAsmxNames.WsseNamespace, "UsernameToken");
            var signatures = ChildElements(security, WsSecurityAsmxNames.DsNamespace, "Signature");
            var timestamps = ChildElements(security, WsSecurityAsmxNames.WsuNamespace, "Timestamp");

            if (usernameTokens.Count > 1 || signatures.Count > 1 || timestamps.Count > 1)
                throw Invalid("The Security header carries a repeated UsernameToken, Signature or Timestamp.");

            if (usernameTokens.Count == 0 && signatures.Count == 0)
                throw Invalid("The Security header carries neither a UsernameToken nor a Signature.");

            if (_requireSignature && signatures.Count == 0)
                throw Invalid("This operation requires an X.509 signature.");

            if (timestamps.Count == 1)
                CheckTimestamp(timestamps[0], now);

            var identity = new AsmxCallerIdentity
            {
                UserName = string.Empty,
                CertificateThumbprint = string.Empty,
                Mode = string.Empty,
                SignedParts = string.Empty
            };

            string nonceToRegister = null;

            if (usernameTokens.Count == 1)
                nonceToRegister = AuthenticateUsernameToken(usernameTokens[0], now, identity);

            if (signatures.Count == 1)
                VerifySignature(envelope, security, signatures[0], identity);

            if (nonceToRegister != null && !NonceReplayCache.TryRegister(nonceToRegister, now, FreshnessWindow + FreshnessWindow))
                throw Failed("The UsernameToken nonce was already accepted once; the message is a replay.");

            return identity;
        }

        private XmlElement FindSecurityHeader(XmlDocument envelope)
        {
            var root = envelope.DocumentElement;
            if (root == null || root.LocalName != "Envelope")
                throw Invalid("The request is not a SOAP envelope.");

            var headers = ChildElements(root, root.NamespaceURI, "Header");
            if (headers.Count == 0)
                throw Invalid("The request carries no SOAP Header, so no Security header.");

            var securities = ChildElements(headers[0], WsSecurityAsmxNames.WsseNamespace, "Security");
            if (securities.Count == 0)
                throw Invalid("The request carries no wsse:Security header.");

            if (securities.Count > 1)
                throw Invalid("The request carries more than one wsse:Security header; the verifier targets exactly one role.");

            return securities[0];
        }

        private string AuthenticateUsernameToken(XmlElement token, DateTime now, AsmxCallerIdentity identity)
        {
            var username = SingleChildText(token, WsSecurityAsmxNames.WsseNamespace, "Username");
            if (string.IsNullOrEmpty(username))
                throw Invalid("The UsernameToken names no user.");

            var passwordElements = ChildElements(token, WsSecurityAsmxNames.WsseNamespace, "Password");
            if (passwordElements.Count != 1)
                throw Failed("The UsernameToken carries no password, so it cannot be authenticated.");

            var passwordElement = passwordElements[0];
            var passwordType = passwordElement.HasAttribute("Type") ? passwordElement.GetAttribute("Type") : WsSecurityAsmxNames.PasswordTextType;
            var digest = string.Equals(passwordType, WsSecurityAsmxNames.PasswordDigestType, StringComparison.Ordinal);

            if (!digest && !string.Equals(passwordType, WsSecurityAsmxNames.PasswordTextType, StringComparison.Ordinal))
                throw Invalid("The UsernameToken password type is not supported.");

            var nonceElements = ChildElements(token, WsSecurityAsmxNames.WsseNamespace, "Nonce");
            var createdElements = ChildElements(token, WsSecurityAsmxNames.WsuNamespace, "Created");

            if (nonceElements.Count > 1 || createdElements.Count > 1)
                throw Invalid("The UsernameToken repeats Nonce or Created.");

            if (digest && nonceElements.Count == 0)
                throw Invalid("A PasswordDigest token must carry a Nonce.");

            if (digest && createdElements.Count == 0)
                throw Invalid("A PasswordDigest token must carry a Created instant.");

            byte[] nonce = null;
            string nonceText = null;

            if (nonceElements.Count == 1)
            {
                var nonceElement = nonceElements[0];
                if (nonceElement.HasAttribute("EncodingType")
                    && !string.Equals(nonceElement.GetAttribute("EncodingType"), WsSecurityAsmxNames.Base64BinaryEncodingType, StringComparison.Ordinal))
                    throw Invalid("The UsernameToken nonce encoding is not Base64Binary.");

                nonceText = nonceElement.InnerText.Trim();

                try
                {
                    nonce = Convert.FromBase64String(nonceText);
                }
                catch (FormatException)
                {
                    throw Invalid("The UsernameToken nonce is not valid base64.");
                }

                if (nonce.Length == 0)
                    throw Invalid("The UsernameToken nonce is empty.");
            }

            string createdText = null;

            if (createdElements.Count == 1)
            {
                createdText = createdElements[0].InnerText.Trim();
                CheckFreshness(ParseInstant(createdText, "The UsernameToken Created instant is not a valid xs:dateTime."), now, "UsernameToken Created");
            }

            string expectedPassword;
            if (!TestCredentials.TryGetPassword(username, out expectedPassword))
                throw Failed("The user name or password is not known.");

            var presented = passwordElement.InnerText;
            var expected = digest ? ComputeDigest(nonce, createdText, expectedPassword) : expectedPassword;

            if (!FixedTimeEquals(presented, expected))
                throw Failed("The user name or password is not known.");

            identity.UserName = username;
            identity.Mode = digest ? PasswordDigestMode : PasswordTextMode;

            return nonceText;
        }

        private void VerifySignature(XmlDocument envelope, XmlElement security, XmlElement signatureElement, AsmxCallerIdentity identity)
        {
            var certificate = ResolveSigningCertificate(security, signatureElement);

            if (!TestCredentials.IsTrustedThumbprint(certificate.Thumbprint))
                throw Failed("The signing certificate is not on the allow-list.");

            var signedXml = new WsuSignedXml(envelope);
            bool verified;
            var coveredParts = new List<string>();

            try
            {
                signedXml.LoadXml(signatureElement);

                foreach (Reference reference in signedXml.SignedInfo.References)
                {
                    var uri = reference.Uri ?? string.Empty;
                    if (!uri.StartsWith("#", StringComparison.Ordinal))
                        throw new CryptographicException("A signature reference is not a same-document id reference.");

                    var target = signedXml.GetIdElement(envelope, uri.Substring(1));
                    if (target == null)
                        throw new CryptographicException("A signature reference names an element that is not in the message.");

                    coveredParts.Add(target.LocalName);
                }

                verified = signedXml.CheckSignature(certificate.PublicKey.Key);
            }
            catch (CryptographicException ex)
            {
                throw new WsSecurityFault(_version, WsSecurityAsmxNames.FailedCheckFault, "The signature could not be checked: " + ex.Message);
            }

            if (!verified)
                throw new WsSecurityFault(_version, WsSecurityAsmxNames.FailedCheckFault, "The signature does not verify against the certificate it names.");

            if (!coveredParts.Contains("Body") && !coveredParts.Contains("Timestamp"))
                throw Invalid("The signature covers neither the Body nor the Timestamp.");

            identity.CertificateThumbprint = certificate.Thumbprint;
            identity.SignedParts = string.Join(",", coveredParts);
            identity.Mode = identity.Mode.Length == 0 ? X509Mode : identity.Mode + "+" + X509Mode;
        }

        private X509Certificate2 ResolveSigningCertificate(XmlElement security, XmlElement signatureElement)
        {
            var tokens = ChildElements(security, WsSecurityAsmxNames.WsseNamespace, "BinarySecurityToken");
            XmlElement token = null;

            var keyInfos = ChildElements(signatureElement, WsSecurityAsmxNames.DsNamespace, "KeyInfo");
            if (keyInfos.Count == 1)
            {
                var references = ChildElements(keyInfos[0], WsSecurityAsmxNames.WsseNamespace, "SecurityTokenReference");
                if (references.Count == 1)
                {
                    var directReferences = ChildElements(references[0], WsSecurityAsmxNames.WsseNamespace, "Reference");
                    if (directReferences.Count == 1)
                    {
                        var uri = directReferences[0].GetAttribute("URI");
                        if (uri.StartsWith("#", StringComparison.Ordinal))
                        {
                            var id = uri.Substring(1);
                            foreach (var candidate in tokens)
                                if (string.Equals(candidate.GetAttribute("Id", WsSecurityAsmxNames.WsuNamespace), id, StringComparison.Ordinal))
                                    token = candidate;
                        }
                    }
                }
            }

            if (token == null && tokens.Count == 1)
                token = tokens[0];

            if (token == null)
                throw Invalid("The signature names no BinarySecurityToken that is present in the Security header.");

            if (token.HasAttribute("ValueType")
                && !string.Equals(token.GetAttribute("ValueType"), WsSecurityAsmxNames.X509TokenValueType, StringComparison.Ordinal))
                throw Invalid("The BinarySecurityToken is not an X509v3 token.");

            try
            {
                return new X509Certificate2(Convert.FromBase64String(token.InnerText.Trim()));
            }
            catch (FormatException)
            {
                throw Invalid("The BinarySecurityToken is not valid base64.");
            }
            catch (CryptographicException)
            {
                throw Invalid("The BinarySecurityToken does not decode to a certificate.");
            }
        }

        private void CheckTimestamp(XmlElement timestamp, DateTime now)
        {
            var created = SingleChildText(timestamp, WsSecurityAsmxNames.WsuNamespace, "Created");
            var expires = SingleChildText(timestamp, WsSecurityAsmxNames.WsuNamespace, "Expires");

            if (created != null)
                CheckFreshness(ParseInstant(created, "The Timestamp Created instant is not a valid xs:dateTime."), now, "Timestamp Created");

            if (expires != null && ParseInstant(expires, "The Timestamp Expires instant is not a valid xs:dateTime.") < now - FreshnessWindow)
                throw Failed("The Timestamp has expired.");
        }

        private void CheckFreshness(DateTime instant, DateTime now, string what)
        {
            var skew = instant > now ? instant - now : now - instant;
            if (skew > FreshnessWindow)
                throw Failed("The " + what + " instant is outside the accepted window.");
        }

        private DateTime ParseInstant(string text, string invalidMessage)
        {
            try
            {
                return XmlConvert.ToDateTime(text, XmlDateTimeSerializationMode.Utc);
            }
            catch (FormatException)
            {
                throw Invalid(invalidMessage);
            }
        }

        private WsSecurityFault Invalid(string reason)
        {
            return new WsSecurityFault(_version, WsSecurityAsmxNames.InvalidSecurityFault, reason);
        }

        private WsSecurityFault Failed(string reason)
        {
            return new WsSecurityFault(_version, WsSecurityAsmxNames.FailedAuthenticationFault, reason);
        }

        private static string ComputeDigest(byte[] nonce, string created, string password)
        {
            var createdBytes = Encoding.UTF8.GetBytes(created);
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var input = new byte[nonce.Length + createdBytes.Length + passwordBytes.Length];

            Buffer.BlockCopy(nonce, 0, input, 0, nonce.Length);
            Buffer.BlockCopy(createdBytes, 0, input, nonce.Length, createdBytes.Length);
            Buffer.BlockCopy(passwordBytes, 0, input, nonce.Length + createdBytes.Length, passwordBytes.Length);

            try
            {
                using (var sha1 = SHA1.Create())
                    return Convert.ToBase64String(sha1.ComputeHash(input));
            }
            finally
            {
                Array.Clear(input, 0, input.Length);
                Array.Clear(passwordBytes, 0, passwordBytes.Length);
            }
        }

        private static bool FixedTimeEquals(string left, string right)
        {
            var a = Encoding.UTF8.GetBytes(left ?? string.Empty);
            var b = Encoding.UTF8.GetBytes(right ?? string.Empty);
            var difference = a.Length ^ b.Length;

            for (var i = 0; i < a.Length && i < b.Length; i++)
                difference |= a[i] ^ b[i];

            return difference == 0;
        }

        private static List<XmlElement> ChildElements(XmlElement parent, string namespaceUri, string localName)
        {
            var result = new List<XmlElement>();

            foreach (XmlNode child in parent.ChildNodes)
            {
                var element = child as XmlElement;
                if (element != null && element.LocalName == localName && element.NamespaceURI == namespaceUri)
                    result.Add(element);
            }

            return result;
        }

        private string SingleChildText(XmlElement parent, string namespaceUri, string localName)
        {
            var elements = ChildElements(parent, namespaceUri, localName);
            if (elements.Count > 1)
                throw Invalid("The element " + parent.LocalName + " repeats " + localName + ".");

            return elements.Count == 0 ? null : elements[0].InnerText.Trim();
        }
    }
}
