#nullable disable

using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Dto.Public;
using System;
using System.Security.Cryptography.X509Certificates;

namespace SoapClientCallAssistTests.Soap12.Helpers.Security;

internal sealed class StubSoapMessageVerifier : ISoapMessageVerifier
{

    private readonly Func<IResult<SoapSignatureVerificationResult>> _behaviour;

    internal StubSoapMessageVerifier(Func<IResult<SoapSignatureVerificationResult>> behaviour)
        => _behaviour = behaviour;

    internal bool WasCalled { get; private set; }

    public IResult<SoapSignatureVerificationResult> Verify(string soapResponse, X509Certificate2 expectedCertificate)
        => Verify(soapResponse, expectedCertificate, null);

    public IResult<SoapSignatureVerificationResult> Verify(
        string soapResponse, X509Certificate2 expectedCertificate, SoapVerificationPolicyDto policy)
    {
        WasCalled = true;

        return _behaviour();
    }
}
