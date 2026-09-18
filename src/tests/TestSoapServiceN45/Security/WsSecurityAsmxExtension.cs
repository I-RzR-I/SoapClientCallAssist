using System;
using System.IO;
using System.Web;
using System.Web.Services.Protocols;
using System.Xml;
using TestSoapServiceN45.Dto;

namespace TestSoapServiceN45.Security
{
    public sealed class WsSecurityAsmxExtension : SoapExtension
    {
        public const string IdentityItemKey = "TestSoapServiceN45.Security.AsmxCallerIdentity";

        private Stream _oldStream;

        private Stream _newStream;

        private bool _requireSignature;

        public override object GetInitializer(LogicalMethodInfo methodInfo, SoapExtensionAttribute attribute)
        {
            return attribute;
        }

        public override object GetInitializer(Type serviceType)
        {
            return null;
        }

        public override void Initialize(object initializer)
        {
            var attribute = initializer as WsSecurityRequiredAttribute;
            _requireSignature = attribute != null && attribute.RequireSignature;
        }

        public override Stream ChainStream(Stream stream)
        {
            _oldStream = stream;
            _newStream = new MemoryStream();

            return _newStream;
        }

        public override void ProcessMessage(SoapMessage message)
        {
            switch (message.Stage)
            {
                case SoapMessageStage.BeforeDeserialize:
                    AuthenticateRequest(message);
                    break;

                case SoapMessageStage.AfterDeserialize:
                    MarkSecurityHeadersUnderstood(message);
                    break;

                case SoapMessageStage.BeforeSerialize:
                    break;

                case SoapMessageStage.AfterSerialize:
                    CopyResponseOut();
                    break;

                default:
                    throw new InvalidOperationException("Unexpected SOAP message stage " + message.Stage + ".");
            }
        }

        private void AuthenticateRequest(SoapMessage message)
        {
            byte[] raw;

            using (var buffer = new MemoryStream())
            {
                _oldStream.CopyTo(buffer);
                raw = buffer.ToArray();
            }

            _newStream.Write(raw, 0, raw.Length);
            _newStream.Position = 0;

            var envelope = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };

            try
            {
                using (var reader = XmlReader.Create(new MemoryStream(raw), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }))
                    envelope.Load(reader);
            }
            catch (XmlException ex)
            {
                throw new WsSecurityFault(message.SoapVersion, WsSecurityAsmxNames.InvalidSecurityFault, "The request is not well-formed XML: " + ex.Message);
            }

            AsmxCallerIdentity identity;

            try
            {
                identity = new WsSecurityAsmxAuthenticator(message.SoapVersion, _requireSignature).Authenticate(envelope);
            }
            catch (WsSecurityFault fault)
            {
                SecuredDiagnosticsLog.Write(nameof(WsSecurityAsmxExtension), "Refused " + message.MethodInfo.Name + " with wsse:" + fault.WsseCode + ": " + fault.Message);

                throw;
            }

            SecuredDiagnosticsLog.Write(
                nameof(WsSecurityAsmxExtension),
                "Accepted " + message.MethodInfo.Name + " as '" + identity.UserName + "' mode " + identity.Mode + " thumbprint '" + identity.CertificateThumbprint + "'.");

            var context = HttpContext.Current;
            if (context != null)
                context.Items[IdentityItemKey] = identity;
        }

        private static void MarkSecurityHeadersUnderstood(SoapMessage message)
        {
            foreach (SoapHeader header in message.Headers)
            {
                var unknown = header as SoapUnknownHeader;
                if (unknown == null || unknown.Element == null)
                    continue;

                if (unknown.Element.LocalName == "Security" && unknown.Element.NamespaceURI == WsSecurityAsmxNames.WsseNamespace)
                    unknown.DidUnderstand = true;
            }
        }

        private void CopyResponseOut()
        {
            if (_newStream == null || _oldStream == null)
                return;

            _newStream.Position = 0;
            _newStream.CopyTo(_oldStream);
        }
    }
}
