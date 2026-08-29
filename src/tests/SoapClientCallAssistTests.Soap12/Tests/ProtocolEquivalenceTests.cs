using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssistTests.Soap12.Helpers;
using System;
using System.Net.Http;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests;

[TestClass]
public sealed class ProtocolEquivalenceTests
{

    [TestMethod]
    public void DisallowedHttpMethod_IsReportedIdenticallyByBothProtocolClients()
    {
        var soap12Result = SoapClientFactoryHelper.CreateSoap12Client()
            .BuildRequest(HttpMethod.Put, SoapServiceFixture.ServiceUri, NegativeTestSupport.Bodies("EchoValue", ("value", "abc")));

        var soap11Result = SoapClientFactoryHelper.CreateSoap11Client()
            .BuildRequest(HttpMethod.Put, SoapServiceFixture.ServiceUri, NegativeTestSupport.Bodies("EchoValue", ("value", "abc")));

        AssertEquivalentFailures(soap12Result, soap11Result, "The supplied HTTP method (PUT) is not allowed.");
    }

    [TestMethod]
    public void MissingUri_IsReportedIdenticallyByBothProtocolClients()
    {
        var soap12Result = SoapClientFactoryHelper.CreateSoap12Client()
            .BuildRequest(HttpMethod.Post, null, NegativeTestSupport.Bodies("EchoValue", ("value", "abc")));

        var soap11Result = SoapClientFactoryHelper.CreateSoap11Client()
            .BuildRequest(HttpMethod.Post, null, NegativeTestSupport.Bodies("EchoValue", ("value", "abc")));

        AssertEquivalentFailures(soap12Result, soap11Result, "SOAP Uri is mandatory.");
    }

    [TestMethod]
    public void EmptyBodiesOnGet_IsReportedIdenticallyByBothProtocolClients()
    {
        var soap12Result = SoapClientFactoryHelper.CreateSoap12Client()
            .BuildRequest(HttpMethod.Get, SoapServiceFixture.ServiceUri, Array.Empty<XElement>());

        var soap11Result = SoapClientFactoryHelper.CreateSoap11Client()
            .BuildRequest(HttpMethod.Get, SoapServiceFixture.ServiceUri, Array.Empty<XElement>());

        AssertEquivalentFailures(
            soap12Result,
            soap11Result,
            "An error occurred while trying to validate and build SOAP request message.");
    }

    [TestMethod]
    public void NullRequestDto_IsReportedWithAProtocolSpecificCode()
    {
        var soap12Result = SoapClientFactoryHelper.CreateSoap12Client()
            .BuildRequest(HttpMethod.Post, null!);

        var soap11Result = SoapClientFactoryHelper.CreateSoap11Client()
            .BuildRequest(HttpMethod.Post, null!);

        Assert.IsFalse(soap12Result.IsSuccess, "A null request object must fail under SOAP 1.2.");
        Assert.IsFalse(soap11Result.IsSuccess, "A null request object must fail under SOAP 1.1.");

        var soap12Message = NegativeTestSupport.Messages(soap12Result)[0];
        var soap11Message = NegativeTestSupport.Messages(soap11Result)[0];

        Assert.AreEqual("ER-S12-BSR", soap12Message.Key, "The SOAP 1.2 client must raise its own build error code.");
        Assert.AreEqual("ER-S11-BSR", soap11Message.Key, "The SOAP 1.1 client must raise its own build error code.");

        Assert.AreEqual(
            "An error occurred while trying to build a SOAP 1.2 request.",
            soap12Message.Message.Info,
            "The SOAP 1.2 message must name its own protocol version.");

        Assert.AreEqual(
            "An error occurred while trying to build a SOAP 1.1 request.",
            soap11Message.Message.Info,
            "The SOAP 1.1 message must name its own protocol version.");
    }

    private static void AssertEquivalentFailures(
        IResult<HttpRequestMessage> soap12Result,
        IResult<HttpRequestMessage> soap11Result,
        string expectedMessage)
    {
        Assert.IsFalse(soap12Result.IsSuccess, $"The SOAP 1.2 build was expected to fail. Got: {NegativeTestSupport.Describe(soap12Result)}");
        Assert.IsFalse(soap11Result.IsSuccess, $"The SOAP 1.1 build was expected to fail. Got: {NegativeTestSupport.Describe(soap11Result)}");

        var soap12Messages = NegativeTestSupport.Messages(soap12Result);
        var soap11Messages = NegativeTestSupport.Messages(soap11Result);

        Assert.AreEqual(expectedMessage, soap12Messages[0].Message.Info, "Unexpected SOAP 1.2 failure text.");

        Assert.AreEqual(
            soap12Messages.Count,
            soap11Messages.Count,
            "Both protocol clients must report this shared failure with the same number of messages.");

        Assert.AreEqual(
            soap12Messages[0].Message.Info,
            soap11Messages[0].Message.Info,
            "This failure path is protocol agnostic, so both clients must produce the same text.");

        Assert.AreEqual(
            soap12Messages[0].Key,
            soap11Messages[0].Key,
            "This failure path is protocol agnostic, so both clients must produce the same code.");
    }
}
