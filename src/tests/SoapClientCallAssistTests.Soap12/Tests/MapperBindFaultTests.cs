
#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Mapping;
using SoapClientCallAssistTests.Soap12.Helpers;
using SoapClientCallAssistTests.Soap12.Models;

namespace SoapClientCallAssistTests.Soap12.Tests;

[TestClass]
public sealed class MapperBindFaultTests
{
    private ISoapModelMapper _mapper;

    [TestInitialize]
    public void Initialize() => _mapper = new SoapModelMapper();

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

        Assert.IsTrue(
            MapperBindEnvelope.CarriesFaultElement(response, MapperBindEnvelope.Soap12Ns),
            "The fixture must really carry a SOAP 1.2 Fault element.");

        var result = _mapper.FromResponse<MapperBindFlat>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.FailedWithCode(result, MapperBindAssert.FaultCode);
    }

    [TestMethod]
    public void FromResponse_Soap12FaultEnvelope_DoesNotReturnAnAllDefaultsObject_Test()
    {
        var response = Fault12();

        Assert.IsTrue(
            MapperBindEnvelope.CarriesFaultElement(response, MapperBindEnvelope.Soap12Ns),
            "The fixture must really carry a SOAP 1.2 Fault element.");

        var result = _mapper.FromResponse<MapperBindFlat>(response, MapperBindEnvelope.Protocol12);

        Assert.IsFalse(result.IsSuccess);
        Assert.IsNull(result.Response, "A fault must not hand back an instance at all.");
    }

    [TestMethod]
    public void FromResponse_Soap11FaultEnvelope_FailsWithTheFaultCode_Test()
    {
        var response = Fault11();

        Assert.IsTrue(
            MapperBindEnvelope.CarriesFaultElement(response, MapperBindEnvelope.Soap11Ns),
            "The fixture must really carry a SOAP 1.1 Fault element.");

        var result = _mapper.FromResponse<MapperBindFlat>(response, MapperBindEnvelope.Protocol11);

        MapperBindAssert.FailedWithCode(result, MapperBindAssert.FaultCode);
        Assert.IsNull(result.Response);
    }

    [TestMethod]
    public void FromResponse_Soap11FaultWhileTheCallerPassesTheSoap12Namespace_IsStillDetected_Test()
    {
        var response = Fault11();

        Assert.IsTrue(
            MapperBindEnvelope.CarriesFaultElement(response, MapperBindEnvelope.Soap11Ns),
            "The fixture must really carry a SOAP 1.1 Fault element.");

        var result = _mapper.FromResponse<MapperBindFlat>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.FailedWithCode(result, MapperBindAssert.FaultCode);
    }

    [TestMethod]
    public void FromResponse_Soap12FaultWhileTheCallerPassesTheSoap11Namespace_IsStillDetected_Test()
    {
        var response = Fault12();

        Assert.IsTrue(
            MapperBindEnvelope.CarriesFaultElement(response, MapperBindEnvelope.Soap12Ns),
            "The fixture must really carry a SOAP 1.2 Fault element.");

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

        Assert.IsTrue(
            MapperBindEnvelope.CarriesFaultElement(response, MapperBindEnvelope.Soap12Ns),
            "The fixture must really carry a Fault element.");
        Assert.AreEqual(
            2,
            MapperBindEnvelope.CountBodyChildren(response, MapperBindEnvelope.Soap12Ns),
            "The fixture must really carry both a payload and a fault.");

        var result = _mapper.FromResponse<MapperBindFlat>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.FailedWithCode(result, MapperBindAssert.FaultCode);
        Assert.IsNull(result.Response);
    }

    [TestMethod]
    public void FromResponse_PayloadElementNamedFaultOutsideTheSoapNamespace_IsNotAFault_Test()
    {
        var response = MapperBindEnvelope.Wrap12(
            $"<Fault xmlns=\"{MapperBindNs.Contract}\"><Id>42</Id></Fault>");

        Assert.IsFalse(
            MapperBindEnvelope.CarriesFaultElement(response, MapperBindEnvelope.Soap12Ns),
            "The fixture must NOT carry a Fault in the SOAP namespace.");
        Assert.IsFalse(
            MapperBindEnvelope.CarriesFaultElement(response, MapperBindEnvelope.Soap11Ns),
            "The fixture must NOT carry a Fault in the SOAP 1.1 namespace either.");

        var result = _mapper.FromResponse<MapperBindFlat>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        Assert.AreEqual(42, result.Response.Id);
    }
}
