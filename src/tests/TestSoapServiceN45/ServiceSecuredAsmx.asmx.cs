using System.ComponentModel;
using System.Web;
using System.Web.Services;
using System.Web.Services.Protocols;
using TestSoapServiceN45.Dto;
using TestSoapServiceN45.Security;

namespace TestSoapServiceN45
{
    [WebService(Namespace = "http://SoapClientCallAssist.local/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ToolboxItem(false)]
    public class ServiceSecuredAsmx : WebService
    {
        [WebMethod]
        [WsSecurityRequired]
        public AsmxCallerIdentity WhoAmI()
        {
            return CurrentIdentity();
        }

        [WebMethod]
        [WsSecurityRequired(RequireSignature = true)]
        public AsmxCallerIdentity WhoAmISigned()
        {
            return CurrentIdentity();
        }

        [WebMethod]
        [WsSecurityRequired]
        public string Echo(string value)
        {
            return value;
        }

        [WebMethod]
        public bool Ping()
        {
            return true;
        }

        private static AsmxCallerIdentity CurrentIdentity()
        {
            var context = HttpContext.Current;
            var identity = context == null ? null : context.Items[WsSecurityAsmxExtension.IdentityItemKey] as AsmxCallerIdentity;

            if (identity == null)
                throw new SoapException("The request reached the method without passing the WS-Security extension.", SoapException.ServerFaultCode);

            return identity;
        }
    }
}
