using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssistTests.Common;
using System.Collections.Generic;
using System.Net.Http;

namespace SoapClientCallAssistTests.Wcf.Helpers;

internal sealed class RejectedCall
{
    internal RejectedCall(IResult<HttpResponseMessage> send, IResult faultCheck, SoapFaultInfo fault)
    {
        Send = send;
        FaultCheck = faultCheck;
        Fault = fault;
    }

    internal IResult<HttpResponseMessage> Send { get; }

    internal IResult FaultCheck { get; }

    internal SoapFaultInfo Fault { get; }

    internal void AssertFault(string expectedNamespace, string expectedLocalName, string what)
    {
        Assert.AreEqual(expectedNamespace, Fault.NamespaceName, $"{what} | {Fault}");
        Assert.AreEqual(expectedLocalName, Fault.LocalName, $"{what} | {Fault}");
    }

    internal void AssertNoSecret(IEnumerable<string> secrets, string what)
    {
        var secretList = new List<string>(secrets);

        ResultLeakSweep.AssertNoSecret(Send, secretList, $"{what} (SendRequestAsync)");
        ResultLeakSweep.AssertNoSecret(FaultCheck, secretList, $"{what} (CheckBodyForFaultCode)");
    }
}
