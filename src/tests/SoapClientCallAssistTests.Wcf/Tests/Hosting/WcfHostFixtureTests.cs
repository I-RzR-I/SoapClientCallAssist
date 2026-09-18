using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssistTests.Wcf.Helpers;
using System;
using System.IO;
using System.Security.Principal;

namespace SoapClientCallAssistTests.Wcf.Tests.Hosting;

[TestClass]
public sealed class WcfHostFixtureTests
{

    private const string TraceFileName = "SoapClientCallAssistTests.Wcf.svclog";

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void BaseAddress_IsATemporaryListenAddressOnPort80_Test()
    {
        var baseAddress = WcfHostFixture.BaseAddress;

        TestContext.WriteLine($"BaseAddress={baseAddress}");

        Assert.AreEqual(80, baseAddress.Port);
        Assert.IsTrue(
            new Uri(WcfHostFixture.ListenPrefix).IsBaseOf(baseAddress),
            baseAddress.ToString());
    }

    [TestMethod]
    public void Process_ElevationIsReported_Test()
    {
        using var identity = WindowsIdentity.GetCurrent();

        var isElevated = new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);

        TestContext.WriteLine($"IsElevated={isElevated}");
    }

    [TestMethod]
    public void Certificates_LeaveNoKeyFilesInTheUserKeyStore_Test()
    {
        var now = CngKeyStoreLitter.CountKeyFiles();

        TestContext.WriteLine($"KeyFiles before={WcfHostFixture.KeyFilesAtStart} now={now} dir={CngKeyStoreLitter.KeyDirectory}");

        Assert.AreEqual(
            WcfHostFixture.KeyFilesAtStart,
            now);
    }

    [TestMethod]
    public void Certificates_AreDistinctAndCarryPrivateKeys_Test()
    {
        Assert.AreNotEqual(WcfHostFixture.TrustedCertificate.Thumbprint, WcfHostFixture.UntrustedCertificate.Thumbprint);
        Assert.IsTrue(WcfHostFixture.TrustedCertificate.Certificate.HasPrivateKey);
        Assert.IsTrue(WcfHostFixture.UntrustedCertificate.Certificate.HasPrivateKey);
    }

    [TestMethod]
    public void ServiceModelTrace_IsWrittenUnderTheTestOutputDirectory_Test()
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, TraceFileName);

        TestContext.WriteLine($"svclog={path}");

        Assert.IsTrue(File.Exists(path), path);
    }
}
