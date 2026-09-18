using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssistTests.Soap12.Helpers.Protocol;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using System;
using System.Linq;
using System.Net.Http;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests.Protocol;

[TestClass]
public sealed class NegativePathTests
{
    private const string MethodNotAllowedMessage = "The supplied HTTP method (PUT) is not allowed.";

    private const string MissingUriMessage = "SOAP Uri is mandatory.";

    private const string GenericBuildFailureMessage =
        "An error occurred while trying to validate and build SOAP request message.";

    private const string NullRequestValidationMessage = "SOAP request object can not be null.";

    private const string Soap12BuildErrorCode = "ER-S12-BSR";

    private const string Soap12BuildErrorMessage = "An error occurred while trying to build a SOAP 1.2 request.";

    [TestMethod]
    public void BuildRequest_WithDisallowedHttpMethod_FailsAndNamesTheMethod_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var result = client.BuildRequest(
            HttpMethod.Put,
            SoapServiceFixture.ServiceUri,
            NegativeTestSupport.Bodies("EchoValue", ("value", "abc"), ("count", "7")));

        Assert.IsFalse(result.IsSuccess, NegativeTestSupport.Describe(result));
        Assert.IsNull(result.Response);

        var messages = NegativeTestSupport.Messages(result);

        Assert.AreEqual(1, messages.Count, NegativeTestSupport.Describe(result));
        Assert.AreEqual(MethodNotAllowedMessage, messages[0].Message.Info);

        Assert.IsNull(messages[0].Key, "DEFECT-VALIDATION-CODE-LOST");
    }

    [TestMethod]
    [Ignore("DEFECT-VALIDATION-CODE-LOST: unpins when fixed")]
    public void BuildRequest_WithDisallowedHttpMethod_ShouldSurfaceTheValidationCode_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var result = client.BuildRequest(
            HttpMethod.Put,
            SoapServiceFixture.ServiceUri,
            NegativeTestSupport.Bodies("EchoValue", ("value", "abc")));

        var messages = NegativeTestSupport.Messages(result);

        Assert.AreEqual("V_BEC_VR_002", messages[0].Key);
    }

    [TestMethod]
    public void BuildRequest_WithNullUri_FailsAndSaysTheUriIsMandatory_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var result = client.BuildRequest(
            HttpMethod.Post,
            null,
            NegativeTestSupport.Bodies("EchoValue", ("value", "abc")));

        Assert.IsFalse(result.IsSuccess, NegativeTestSupport.Describe(result));
        Assert.IsNull(result.Response);

        var messages = NegativeTestSupport.Messages(result);

        Assert.AreEqual(1, messages.Count, NegativeTestSupport.Describe(result));
        Assert.AreEqual(MissingUriMessage, messages[0].Message.Info);

        Assert.IsNull(messages[0].Key);
    }

    [TestMethod]
    public void BuildRequest_WithNullRequestDto_FailsWithAWrappedNullReferenceRatherThanValidation_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var result = client.BuildRequest(HttpMethod.Post, null!);

        Assert.IsFalse(result.IsSuccess, NegativeTestSupport.Describe(result));

        var messages = NegativeTestSupport.Messages(result);

        Assert.AreEqual(2, messages.Count, NegativeTestSupport.Describe(result));

        Assert.AreEqual(Soap12BuildErrorCode, messages[0].Key);
        Assert.AreEqual(Soap12BuildErrorMessage, messages[0].Message.Info);

        Assert.IsTrue(messages.Any(message => message.MessageType.ToString() == "Exception"), NegativeTestSupport.Describe(result));

        Assert.IsFalse(messages.Any(message => message.Message?.Info == NullRequestValidationMessage));
    }

    [TestMethod]
    [Ignore("DEFECT-NULL-DTO-NRE: unpins when fixed")]
    public void BuildRequest_WithNullRequestDto_ShouldReportTheNullRequestValidationFailure_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var result = client.BuildRequest(HttpMethod.Post, null!);

        var messages = NegativeTestSupport.Messages(result);

        Assert.IsTrue(messages.Any(message => message.Message?.Info == NullRequestValidationMessage));
    }

    [TestMethod]
    public void BuildRequest_WithEmptyBodiesOnGet_FailsWithTheCatchAllBuildMessage_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var result = client.BuildRequest(HttpMethod.Get, SoapServiceFixture.ServiceUri, Array.Empty<XElement>());

        Assert.IsFalse(result.IsSuccess, NegativeTestSupport.Describe(result));
        Assert.IsNull(result.Response);

        var messages = NegativeTestSupport.Messages(result);

        Assert.AreEqual(1, messages.Count, NegativeTestSupport.Describe(result));

        Assert.AreEqual(GenericBuildFailureMessage, messages[0].Message.Info);
        Assert.IsNull(messages[0].Key);
    }

    [TestMethod]
    public void BuildRequest_FailureMessage_IsTheSameForEmptyBodiesAndNullBodies_Test()
    {
        var emptyBodiesResult = SoapClientFactoryHelper.CreateSoap12Client()
            .BuildRequest(HttpMethod.Get, SoapServiceFixture.ServiceUri, Array.Empty<XElement>());

        var nullBodiesResult = SoapClientFactoryHelper.CreateSoap12Client()
            .BuildRequest(HttpMethod.Post, SoapServiceFixture.ServiceUri, null);

        Assert.IsFalse(emptyBodiesResult.IsSuccess);
        Assert.IsFalse(nullBodiesResult.IsSuccess);

        Assert.AreEqual(NegativeTestSupport.FirstMessageInfo(emptyBodiesResult), NegativeTestSupport.FirstMessageInfo(nullBodiesResult));
    }

    [TestMethod]
    [Ignore("DEFECT-BUILD-CAUSE-OPAQUE: unpins when fixed")]
    public void BuildRequest_FailureMessage_ShouldDistinguishEmptyBodiesFromNullBodies_Test()
    {
        var emptyBodiesResult = SoapClientFactoryHelper.CreateSoap12Client()
            .BuildRequest(HttpMethod.Get, SoapServiceFixture.ServiceUri, Array.Empty<XElement>());

        var nullBodiesResult = SoapClientFactoryHelper.CreateSoap12Client()
            .BuildRequest(HttpMethod.Post, SoapServiceFixture.ServiceUri, null);

        Assert.AreNotEqual(NegativeTestSupport.FirstMessageInfo(emptyBodiesResult), NegativeTestSupport.FirstMessageInfo(nullBodiesResult));
    }
}
