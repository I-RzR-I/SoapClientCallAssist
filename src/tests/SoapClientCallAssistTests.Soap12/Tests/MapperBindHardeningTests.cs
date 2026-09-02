
#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Mapping;
using SoapClientCallAssistTests.Soap12.Helpers;
using SoapClientCallAssistTests.Soap12.Models;

namespace SoapClientCallAssistTests.Soap12.Tests;

[TestClass]
public sealed class MapperBindHardeningTests
{
    private const int MaxXmlDepth = 64;

    private const int DepthOffsetToFirstChainElement = 4;

    private const string DtdMarker = "ZZQ-DTD-MARKER-4a2d";

    private ISoapModelMapper _mapper;

    [TestInitialize]
    public void Initialize() => _mapper = new SoapModelMapper();

    private static string DeepResponse(int chainLength)
        => MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload(
                "Deep",
                "<Id>5</Id><Filler>" + MapperBindEnvelope.NestedChain(chainLength, "leaf") + "</Filler>"));

    [TestMethod]
    public void FromResponse_ResponseCarryingADtd_IsRejected_Test()
    {

        var response =
            "<?xml version=\"1.0\"?>"
            + $"<!DOCTYPE Envelope [<!ENTITY marker \"{DtdMarker}\">]>"
            + $"<soap:Envelope xmlns:soap=\"{MapperBindEnvelope.Soap12Ns}\"><soap:Body>"
            + MapperBindEnvelope.Payload("Flat", "<Id>42</Id>")
            + "</soap:Body></soap:Envelope>";


        Assert.IsTrue(response.Contains("<!DOCTYPE"), "The fixture must really carry a DTD.");


        var result = _mapper.FromResponse<MapperBindFlat>(response, MapperBindEnvelope.Protocol12);


        MapperBindAssert.FailedWithCode(result, MapperBindAssert.ResponseCode);
        Assert.IsNull(result.Response);
    }

    [TestMethod]
    public void FromResponse_ResponseCarryingADtd_DoesNotEchoTheDtdContent_Test()
    {

        var response =
            "<?xml version=\"1.0\"?>"
            + $"<!DOCTYPE Envelope [<!ENTITY marker \"{DtdMarker}\">]>"
            + $"<soap:Envelope xmlns:soap=\"{MapperBindEnvelope.Soap12Ns}\"><soap:Body>"
            + MapperBindEnvelope.Payload("Flat", "<Id>42</Id>")
            + "</soap:Body></soap:Envelope>";

        Assert.IsTrue(response.Contains(DtdMarker), "The fixture must really carry the DTD marker.");


        var result = _mapper.FromResponse<MapperBindFlat>(response, MapperBindEnvelope.Protocol12);


        MapperBindAssert.FailedWithCode(result, MapperBindAssert.ResponseCode);
        MapperBindAssert.MessagesDoNotContain(result, DtdMarker);
    }

    [TestMethod]
    public void FromResponse_ExternalEntityReference_IsRejectedWithoutResolvingAnything_Test()
    {

        var response =
            "<?xml version=\"1.0\"?>"
            + "<!DOCTYPE Envelope [<!ENTITY xxe SYSTEM \"file:///etc/passwd\">]>"
            + $"<soap:Envelope xmlns:soap=\"{MapperBindEnvelope.Soap12Ns}\"><soap:Body>"
            + $"<Flat xmlns=\"{MapperBindNs.Contract}\"><Name>&xxe;</Name></Flat>"
            + "</soap:Body></soap:Envelope>";

        Assert.IsTrue(response.Contains("SYSTEM"), "The fixture must really declare an external entity.");


        var result = _mapper.FromResponse<MapperBindFlat>(response, MapperBindEnvelope.Protocol12);


        MapperBindAssert.FailedWithCode(result, MapperBindAssert.ResponseCode);
        MapperBindAssert.MessagesDoNotContain(result, "etc/passwd", "root:");
    }

    [TestMethod]
    public void FromResponse_BillionLaughsPayload_IsRejectedRatherThanExpanded_Test()
    {

        var response = MapperBindEnvelope.BillionLaughs(9, 10);

        Assert.IsTrue(response.Contains("<!ENTITY lol9"), "The fixture must really chain nine entities.");
        Assert.IsTrue(response.Contains("&lol9;"), "The fixture must really reference the top entity.");


        var result = _mapper.FromResponse<MapperBindFlat>(response, MapperBindEnvelope.Protocol12);


        MapperBindAssert.FailedWithCode(result, MapperBindAssert.ResponseCode);
        Assert.IsNull(result.Response);
    }


    [TestMethod]
    public void FromResponse_NestingJustInsideTheDepthCap_IsAccepted_Test()
    {

        var chainLength = MaxXmlDepth - DepthOffsetToFirstChainElement;
        var response = DeepResponse(chainLength);

        var result = _mapper.FromResponse<MapperBindDeep>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        Assert.AreEqual(5, result.Response.Id);
    }

    [TestMethod]
    public void FromResponse_NestingAtTheDepthCap_IsRejected_Test()
    {

        var chainLength = MaxXmlDepth - DepthOffsetToFirstChainElement + 1;
        var response = DeepResponse(chainLength);

        var result = _mapper.FromResponse<MapperBindDeep>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.FailedWithCode(result, MapperBindAssert.ResponseCode);
        Assert.IsNull(result.Response);
    }

    [TestMethod]
    public void FromResponse_DeeplyNestedResponse_IsRejectedRatherThanOverflowingTheStack_Test()
    {

        var response = DeepResponse(5000);

        var result = _mapper.FromResponse<MapperBindDeep>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.FailedWithCode(result, MapperBindAssert.ResponseCode);
        Assert.IsNull(result.Response);
    }



    [TestMethod]
    public void FromResponse_ResponseLargerThanTheDocumentCap_IsRefusedBeforeAnyTreeIsBuilt_Test()
    {
        const int maxDocumentCharacters = 8 * 1024 * 1024;
        var response = new string('x', maxDocumentCharacters + 1);

        Assert.IsTrue(
            response.Length > maxDocumentCharacters,
            "The fixture must really exceed the documented character cap.");


        var result = _mapper.FromResponse<MapperBindFlat>(response, MapperBindEnvelope.Protocol12);


        MapperBindAssert.FailedWithCode(result, MapperBindAssert.ResponseCode);
        Assert.IsNull(result.Response);
    }

}
