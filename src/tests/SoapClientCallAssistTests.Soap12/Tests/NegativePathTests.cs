using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssistTests.Soap12.Helpers;
using System;
using System.Linq;
using System.Net.Http;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests;

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

        Assert.IsFalse(result.IsSuccess, $"PUT is not an allowed SOAP binding, so the build must fail. Got: {NegativeTestSupport.Describe(result)}");
        Assert.IsNull(result.Response, "A rejected build must not hand back a request message.");

        var messages = NegativeTestSupport.Messages(result);

        Assert.AreEqual(1, messages.Count, $"Expected exactly one message. Got: {NegativeTestSupport.Describe(result)}");
        Assert.AreEqual(MethodNotAllowedMessage, messages[0].Message.Info, "The failure must name the rejected method.");

        Assert.IsNull(
            messages[0].Key,
            "Pinning current behaviour: the validation code is dropped. If this now carries a key, DEFECT-VALIDATION-CODE-LOST is fixed and its companion test should be un-ignored.");
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

        Assert.AreEqual(
            "V_BEC_VR_002",
            messages[0].Key,
            "A caller must be able to identify a disallowed HTTP method from the code, not from the message text.");
    }

    [TestMethod]
    public void BuildRequest_WithNullUri_FailsAndSaysTheUriIsMandatory_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var result = client.BuildRequest(
            HttpMethod.Post,
            null,
            NegativeTestSupport.Bodies("EchoValue", ("value", "abc")));

        Assert.IsFalse(result.IsSuccess, $"A null endpoint must fail the build. Got: {NegativeTestSupport.Describe(result)}");
        Assert.IsNull(result.Response, "A rejected build must not hand back a request message.");

        var messages = NegativeTestSupport.Messages(result);

        Assert.AreEqual(1, messages.Count, $"Expected exactly one message. Got: {NegativeTestSupport.Describe(result)}");
        Assert.AreEqual(MissingUriMessage, messages[0].Message.Info, "The failure must say which input was missing.");

        Assert.IsNull(messages[0].Key, "Pinning current behaviour: the validation code is dropped on this path too.");
    }

    [TestMethod]
    public void BuildRequest_WithNullRequestDto_FailsWithAWrappedNullReferenceRatherThanValidation_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var result = client.BuildRequest(HttpMethod.Post, null!);

        Assert.IsFalse(result.IsSuccess, $"A null request object must fail the build. Got: {NegativeTestSupport.Describe(result)}");

        var messages = NegativeTestSupport.Messages(result);

        Assert.AreEqual(2, messages.Count, $"Expected the failure plus the captured exception. Got: {NegativeTestSupport.Describe(result)}");

        Assert.AreEqual(Soap12BuildErrorCode, messages[0].Key, "The SOAP 1.2 build error code must reach the caller on this path.");
        Assert.AreEqual(Soap12BuildErrorMessage, messages[0].Message.Info, "The first message must be the build failure.");

        Assert.IsTrue(
            messages.Any(message => message.MessageType.ToString() == "Exception"),
            $"An exception-typed message must be attached so the cause is diagnosable. Got: {NegativeTestSupport.Describe(result)}");

        Assert.IsFalse(
            messages.Any(message => message.Message?.Info == NullRequestValidationMessage),
            "Pinning current behaviour: the null-request validation branch is never reached.");
    }

    [TestMethod]
    [Ignore("DEFECT-NULL-DTO-NRE: unpins when fixed")]
    public void BuildRequest_WithNullRequestDto_ShouldReportTheNullRequestValidationFailure_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var result = client.BuildRequest(HttpMethod.Post, null!);

        var messages = NegativeTestSupport.Messages(result);

        Assert.IsTrue(
            messages.Any(message => message.Message?.Info == NullRequestValidationMessage),
            "A null request object is a validation failure, not an internal error.");
    }

    [TestMethod]
    public void BuildRequest_WithEmptyBodiesOnGet_FailsWithTheCatchAllBuildMessage_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var result = client.BuildRequest(HttpMethod.Get, SoapServiceFixture.ServiceUri, Array.Empty<XElement>());

        Assert.IsFalse(result.IsSuccess, $"A GET with no body has no operation to address. Got: {NegativeTestSupport.Describe(result)}");
        Assert.IsNull(result.Response, "A rejected build must not hand back a request message.");

        var messages = NegativeTestSupport.Messages(result);

        Assert.AreEqual(1, messages.Count, $"Expected exactly one message. Got: {NegativeTestSupport.Describe(result)}");

        Assert.AreEqual(GenericBuildFailureMessage, messages[0].Message.Info, "Pinning the catch-all message produced for this input.");
        Assert.IsNull(messages[0].Key, "Pinning current behaviour: the internal error code is dropped as well.");
    }

    [TestMethod]
    public void BuildRequest_FailureMessage_IsTheSameForEmptyBodiesAndNullBodies_Test()
    {
        var emptyBodiesResult = SoapClientFactoryHelper.CreateSoap12Client()
            .BuildRequest(HttpMethod.Get, SoapServiceFixture.ServiceUri, Array.Empty<XElement>());

        var nullBodiesResult = SoapClientFactoryHelper.CreateSoap12Client()
            .BuildRequest(HttpMethod.Post, SoapServiceFixture.ServiceUri, null);

        Assert.IsFalse(emptyBodiesResult.IsSuccess, "The empty-bodies build was expected to fail.");
        Assert.IsFalse(nullBodiesResult.IsSuccess, "The null-bodies build was expected to fail.");

        Assert.AreEqual(
            NegativeTestSupport.FirstMessageInfo(emptyBodiesResult),
            NegativeTestSupport.FirstMessageInfo(nullBodiesResult),
            "Pinning current behaviour: two different input defects are indistinguishable to the caller.");
    }

    [TestMethod]
    [Ignore("DEFECT-BUILD-CAUSE-OPAQUE: unpins when fixed")]
    public void BuildRequest_FailureMessage_ShouldDistinguishEmptyBodiesFromNullBodies_Test()
    {
        var emptyBodiesResult = SoapClientFactoryHelper.CreateSoap12Client()
            .BuildRequest(HttpMethod.Get, SoapServiceFixture.ServiceUri, Array.Empty<XElement>());

        var nullBodiesResult = SoapClientFactoryHelper.CreateSoap12Client()
            .BuildRequest(HttpMethod.Post, SoapServiceFixture.ServiceUri, null);

        Assert.AreNotEqual(
            NegativeTestSupport.FirstMessageInfo(emptyBodiesResult),
            NegativeTestSupport.FirstMessageInfo(nullBodiesResult),
            "Two unrelated input defects must be reported distinguishably.");
    }
}
