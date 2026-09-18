#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using SoapClientCallAssistTests.Soap12.Helpers.Wire;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.Signing;

[TestClass]
public sealed class WsSecurityWireGoldenTests
{

    private const string SigningInputCode = "V-SEC-001";

    private const string DsigDefaultNamespaceDeclaration = "<Signature xmlns=\"http://www.w3.org/2000/09/xmldsig#\">";

    private static readonly string[] SelfClosingElementNames =
    {
        "CanonicalizationMethod", "SignatureMethod", "Transform", "DigestMethod", "wsse:Reference"
    };

    public static IEnumerable<object[]> GoldenCases => WsSecurityWireGoldenMatrix.Cases.Select(goldenCase => new object[]
    {
        goldenCase
    });

    public static IEnumerable<object[]> RefusedCases => WsSecurityWireGoldenMatrix.RefusedCases.Select(
        goldenCase => new object[] { goldenCase });

    public static string CaseDisplayName(MethodInfo method, object[] data) => $"{method.Name} ({data[0]})";

    [ClassInitialize]
    public static void RegenerateGoldensWhenRequested(TestContext _) => WsSecurityWireGoldenMatrix.RegenerateIfRequested();

    [DataTestMethod]
    [DynamicData(nameof(GoldenCases), DynamicDataDisplayName = nameof(CaseDisplayName))]
    public void Golden_ForEachGoldenCase_IsOneLineWithoutBomTabsOrReformattedSelfClosingElements_Test(WsSecurityWireGoldenCase goldenCase)
        => WsSecurityWireGoldenIntegrity.AssertWellFormed(WsSecurityWireGoldenMatrix.GoldenPath(goldenCase));

    [DataTestMethod]
    [DynamicData(nameof(GoldenCases), DynamicDataDisplayName = nameof(CaseDisplayName))]
    public void Sign_ForEachGoldenCase_NormalisedWireMatchesTheCheckedInGolden_Test(WsSecurityWireGoldenCase goldenCase)
    {
        var goldenPath = WsSecurityWireGoldenMatrix.GoldenPath(goldenCase);

        WsSecurityWireGoldenIntegrity.AssertWellFormed(goldenPath);

        var normalised = WsSecurityWireNormaliser.Normalise(WsSecurityWireGoldenMatrix.Wire(goldenCase));

        Assert.AreEqual(WsSecurityWireGoldenMatrix.ReadGolden(goldenCase), normalised, $"{goldenCase.Name} | {goldenPath} | {normalised}");
    }

    [DataTestMethod]
    [DynamicData(nameof(RefusedCases), DynamicDataDisplayName = nameof(CaseDisplayName))]
    public void Sign_ForEachCombinationCoveringNothing_RefusesUnderTheSigningInputCode_Test(WsSecurityWireGoldenCase refusedCase)
    {
        var built = WsSecurityWireGoldenMatrix.Build(refusedCase);

        Assert.IsFalse(built.IsSuccess, $"{refusedCase.Name} | {NegativeTestSupport.Describe(built)}");

        Assert.AreEqual(SigningInputCode, NegativeTestSupport.Messages(built)[0].Key, $"{refusedCase.Name} | {NegativeTestSupport.Describe(built)}");
    }

    [TestMethod]
    public void GoldenDirectory_CarriesExactlyOneGoldenPerCaseAndNothingElse_Test()
    {
        var expected = WsSecurityWireGoldenMatrix.Cases
            .Select(goldenCase => goldenCase.Name + WsSecurityWireGoldenMatrix.GoldenExtension)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        var actual = Directory.GetFiles(WsSecurityWireGoldenMatrix.GoldenDirectory)
            .Select(Path.GetFileName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        CollectionAssert.AreEqual(expected, actual, $"{string.Join(", ", expected)} | {string.Join(", ", actual)}");
    }

    [TestMethod]
    public void Normalise_TwoConsecutiveSignings_DifferOnTheWireButNormaliseToTheSameString_Test()
    {
        var first = WsSecurityTestSupport.SignedWire();
        var second = WsSecurityTestSupport.SignedWire();

        Assert.AreNotEqual(first, second);

        Assert.AreEqual(WsSecurityWireNormaliser.Normalise(first), WsSecurityWireNormaliser.Normalise(second));
    }

    [TestMethod]
    public void Normalise_OnTheSameWireTwice_YieldsTheSameString_Test()
    {
        var wire = WsSecurityTestSupport.SignedWire();

        Assert.AreEqual(WsSecurityWireNormaliser.Normalise(wire), WsSecurityWireNormaliser.Normalise(wire));
    }

    [TestMethod]
    public void Normalise_OnASyntheticFragment_AssignsIdTokensInOrderOfFirstAppearanceAndMasksOnlyTheNamedText_Test()
    {
        const string fragment =
            "<a xmlns:x=\"u\" wsu:Id=\"first\"><b URI=\"#first\" /><c Id=\"second\"/><wsu:Created>t1</wsu:Created>"
            + "<wsu:Expires>t2</wsu:Expires><ds:DigestValue>d</ds:DigestValue><DigestValue>d2</DigestValue>"
            + "<SignatureValue>s</SignatureValue><wsse:BinarySecurityToken EncodingType=\"e\" wsu:Id=\"third\">b</wsse:BinarySecurityToken>"
            + "<wsse:Reference URI=\"#third\" ValueType=\"v\" /><keep>text</keep></a>";

        const string expected =
            "<a xmlns:x=\"u\" wsu:Id=\"{id:1}\"><b URI=\"#{id:1}\" /><c Id=\"{id:2}\"/><wsu:Created>{created}</wsu:Created>"
            + "<wsu:Expires>{expires}</wsu:Expires><ds:DigestValue>{digest}</ds:DigestValue><DigestValue>{digest}</DigestValue>"
            + "<SignatureValue>{sig}</SignatureValue><wsse:BinarySecurityToken EncodingType=\"e\" wsu:Id=\"{id:3}\">{bst}</wsse:BinarySecurityToken>"
            + "<wsse:Reference URI=\"#{id:3}\" ValueType=\"v\" /><keep>text</keep></a>";

        Assert.AreEqual(expected, WsSecurityWireNormaliser.Normalise(fragment));
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_1)]
    [DataRow(SoapProtocolType.SOAP_1_2)]
    public void SignedWire_DeclaresXmlnsWsuOnSecurityAndOnBody_NeverOnTheEnvelope_Test(SoapProtocolType protocol)
    {
        var wire = SignedWire(protocol);

        var wsuDeclaration = $"xmlns:wsu=\"{WsSecurityTestSupport.WsuNamespace}\"";

        StringAssert.Contains(StartTag(wire, "wsse:Security"), wsuDeclaration, wire);

        StringAssert.Contains(StartTag(wire, "soap:Body"), wsuDeclaration, wire);

        Assert.IsFalse(StartTag(wire, "soap:Envelope").Contains("xmlns:wsu=", StringComparison.Ordinal), wire);
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_1)]
    [DataRow(SoapProtocolType.SOAP_1_2)]
    public void SignedWire_DeclaresXmlnsWsseOnSecurity_NeverOnTheEnvelope_Test(SoapProtocolType protocol)
    {
        var wire = SignedWire(protocol);

        StringAssert.Contains(StartTag(wire, "wsse:Security"), $"xmlns:wsse=\"{WsSecurityTestSupport.WsseNamespace}\"", wire);

        Assert.IsFalse(StartTag(wire, "soap:Envelope").Contains("xmlns:wsse=", StringComparison.Ordinal), wire);
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_1)]
    [DataRow(SoapProtocolType.SOAP_1_2)]
    public void SignedWire_EmitsSignatureUnprefixedInTheDefaultXmlDsigNamespace_Test(SoapProtocolType protocol)
    {
        var wire = SignedWire(protocol);

        StringAssert.Contains(wire, DsigDefaultNamespaceDeclaration, wire);

        Assert.IsFalse(wire.Contains("<ds:Signature", StringComparison.Ordinal), wire);
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_1)]
    [DataRow(SoapProtocolType.SOAP_1_2)]
    public void SignedWire_SecurityHeaderChildren_AreBinarySecurityTokenThenTimestampThenSignature_Test(SoapProtocolType protocol)
    {
        var wire = SignedWire(protocol);

        var document = WsSecurityTestSupport.ParseWire(wire);
        var security = WsSecurityTestSupport.RequireNode(document, "//wsse:Security", WsSecurityTestSupport.Namespaces(document), "wsse:Security");

        var children = security.ChildNodes.Cast<XmlNode>().Select(node => node.LocalName).ToArray();

        CollectionAssert.AreEqual(new[] { "BinarySecurityToken", "Timestamp", "Signature" }, children, $"{string.Join(", ", children)} | {wire}");
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_1)]
    [DataRow(SoapProtocolType.SOAP_1_2)]
    public void SignedWire_EmptyElements_UseTheSelfClosingFormWithASpaceBeforeTheSlash_Test(SoapProtocolType protocol)
    {
        var wire = SignedWire(protocol);

        foreach (var name in SelfClosingElementNames)
        {
            Assert.IsTrue(Regex.IsMatch(wire, "<" + Regex.Escape(name) + @"\s[^<>]*\s/>"), $"{name} | {wire}");

            Assert.IsFalse(wire.Contains("></" + name + ">", StringComparison.Ordinal), $"{name} | {wire}");
        }
    }

    private static string SignedWire(SoapProtocolType protocol)
        => WsSecurityTestSupport.Wire(
            WsSecurityTestSupport.Build(protocol, HttpMethod.Post, WsSecurityTestSupport.Security()),
            $"Build {protocol}");

    private static string StartTag(string wire, string qualifiedName)
    {
        var match = Regex.Match(wire, "<" + Regex.Escape(qualifiedName) + @"(?:\s[^>]*)?>");

        Assert.IsTrue(match.Success, $"{qualifiedName} | {wire}");

        return match.Value;
    }
}
