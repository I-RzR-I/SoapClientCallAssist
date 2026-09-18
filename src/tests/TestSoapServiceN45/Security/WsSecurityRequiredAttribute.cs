using System;
using System.Web.Services.Protocols;

namespace TestSoapServiceN45.Security
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class WsSecurityRequiredAttribute : SoapExtensionAttribute
    {
        private int _priority;

        public override Type ExtensionType
        {
            get { return typeof(WsSecurityAsmxExtension); }
        }

        public override int Priority
        {
            get { return _priority; }
            set { _priority = value; }
        }

        public bool RequireSignature { get; set; }
    }
}
