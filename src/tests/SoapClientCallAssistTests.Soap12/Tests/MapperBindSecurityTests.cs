
#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Mapping;
using SoapClientCallAssistTests.Soap12.Helpers;
using SoapClientCallAssistTests.Soap12.Models;
using System.Linq;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests;

[TestClass]
public sealed class MapperBindSecurityTests
{
    private const string FaultMarker = "ZZQ-FAULT-MARKER-7b1e";

    private const string DetailMarker = "ZZQ-DETAIL-MARKER-3c5a";

    private const string ScalarMarker = "not-a-number-9f3c";

    private const string TagMarker = "ZZQ-TAG-MARKER-8c4d";

    private ISoapModelMapper _mapper;

    [TestInitialize]
    public void Initialize() => _mapper = new SoapModelMapper();

    [TestMethod]
    public void FromResponse_FaultCarryingDistinctiveText_DoesNotEchoAnyOfItIntoTheMessages_Test()
    {

        var response =
            $"<soap:Envelope xmlns:soap=\"{MapperBindEnvelope.Soap12Ns}\"><soap:Body>"
            + "<soap:Fault>"
            + "<soap:Code><soap:Value>soap:Receiver</soap:Value></soap:Code>"
            + $"<soap:Reason><soap:Text xml:lang=\"en\">{FaultMarker}</soap:Text></soap:Reason>"
            + $"<soap:Detail><trace>{DetailMarker}</trace></soap:Detail>"
            + "</soap:Fault>"
            + "</soap:Body></soap:Envelope>";


        Assert.IsTrue(response.Contains(FaultMarker), "The fixture must really carry the reason marker.");
        Assert.IsTrue(response.Contains(DetailMarker), "The fixture must really carry the detail marker.");
        Assert.IsTrue(
            MapperBindEnvelope.CarriesFaultElement(response, MapperBindEnvelope.Soap12Ns),
            "The fixture must really carry a SOAP 1.2 Fault element.");


        var result = _mapper.FromResponse<MapperBindFlat>(response, MapperBindEnvelope.Protocol12);


        MapperBindAssert.FailedWithCode(result, MapperBindAssert.FaultCode);
        MapperBindAssert.MessagesDoNotContain(result, FaultMarker, DetailMarker);
    }


    [TestMethod]
    public void FromResponse_MalformedScalar_FailsWithTheBindCode_Test()
    {

        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("Flat", $"<Id>{ScalarMarker}</Id>"));

        var result = _mapper.FromResponse<MapperBindFlat>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.FailedWithCode(result, MapperBindAssert.BindCode);

        MapperBindAssert.MessagesContain(result, "MapperBindFlat.Id");
    }
    [TestMethod]
    public void FromResponse_MalformedScalar_EchoesNeitherTheValueNorTheBclExceptionText_Test()
    {

        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("Flat", $"<Id>{ScalarMarker}</Id>"));
        Assert.IsTrue(response.Contains(ScalarMarker), "The fixture must really carry the marker value.");


        var result = _mapper.FromResponse<MapperBindFlat>(response, MapperBindEnvelope.Protocol12);


        MapperBindAssert.FailedWithCode(result, MapperBindAssert.BindCode);
        MapperBindAssert.MessagesDoNotContain(
            result,
            ScalarMarker,
            "was not in a correct format",
            "is not a valid");
    }

    [TestMethod]
    public void FromResponse_MalformedDateTime_DoesNotEchoTheOffendingValue_Test()
    {


        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("Flat", $"<CreatedOn>{ScalarMarker}</CreatedOn>"));
        Assert.IsTrue(response.Contains(ScalarMarker), "The fixture must really carry the marker value.");


        var result = _mapper.FromResponse<MapperBindFlat>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.FailedWithCode(result, MapperBindAssert.BindCode);
        MapperBindAssert.MessagesContain(result, "MapperBindFlat.CreatedOn");
        MapperBindAssert.MessagesDoNotContain(result, ScalarMarker, "is not a valid");
    }

    [TestMethod]
    public void FromResponse_MalformedGuid_DoesNotEchoTheOffendingValue_Test()
    {

        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("Flat", $"<Reference>{ScalarMarker}</Reference>"));
        Assert.IsTrue(response.Contains(ScalarMarker), "The fixture must really carry the marker value.");


        var result = _mapper.FromResponse<MapperBindFlat>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.FailedWithCode(result, MapperBindAssert.BindCode);
        MapperBindAssert.MessagesDoNotContain(result, ScalarMarker);
    }

    [TestMethod]
    public void FromResponse_MalformedXml_DoesNotEchoTheOffendingMarkupIntoTheMessages_Test()
    {


        var response =
            $"<soap:Envelope xmlns:soap=\"{MapperBindEnvelope.Soap12Ns}\"><soap:Body>"
            + $"<Flat xmlns=\"{MapperBindNs.Contract}\"><Id>1</{TagMarker}>"
            + "</soap:Body></soap:Envelope>";

        Assert.IsTrue(response.Contains(TagMarker), "The fixture must really carry the marker tag.");


        var result = _mapper.FromResponse<MapperBindFlat>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.FailedWithCode(result, MapperBindAssert.ResponseCode);
        MapperBindAssert.MessagesDoNotContain(result, TagMarker);
    }



    [TestMethod]
    public void FromResponse_PayloadDeclaringAnXsiType_BindsTheDeclaredGenericTypeInstead_Test()
    {
        var before = MapperBindTripwire.Constructions;
        var response = MapperBindEnvelope.Wrap12(
            $"<Flat xmlns=\"{MapperBindNs.Contract}\" xmlns:xsi=\"{MapperBindEnvelope.XsiNs}\" "
            + $"xmlns:tns=\"{MapperBindNs.Contract}\" xsi:type=\"tns:MapperBindTripwire\">"
            + "<Id>42</Id><Name>widget</Name>"
            + "</Flat>");


        var payload = XDocument
            .Parse(response)
            .Descendants(XName.Get("Flat", MapperBindNs.Contract))
            .Single();
        Assert.AreEqual(
            "tns:MapperBindTripwire",
            payload.Attribute(XName.Get("type", MapperBindEnvelope.XsiNs))?.Value,
            "The fixture must really declare an xsi:type on the payload element.");


        var result = _mapper.FromResponse<MapperBindFlat>(response, MapperBindEnvelope.Protocol12);


        MapperBindAssert.Succeeded(result);
        Assert.IsInstanceOfType(result.Response, typeof(MapperBindFlat));
        Assert.AreEqual(42, result.Response.Id);
        Assert.AreEqual("widget", result.Response.Name);
        Assert.AreEqual(
            before,
            MapperBindTripwire.Constructions,
            "The type named by xsi:type must never be instantiated.");
    }

    [TestMethod]
    public void FromResponse_MemberElementDeclaringAnXsiType_StillReadsTheDeclaredClrType_Test()
    {

        var response = MapperBindEnvelope.Wrap12(
            $"<Flat xmlns=\"{MapperBindNs.Contract}\" xmlns:xsi=\"{MapperBindEnvelope.XsiNs}\" "
            + "xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">"
            + "<Id>42</Id><Name xsi:type=\"xs:base64Binary\">widget</Name>"
            + "</Flat>");


        var result = _mapper.FromResponse<MapperBindFlat>(response, MapperBindEnvelope.Protocol12);


        MapperBindAssert.Succeeded(result);
        Assert.AreEqual("widget", result.Response.Name);
    }

    [TestMethod]
    public void FromResponse_NestedComplexMemberDeclaringAnXsiType_BindsTheDeclaredMemberType_Test()
    {

        var before = MapperBindTripwire.Constructions;
        var response = MapperBindEnvelope.Wrap12(
            $"<Order xmlns=\"{MapperBindNs.Contract}\" xmlns:xsi=\"{MapperBindEnvelope.XsiNs}\" "
            + $"xmlns:tns=\"{MapperBindNs.Contract}\">"
            + "<Code>ORD-1</Code>"
            + "<Customer xsi:type=\"tns:MapperBindTripwire\"><FullName>Ada</FullName><Age>36</Age></Customer>"
            + "</Order>");


        var result = _mapper.FromResponse<MapperBindOrder>(response, MapperBindEnvelope.Protocol12);


        MapperBindAssert.Succeeded(result);
        Assert.IsInstanceOfType(result.Response.Customer, typeof(MapperBindCustomer));
        Assert.AreEqual("Ada", result.Response.Customer.FullName);
        Assert.AreEqual(
            before,
            MapperBindTripwire.Constructions,
            "The type named by xsi:type must never be instantiated for a nested member either.");
    }

}
