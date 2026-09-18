
#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Mapping;
using SoapClientCallAssistTests.Soap12.Helpers.Mapper;
using SoapClientCallAssistTests.Soap12.Models;

namespace SoapClientCallAssistTests.Soap12.Tests.Mapper.Bind;

[TestClass]
public sealed class MapperBindFaultTests
{
    private readonly ISoapModelMapper _mapper = new SoapModelMapper();

    private static string Fault12(string reason = "processing failed", string detail = "<inner>context</inner>")
        => $"<soap:Envelope xmlns:soap=\"{MapperBindEnvelope.Soap12Ns}\"><soap:Body>"
           + "<soap:Fault>"
           + "<soap:Code><soap:Value>soap:Receiver</soap:Value></soap:Code>"
           + $"<soap:Reason><soap:Text xml:lang=\"en\">{reason}</soap:Text></soap:Reason>"
           + $"<soap:Detail>{detail}</soap:Detail>"
           + "</soap:Fault>"
           + "</soap:Body></soap:Envelope>";

    private static string Fault11(string reason = "processing failed", string detail = "<inner>context</inner>")
        => $"<soap:Envelope xmlns:soap=\"{MapperBindEnvelope.Soap11Ns}\"><soap:Body>"
           + "<soap:Fault>"
           + "<faultcode>soap:Server</faultcode>"
           + $"<faultstring>{reason}</faultstring>"
           + $"<detail>{detail}</detail>"
           + "</soap:Fault>"
           + "</soap:Body></soap:Envelope>";

    [TestMethod]
    public void FromResponse_Soap12FaultEnvelope_FailsWithTheFaultCode_Test()
    {
        var response = Fault12();

        Assert.IsTrue(MapperBindEnvelope.CarriesFaultElement(response, MapperBindEnvelope.Soap12Ns));

        var result = _mapper.FromResponse<MapperBindFlat>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.FailedWithCode(result, MapperBindAssert.FaultCode);
    }

    [TestMethod]
    public void FromResponse_Soap12FaultEnvelope_DoesNotReturnAnAllDefaultsObject_Test()
    {
        var response = Fault12();

        Assert.IsTrue(MapperBindEnvelope.CarriesFaultElement(response, MapperBindEnvelope.Soap12Ns));

        var result = _mapper.FromResponse<MapperBindFlat>(response, MapperBindEnvelope.Protocol12);

        Assert.IsFalse(result.IsSuccess);
        Assert.IsNull(result.Response);
    }

    [TestMethod]
    public void FromResponse_Soap11FaultEnvelope_FailsWithTheFaultCode_Test()
    {
        var response = Fault11();

        Assert.IsTrue(MapperBindEnvelope.CarriesFaultElement(response, MapperBindEnvelope.Soap11Ns));

        var result = _mapper.FromResponse<MapperBindFlat>(response, MapperBindEnvelope.Protocol11);

        MapperBindAssert.FailedWithCode(result, MapperBindAssert.FaultCode);
        Assert.IsNull(result.Response);
    }

    [TestMethod]
    public void FromResponse_Soap11FaultWhileTheCallerPassesTheSoap12Namespace_IsStillDetected_Test()
    {
        var response = Fault11();

        Assert.IsTrue(MapperBindEnvelope.CarriesFaultElement(response, MapperBindEnvelope.Soap11Ns));

        var result = _mapper.FromResponse<MapperBindFlat>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.FailedWithCode(result, MapperBindAssert.FaultCode);
    }

    [TestMethod]
    public void FromResponse_Soap12FaultWhileTheCallerPassesTheSoap11Namespace_IsStillDetected_Test()
    {
        var response = Fault12();

        Assert.IsTrue(MapperBindEnvelope.CarriesFaultElement(response, MapperBindEnvelope.Soap12Ns));

        var result = _mapper.FromResponse<MapperBindFlat>(response, MapperBindEnvelope.Protocol11);

        MapperBindAssert.FailedWithCode(result, MapperBindAssert.FaultCode);
    }

    [TestMethod]
    public void FromResponse_FaultIsDetectedBeforeBinding_EvenWhenThePayloadWouldBindCleanly_Test()
    {
        var response =
            $"<soap:Envelope xmlns:soap=\"{MapperBindEnvelope.Soap12Ns}\"><soap:Body>"
            + MapperBindEnvelope.Payload("Flat", "<Id>42</Id><Name>widget</Name>")
            + "<soap:Fault><soap:Code><soap:Value>soap:Sender</soap:Value></soap:Code></soap:Fault>"
            + "</soap:Body></soap:Envelope>";

        Assert.IsTrue(MapperBindEnvelope.CarriesFaultElement(response, MapperBindEnvelope.Soap12Ns));
        Assert.AreEqual(2, MapperBindEnvelope.CountBodyChildren(response, MapperBindEnvelope.Soap12Ns));

        var result = _mapper.FromResponse<MapperBindFlat>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.FailedWithCode(result, MapperBindAssert.FaultCode);
        Assert.IsNull(result.Response);
    }

    [TestMethod]
    public void FromResponse_FaultPlantedInTheHeader_IsIgnoredAndThePayloadBinds_Test()
    {
        var response =
            $"<soap:Envelope xmlns:soap=\"{MapperBindEnvelope.Soap12Ns}\">"
            + "<soap:Header><soap:Fault>"
            + "<soap:Code><soap:Value>soap:Receiver</soap:Value></soap:Code>"
            + "<soap:Reason><soap:Text xml:lang=\"en\">Account suspended</soap:Text></soap:Reason>"
            + "</soap:Fault></soap:Header>"
            + $"<soap:Body>{MapperBindEnvelope.Payload("Flat", "<Id>42</Id><Name>widget</Name>")}</soap:Body>"
            + "</soap:Envelope>";

        Assert.IsTrue(MapperBindEnvelope.CarriesFaultElement(response, MapperBindEnvelope.Soap12Ns));
        Assert.AreEqual(1, MapperBindEnvelope.CountBodyChildren(response, MapperBindEnvelope.Soap12Ns));

        var result = _mapper.FromResponse<MapperBindFlat>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        Assert.AreEqual(42, result.Response.Id);
        Assert.AreEqual("widget", result.Response.Name);
    }

    [TestMethod]
    public void FromResponse_FaultNestedInsideThePayload_IsNotAFault_Test()
    {
        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("Flat", "<Id>42</Id><soap:Fault xmlns:soap=\"" + MapperBindEnvelope.Soap12Ns + "\" />"));

        Assert.IsTrue(MapperBindEnvelope.CarriesFaultElement(response, MapperBindEnvelope.Soap12Ns));

        var result = _mapper.FromResponse<MapperBindFlat>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        Assert.AreEqual(42, result.Response.Id);
    }

    [TestMethod]
    public void FromResponse_PayloadElementNamedFaultOutsideTheSoapNamespace_IsNotAFault_Test()
    {
        var response = MapperBindEnvelope.Wrap12(
            $"<Fault xmlns=\"{MapperBindNs.Contract}\"><Id>42</Id></Fault>");

        Assert.IsFalse(MapperBindEnvelope.CarriesFaultElement(response, MapperBindEnvelope.Soap12Ns));
        Assert.IsFalse(MapperBindEnvelope.CarriesFaultElement(response, MapperBindEnvelope.Soap11Ns));

        var result = _mapper.FromResponse<MapperBindFaultNamedPayload>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        Assert.AreEqual(42, result.Response.Id);
    }
}
