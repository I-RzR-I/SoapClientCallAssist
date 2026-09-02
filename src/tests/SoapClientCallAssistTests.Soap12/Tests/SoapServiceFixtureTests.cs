using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace SoapClientCallAssistTests.Soap12.Tests;

[TestClass]
public sealed class SoapServiceFixtureTests
{

    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);

    [TestMethod]
    public void BaseAddress_IsLoopbackOnAnAssignedPort_Test()
    {
        var baseAddress = SoapServiceFixture.BaseAddress;

        Assert.IsNotNull(baseAddress, "The fixture published no base address.");
        Assert.IsTrue(baseAddress.Port > 0, $"Expected an assigned port but got {baseAddress.Port}.");
        Assert.AreEqual(IPAddress.Loopback.ToString(), baseAddress.Host, "The service must bind loopback only.");
    }

    [TestMethod]
    public void ServiceUri_IsTheServicePathUnderTheBaseAddress_Test()
    {
        Assert.AreEqual(
            SoapServiceFixture.BaseAddress.Port,
            SoapServiceFixture.ServiceUri.Port,
            "The service URI must address the same listener as the base address.");

        Assert.AreEqual("/ServiceSvc.svc", SoapServiceFixture.ServiceUri.AbsolutePath);
    }

    [TestMethod]
    public void Recorder_IsResolvedFromTheRunningService_Test()
        => Assert.IsNotNull(SoapServiceFixture.Recorder, "The fixture published no request recorder.");

    [TestMethod]
    public async Task Health_RespondsOk_Test()
    {
        using var client = new HttpClient { Timeout = RequestTimeout };

        using var response = await client.GetAsync(new Uri(SoapServiceFixture.BaseAddress, "health"));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
}
