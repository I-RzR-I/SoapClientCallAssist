#region U S I N G

using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssistTests.Common;
using System.Collections.Generic;
using System.Net.Http;

#endregion

namespace SoapClientCallAssistTests.Helpers
{
    public sealed class RejectedSoapCall
    {
        public RejectedSoapCall(IResult<HttpResponseMessage> send, IResult faultCheck, SoapFaultInfo fault)
        {
            Send = send;
            FaultCheck = faultCheck;
            Fault = fault;
        }

        public IResult<HttpResponseMessage> Send { get; }

        public IResult FaultCheck { get; }

        public SoapFaultInfo Fault { get; }

        public void AssertFault(string expectedNamespace, string expectedLocalName, string what)
        {
            Assert.AreEqual(expectedNamespace, Fault.NamespaceName, what + " | " + Fault);
            Assert.AreEqual(expectedLocalName, Fault.LocalName, what + " | " + Fault);
        }

        public void AssertNoSecret(IEnumerable<string> secrets, string what)
        {
            var secretList = new List<string>(secrets);

            AuthTestSupport.AssertNoSecret(Send, secretList, what + " (SendRequest)");
            AuthTestSupport.AssertNoSecret(FaultCheck, secretList, what + " (CheckBodyForFaultCode)");
        }
    }
}
