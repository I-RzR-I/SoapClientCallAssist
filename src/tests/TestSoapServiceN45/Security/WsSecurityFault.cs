using System.Web.Services.Protocols;
using System.Xml;

namespace TestSoapServiceN45.Security
{
    public sealed class WsSecurityFault : SoapException
    {
        public WsSecurityFault(SoapProtocolVersion version, string wsseCode, string reason)
            : base(
                reason,
                version == SoapProtocolVersion.Soap12 ? ClientFaultCode : new XmlQualifiedName(wsseCode, WsSecurityAsmxNames.WsseNamespace),
                null,
                null,
                null,
                null,
                version == SoapProtocolVersion.Soap12 ? new SoapFaultSubCode(new XmlQualifiedName(wsseCode, WsSecurityAsmxNames.WsseNamespace)) : null,
                null)
        {
            WsseCode = wsseCode;
        }

        public string WsseCode { get; private set; }
    }
}
