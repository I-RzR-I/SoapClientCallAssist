#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.Xml;
using System.Xml;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.Saml;

[TestClass]
public sealed class SamlIdPrecedenceTests
{

    private static readonly XNamespace Wsu = WsSecurityTestSupport.WsuNamespace;

    [TestMethod]
    public void SignedXml_ResolvesAPlantedUnqualifiedIdBeforeTheAssertionId_WhichIsTheHazardTheGateGuards_Test()
    {
        var assertion = SamlTestSupport.Bearer20(issuance => issuance.Sign = false);
        var id = SamlTestSupport.IdOf(assertion);

        var document = new XmlDocument { PreserveWhitespace = true };
        document.LoadXml($"<root><planted Id=\"{id}\"/></root>");
        document.DocumentElement.AppendChild(document.ImportNode(assertion, true));

        var resolved = new SignedXml(document).GetIdElement(document, id);

        Assert.IsNotNull(resolved);
        Assert.AreEqual("planted", resolved.LocalName);
    }

    [DataTestMethod]
    [DataRow("wsu", DisplayName = "planted wsu:Id")]
    [DataRow("Id", DisplayName = "planted unqualified Id")]
    [DataRow("id", DisplayName = "planted unqualified id")]
    [DataRow("ID", DisplayName = "planted unqualified ID")]
    public void BuildRequest_WithAHolderOfKeyAssertionAndAHeaderPlantedWithItsIdentifier_RefusesAsAmbiguous_Test(string attribute)
    {
        var assertion = SamlTestSupport.HolderOfKey20(WsSecurityTestSupport.SigningCertificate);

        var built = WsSecurityTestSupport.BuildWithHeaders(
            SoapProtocolType.SOAP_1_2,
            HttpMethod.Post,
            SamlTestSupport.HolderOfKeySecurity(assertion),
            new[] { PlantedHeader(attribute, SamlTestSupport.IdOf(assertion)) });

        WsSecurityFoundationTestSupport.RefusedWith(
            built,
            SamlTestSupport.AmbiguousIdCode,
            attribute,
            SamlTestSupport.Secrets(assertion));
    }

    [TestMethod]
    public void BuildRequest_WithASaml11HolderOfKeyAssertionAndAHeaderPlantedWithItsAssertionId_RefusesAsAmbiguous_Test()
    {
        var assertion = SamlTestSupport.HolderOfKey11(WsSecurityTestSupport.SigningCertificate);

        var built = WsSecurityTestSupport.BuildWithHeaders(
            SoapProtocolType.SOAP_1_2,
            HttpMethod.Post,
            SamlTestSupport.HolderOfKeySecurity(assertion),
            new[] { PlantedHeader("wsu", SamlTestSupport.IdOf(assertion)) });

        WsSecurityFoundationTestSupport.RefusedWith(built, SamlTestSupport.AmbiguousIdCode, "", SamlTestSupport.Secrets(assertion));
    }

    [TestMethod]
    public void BuildRequest_WithABearerAssertionAndAHeaderPlantedWithItsIdentifier_RefusesAsAmbiguous_Test()
    {
        var assertion = SamlTestSupport.Bearer20();

        var built = WsSecurityTestSupport.BuildWithHeaders(
            SoapProtocolType.SOAP_1_2,
            HttpMethod.Post,
            SamlTestSupport.BearerOnlySecurity(assertion),
            new[] { PlantedHeader("wsu", SamlTestSupport.IdOf(assertion)) });

        WsSecurityFoundationTestSupport.RefusedWith(built, SamlTestSupport.AmbiguousIdCode, "", SamlTestSupport.Secrets(assertion));
    }

    [TestMethod]
    public void BuildRequest_WithAHolderOfKeyAssertionAndAHeaderCarryingADifferentId_IsCarriedAndSigned_Test()
    {
        var assertion = SamlTestSupport.HolderOfKey20(WsSecurityTestSupport.SigningCertificate);

        var wire = WsSecurityTestSupport.Wire(
            WsSecurityTestSupport.BuildWithHeaders(
                SoapProtocolType.SOAP_1_2,
                HttpMethod.Post,
                SamlTestSupport.HolderOfKeySecurity(assertion),
                new[] { PlantedHeader("wsu", "unrelated-" + SamlTestSupport.IdOf(assertion)) }),
            "Build");

        var references = SamlTestSupport.MessageSignatureReferences(WsSecurityTestSupport.ParseWire(wire));

        Assert.IsTrue(references.Contains("#" + SamlTestSupport.IdOf(assertion)), $"{string.Join(", ", references)} | {wire}");
    }

    [TestMethod]
    public void BuildRequest_WithAHolderOfKeyAssertionCarryingTheSameIdentifierTwice_RefusesAsAmbiguous_Test()
    {
        var assertion = SamlTestSupport.Bearer20(issuance => issuance.Sign = false);
        var id = SamlTestSupport.IdOf(assertion);

        SamlTestSupport.Conditions(assertion).SetAttribute("Id", id);

        WsSecurityFoundationTestSupport.RefusedWith(
            WsSecurityTestSupport.BuildPost(SamlTestSupport.BearerOnlySecurity(assertion)),
            SamlTestSupport.AmbiguousIdCode,
            "",
            SamlTestSupport.Secrets(assertion));
    }

    private static XElement PlantedHeader(string attribute, string value)
    {
        var header = new XElement(WsSecurityTestSupport.Service + "Planted", "decoy");

        if (attribute == "wsu")
        {
            header.Add(new XAttribute(XNamespace.Xmlns + "wsu", Wsu.NamespaceName));
            header.Add(new XAttribute(Wsu + "Id", value));
        }
        else
            header.Add(new XAttribute(attribute, value));

        return header;
    }
}
