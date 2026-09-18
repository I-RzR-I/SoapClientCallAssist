#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Security;
using SoapClientCallAssist.Security.WsSecurity;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using System;
using System.Collections.Generic;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.Verification;

[TestClass]
public sealed class KeyFamilyRoutingTests
{

    private static readonly Type KeyFamilyType = WsSecurityFoundationTestSupport.LibraryType("SoapClientCallAssist.Enums.SignatureKeyFamily");

    [TestMethod]
    public void Verify_WithSymmetricMaterialWhoseSnapshotClaimsTheRsaFamily_RefusesTheHmacResponseAtTheShapeGate_Test()
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity());
        var wire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request));

        var response = new SymmetricResponseBuilder(wire.Secret, wire.EncryptedKeySha1).RelatesTo(wire.MessageId).Confirm(wire.PrimarySignatureValue).Build();

        var material = KeyMaterialReflection.MaterialOf(request);

        using var honest = KeyMaterialReflection.Plant(KeyMaterialReflection.Reconstruct(material, new Dictionary<string, object>()));
        using var claimingRsa = KeyMaterialReflection.Plant(KeyMaterialReflection.Reconstruct(material, new Dictionary<string, object> { ["keyFamily"] = Enum.Parse(KeyFamilyType, "Rsa") }));

        WsSecurityAssert.Accepted(new WsSecurityResponseSecurity().Verify(honest, response), "");

        WsSecurityAssert.Rejected(
            new WsSecurityResponseSecurity().Verify(claimingRsa, response),
            WsSecurityTestSupport.AlgorithmCode,
            "");
    }

    [TestMethod]
    public void Verify_WithAsymmetricMaterialWhoseSnapshotClaimsTheHmacFamily_RefusesTheRsaResponseBeforeTheVerifierRuns_Test()
    {
        using var request = WsSecurityTestSupport.BuildPost(WsSecurityTestSupport.Security(security =>
        {
            security.ExpectedResponseCertificate = WsSecurityTestSupport.OtherCertificate;
            security.ResponseSecurity = new SoapResponseSecurityDto { AllowUnboundResponse = true };
        })).Response;

        var response = WsSecurityFoundationTestSupport.SignedResponse(WsSecurityTestSupport.OtherCertificate, null);

        var material = KeyMaterialReflection.MaterialOf(request);

        using var honest = KeyMaterialReflection.Plant(KeyMaterialReflection.Reconstruct(material, new Dictionary<string, object>()));
        using var claimingHmac = KeyMaterialReflection.Plant(KeyMaterialReflection.Reconstruct(material, new Dictionary<string, object> { ["keyFamily"] = Enum.Parse(KeyFamilyType, "Hmac") }));

        WsSecurityAssert.Accepted(new WsSecurityResponseSecurity().Verify(honest, response), "");

        var registered = new StubSoapMessageVerifier(() => new WsSecurityMessageVerifier().Verify(response, WsSecurityTestSupport.OtherCertificate));

        WsSecurityAssert.Rejected(
            new WsSecurityResponseSecurity(registered).Verify(claimingHmac, response),
            WsSecurityTestSupport.AlgorithmCode,
            "");

        Assert.IsFalse(registered.WasCalled);
    }

    [TestMethod]
    public void Verify_WithSymmetricMaterialWhoseSnapshotClaimsNoFamily_RefusesAsUnusableMaterial_Test()
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity());
        var wire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request));

        using var claimingNone = KeyMaterialReflection.Plant(KeyMaterialReflection.Reconstruct(KeyMaterialReflection.MaterialOf(request), new Dictionary<string, object> { ["keyFamily"] = null }));

        WsSecurityAssert.Rejected(
            new WsSecurityResponseSecurity().Verify(claimingNone, new SymmetricResponseBuilder(wire.Secret, wire.EncryptedKeySha1).RelatesTo(wire.MessageId).Confirm(wire.PrimarySignatureValue).Build()),
            SymmetricTestSupport.NoMaterialCode,
            "");
    }
}
