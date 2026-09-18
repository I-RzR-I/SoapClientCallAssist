#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using System;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.Hygiene;

[TestClass]
public sealed class MessageLeakSweepCanaryTests
{

    private const string SecretText = "PLANTED-OBJECT-SECRET-3b9e7d";

    private const int NestingDepth = 10;

    private static readonly byte[] SecretBytes = { 0xDE, 0xAD, 0xBE, 0xEF, 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF, 0xFE, 0xED, 0xFA, 0xCE };

    [TestMethod]
    public void Sweep_WhenAByteArrayIsPlanted_SeesItAsBase64_Test()
        => SecretLeakAssert.CarriesSecret(
            Plant(SecretBytes),
            Convert.ToBase64String(SecretBytes),
            "");

    [TestMethod]
    public void Sweep_WhenAByteArrayIsPlanted_SeesItAsLowercaseHex_Test()
        => SecretLeakAssert.CarriesSecret(
            Plant(SecretBytes),
            Convert.ToHexString(SecretBytes).ToLowerInvariant(),
            "");

    [TestMethod]
    public void Sweep_WhenAByteArrayIsPlanted_SeesItAsUppercaseHex_Test()
        => SecretLeakAssert.CarriesSecret(
            Plant(SecretBytes),
            Convert.ToHexString(SecretBytes),
            "");

    [TestMethod]
    public void Sweep_WhenAByteArrayIsPlanted_SeesItInBitConverterForm_Test()
        => SecretLeakAssert.CarriesSecret(
            Plant(SecretBytes),
            BitConverter.ToString(SecretBytes),
            "");

    [TestMethod]
    public void Sweep_WhenAnArraySegmentOfBytesIsPlanted_SeesItAsBase64_Test()
        => SecretLeakAssert.CarriesSecret(
            Plant(new ArraySegment<byte>(SecretBytes, 2, 8)),
            Convert.ToBase64String(SecretBytes, 2, 8),
            "");

    [TestMethod]
    public void Sweep_WhenAReadOnlyMemoryOfBytesIsPlanted_SeesItAsBase64_Test()
        => SecretLeakAssert.CarriesSecret(
            Plant(new ReadOnlyMemory<byte>(SecretBytes)),
            Convert.ToBase64String(SecretBytes),
            "");

    [TestMethod]
    public void Sweep_WhenAStringBuilderIsPlanted_SeesItsContents_Test()
        => SecretLeakAssert.CarriesSecret(
            Plant(new StringBuilder("prefix ").Append(SecretText).Append(" suffix")),
            SecretText,
            "");

    [TestMethod]
    public void Sweep_WhenAMemoryStreamIsPlanted_SeesItsBytesAsBase64_Test()
        => SecretLeakAssert.CarriesSecret(
            Plant(new MemoryStream(SecretBytes)),
            Convert.ToBase64String(SecretBytes),
            "");

    [TestMethod]
    public void Sweep_WhenAnExceptionNestedTenDeepIsPlanted_SeesTheInnermostMessage_Test()
        => SecretLeakAssert.CarriesSecret(
            Plant(Nested(NestingDepth)),
            SecretText,
            "");

    [TestMethod]
    public void Sweep_WhenAnXElementIsPlanted_SeesAnAttributeBuriedPastTheDepthCap_Test()
        => SecretLeakAssert.CarriesSecret(
            Plant(DeepElement(NestingDepth)),
            SecretText,
            "");

    [TestMethod]
    public void Sweep_WhenAnXmlNodeIsPlanted_SeesAnAttributeBuriedPastTheDepthCap_Test()
    {
        var document = new XmlDocument { XmlResolver = null };
        document.LoadXml(DeepElement(NestingDepth).ToString(SaveOptions.DisableFormatting));

        SecretLeakAssert.CarriesSecret(
            Plant(document.DocumentElement),
            SecretText,
            "");
    }

    [TestMethod]
    public void Sweep_WhenASecureStringIsPlanted_ReportsALeakByShape_Test()
    {
        using var secure = new SecureString();

        foreach (var character in SecretText)
            secure.AppendChar(character);

        secure.MakeReadOnly();

        var planted = Plant(secure);

        Assert.ThrowsException<AssertFailedException>(
            () => SecretLeakAssert.CarriesNoSecret(planted, "signing failure"),
            MessageLeakSweep.Render(planted));
    }

    [TestMethod]
    public void Sweep_WhenALazyIsPlanted_NeverForcesItsValue_Test()
    {
        var forced = false;

        var lazy = new Lazy<string>(() =>
        {
            forced = true;

            return SecretText;
        });

        var swept = MessageLeakSweep.Render(Plant(lazy));

        Assert.IsFalse(forced, swept);

        StringAssert.Contains(swept, "<Lazy<T>.Value not forced>");
    }

    [TestMethod]
    public void Sweep_WhenAnIncompleteTaskIsPlanted_NeverBlocksOnItsResult_Test()
    {
        var completion = new TaskCompletionSource<string>();

        var stopwatch = Stopwatch.StartNew();
        var swept = MessageLeakSweep.Render(Plant(completion.Task));
        stopwatch.Stop();

        completion.SetResult(SecretText);

        Assert.IsTrue(stopwatch.Elapsed < TimeSpan.FromSeconds(5), $"{stopwatch.Elapsed} | {swept}");

        StringAssert.Contains(swept, "<Task<T>.Result not read>");
    }

    [TestMethod]
    public void CarriesNoSecret_WithAnExtraForbiddenByteSecretPlantedAsHexText_GoesRedAndStaysGreenWithoutIt_Test()
    {
        var leaking = new StubSoapMessageSigner(Convert.ToHexString(SecretBytes).ToLowerInvariant())
            .Sign(WsSecurityTestSupport.DefaultBody(), WsSecurityTestSupport.Security());

        Assert.ThrowsException<AssertFailedException>(
            () => SecretLeakAssert.CarriesNoSecret(leaking, "", ForbiddenSecret.OfBytes("the session key", SecretBytes)),
            MessageLeakSweep.Render(leaking));

        SecretLeakAssert.CarriesNoSecret(
            leaking,
            "");
    }

    [TestMethod]
    public void CarriesNoSecret_WithAnExtraForbiddenTextSecretPlantedAsBase64OfItsUtf8_GoesRed_Test()
    {
        var leaking = new StubSoapMessageSigner(Convert.ToBase64String(Encoding.UTF8.GetBytes(SecretText)))
            .Sign(WsSecurityTestSupport.DefaultBody(), WsSecurityTestSupport.Security());

        Assert.ThrowsException<AssertFailedException>(
            () => SecretLeakAssert.CarriesNoSecret(leaking, "", ForbiddenSecret.OfText("the password", SecretText)),
            MessageLeakSweep.Render(leaking));
    }

    [TestMethod]
    public void CarriesNoSecret_WithAnExtraForbiddenSecretPlantedAsBytes_GoesRedOnEveryRendering_Test()
    {
        var leaking = Plant(SecretBytes);

        Assert.ThrowsException<AssertFailedException>(
            () => SecretLeakAssert.CarriesNoSecret(leaking, "signing failure", ForbiddenSecret.OfBytes("the session key", SecretBytes)),
            MessageLeakSweep.Render(leaking));

        SecretLeakAssert.CarriesNoSecret(
            Plant(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 }),
            "signing failure",
            ForbiddenSecret.OfBytes("the session key", SecretBytes));
    }

    private static IResult Plant(object planted)
        => StubSoapMessageSigner.Planting(planted).Sign(WsSecurityTestSupport.DefaultBody(), WsSecurityTestSupport.Security());

    private static Exception Nested(int depth)
    {
        Exception current = new InvalidOperationException("innermost " + SecretText);

        for (var level = 1; level < depth; level++)
            current = new InvalidOperationException($"level {level}", current);

        return current;
    }

    private static XElement DeepElement(int depth)
    {
        var innermost = new XElement("leaf", new XAttribute("secret", SecretText));
        var current = innermost;

        for (var level = 1; level < depth; level++)
            current = new XElement("level" + level, current);

        return current;
    }
}
