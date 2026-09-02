
#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Dto.Map;
using SoapClientCallAssist.Mapping;
using SoapClientCallAssistTests.Soap12.Enums;
using SoapClientCallAssistTests.Soap12.Helpers;
using SoapClientCallAssistTests.Soap12.Models;
using System.Linq;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests;

[TestClass]
public sealed class MapperBindNilAndEnumTests
{
    private const string Xsi = "xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"";

    private ISoapModelMapper _mapper;

    [TestInitialize]
    public void Initialize() => _mapper = new SoapModelMapper();

    private static string NilPayload(string localName, string inner)
        => $"<{localName} xmlns=\"{MapperBindNs.Contract}\" {Xsi}>{inner}</{localName}>";

    [TestMethod]
    public void FromResponse_NilElementForAReferenceType_BindsNull_Test()
    {

        var response = MapperBindEnvelope.Wrap12(
            NilPayload("Nillable", "<Text xsi:nil=\"true\" /><Count>1</Count>"));


        var result = _mapper.FromResponse<MapperBindNillable>(response, MapperBindEnvelope.Protocol12);


        MapperBindAssert.Succeeded(result);
        Assert.IsNull(result.Response.Text, "A nil element must overwrite the preset with null.");
        Assert.AreEqual(1, result.Response.Count);
    }

    [TestMethod]
    public void FromResponse_NilElementForANullableValueType_BindsNull_Test()
    {

        var response = MapperBindEnvelope.Wrap12(
            NilPayload("Nillable", "<Text>present</Text><Count xsi:nil=\"true\" />"));


        var result = _mapper.FromResponse<MapperBindNillable>(response, MapperBindEnvelope.Protocol12);


        MapperBindAssert.Succeeded(result);
        Assert.AreEqual("present", result.Response.Text);
        Assert.IsNull(result.Response.Count, "A nil element must overwrite the preset with null.");
    }

    [TestMethod]
    public void FromResponse_NilElementForANonNullableValueType_ReportsTheNilValidationCode_Test()
    {

        var response = MapperBindEnvelope.Wrap12(
            NilPayload("Presets", "<Text>present</Text><Count xsi:nil=\"true\" />"));


        var count = XDocument
            .Parse(response)
            .Descendants(XName.Get("Count", MapperBindNs.Contract))
            .Single();
        Assert.AreEqual(
            "true",
            count.Attribute(XName.Get("nil", MapperBindEnvelope.XsiNs))?.Value,
            "The fixture must really mark the element nil.");


        var result = _mapper.FromResponse<MapperBindPresets>(response, MapperBindEnvelope.Protocol12);


        MapperBindAssert.FailedWithCode(result, MapperBindAssert.NilNonNullableCode);
    }

    [DataTestMethod]
    [DataRow("true", DisplayName = "xsi:nil=\"true\"")]
    [DataRow("TRUE", DisplayName = "xsi:nil=\"TRUE\" (nil is case insensitive)")]
    [DataRow("1", DisplayName = "xsi:nil=\"1\" (the other lexical form of true)")]
    [DataRow("  true  ", DisplayName = "xsi:nil with surrounding whitespace")]
    public void FromResponse_EveryLexicalFormOfNil_BindsNull_Test(string nilValue)
    {

        var response = MapperBindEnvelope.Wrap12(
            NilPayload("Nillable", $"<Text xsi:nil=\"{nilValue}\" />"));


        var result = _mapper.FromResponse<MapperBindNillable>(response, MapperBindEnvelope.Protocol12);


        MapperBindAssert.Succeeded(result);
        Assert.IsNull(result.Response.Text);
    }

    [TestMethod]
    public void FromResponse_NilExplicitlyFalse_BindsTheElementText_Test()
    {

        var response = MapperBindEnvelope.Wrap12(
            NilPayload("Nillable", "<Text xsi:nil=\"false\">value</Text>"));


        var result = _mapper.FromResponse<MapperBindNillable>(response, MapperBindEnvelope.Protocol12);


        MapperBindAssert.Succeeded(result);
        Assert.AreEqual("value", result.Response.Text);
    }

    [TestMethod]
    public void FromResponse_UnqualifiedNilAttribute_IsNotTreatedAsNil_Test()
    {

        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("Nillable", "<Text nil=\"true\" />"));


        var result = _mapper.FromResponse<MapperBindNillable>(response, MapperBindEnvelope.Protocol12);


        MapperBindAssert.Succeeded(result);
        Assert.AreEqual(string.Empty, result.Response.Text);
    }

    [TestMethod]
    public void FromResponse_NilCollectionWrapper_BindsANullCollection_Test()
    {

        var response = MapperBindEnvelope.Wrap12(
            NilPayload("Nillable", "<Ids xsi:nil=\"true\" />"));


        var result = _mapper.FromResponse<MapperBindNillable>(response, MapperBindEnvelope.Protocol12);


        MapperBindAssert.Succeeded(result);
        Assert.IsNull(result.Response.Ids);
    }

    [TestMethod]
    public void FromResponse_NilItemInACollectionOfValueTypes_ReportsTheNilValidationCode_Test()
    {

        var response = MapperBindEnvelope.Wrap12(
            NilPayload("Nillable", "<Ids><int>1</int><int xsi:nil=\"true\" /></Ids>"));


        var result = _mapper.FromResponse<MapperBindNillable>(response, MapperBindEnvelope.Protocol12);


        MapperBindAssert.FailedWithCode(result, MapperBindAssert.NilNonNullableCode);
    }


    [TestMethod]
    public void FromResponse_AbsentElements_LeaveEveryMemberAtItsClrDefault_Test()
    {

        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("Presets", "<Unrelated>x</Unrelated>"));

        var result = _mapper.FromResponse<MapperBindPresets>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        Assert.AreEqual("preset-text", result.Response.Text);
        Assert.AreEqual(99, result.Response.Count);
    }

    [TestMethod]
    public void FromResponse_AbsentVersusNil_AreDistinguishable_Test()
    {

        var absent = MapperBindEnvelope.Wrap12(MapperBindEnvelope.Payload("Nillable", "<Count>1</Count>"));
        var nil = MapperBindEnvelope.Wrap12(NilPayload("Nillable", "<Text xsi:nil=\"true\" /><Count>1</Count>"));

        var absentResult = _mapper.FromResponse<MapperBindNillable>(absent, MapperBindEnvelope.Protocol12);
        var nilResult = _mapper.FromResponse<MapperBindNillable>(nil, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(absentResult);
        MapperBindAssert.Succeeded(nilResult);
        Assert.AreEqual("preset-text", absentResult.Response.Text);
        Assert.IsNull(nilResult.Response.Text);
    }

    [TestMethod]
    public void FromResponse_EmptyElementForAString_BindsTheEmptyStringNotNull_Test()
    {

        var response = MapperBindEnvelope.Wrap12(MapperBindEnvelope.Payload("Nillable", "<Text />"));

        var result = _mapper.FromResponse<MapperBindNillable>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        Assert.AreEqual(string.Empty, result.Response.Text);
    }



    [TestMethod]
    public void FromResponse_EnumMemberName_BindsTheDeclaredMember_Test()
    {
        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("Account", "<Status>Suspended</Status>"));


        var result = _mapper.FromResponse<MapperBindAccount>(response, MapperBindEnvelope.Protocol12);


        MapperBindAssert.Succeeded(result);
        Assert.AreEqual(MapperBindStatus.Suspended, result.Response.Status);
    }

    [TestMethod]
    public void FromResponse_UndefinedNumericEnumLiteral_IsRejected_Test()
    {


        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("Account", "<Status>9911</Status>"));

        Assert.IsFalse(
            System.Enum.IsDefined(typeof(MapperBindStatus), 9911),
            "The fixture value must really be undefined, otherwise this test proves nothing.");


        var result = _mapper.FromResponse<MapperBindAccount>(response, MapperBindEnvelope.Protocol12);


        MapperBindAssert.FailedWithCode(result, MapperBindAssert.BindCode);
        MapperBindAssert.MessagesContain(result, "MapperBindAccount.Status");
    }

    [TestMethod]
    public void FromResponse_DefinedNumericEnumLiteral_IsAcceptedByTheIsDefinedGuard_Test()
    {

        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("Account", "<Status>2</Status>"));


        var result = _mapper.FromResponse<MapperBindAccount>(response, MapperBindEnvelope.Protocol12);



        MapperBindAssert.Succeeded(result);
        Assert.AreEqual(MapperBindStatus.Suspended, result.Response.Status);
    }
    [TestMethod]
    public void FromResponse_UnknownEnumMemberName_IsRejectedWithoutEchoingIt_Test()
    {

        const string marker = "ZZQ-ENUM-MARKER-2f7d";
        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("Account", $"<Status>{marker}</Status>"));

        Assert.IsTrue(response.Contains(marker), "The fixture must really carry the marker value.");


        var result = _mapper.FromResponse<MapperBindAccount>(response, MapperBindEnvelope.Protocol12);


        MapperBindAssert.FailedWithCode(result, MapperBindAssert.BindCode);
        MapperBindAssert.MessagesDoNotContain(result, marker);
    }

    [TestMethod]
    public void FromResponse_CommaSeparatedFlagsEnum_BindsTheCombinedValue_Test()
    {

        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("Account", "<Rights>Read,Write</Rights>"));


        var result = _mapper.FromResponse<MapperBindAccount>(response, MapperBindEnvelope.Protocol12);


        MapperBindAssert.Succeeded(result);
        Assert.AreEqual(MapperBindRights.Read | MapperBindRights.Write, result.Response.Rights);
    }

    [TestMethod]
    public void FromResponse_UndefinedNumericLiteralForAFlagsEnum_IsRejected_Test()
    {


        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("Account", "<Rights>9911</Rights>"));

        Assert.AreNotEqual(
            0,
            9911 & ~(int)(MapperBindRights.Read | MapperBindRights.Write | MapperBindRights.Admin),
            "The fixture value must really set a bit the enum does not declare.");


        var result = _mapper.FromResponse<MapperBindAccount>(response, MapperBindEnvelope.Protocol12);



        MapperBindAssert.FailedWithCode(result, MapperBindAssert.BindCode);
        MapperBindAssert.MessagesContain(result, "MapperBindAccount.Rights");
    }
    [TestMethod]
    public void FromResponse_SpaceSeparatedFlagsEnum_BindsTheValueTheEmitterWrote_Test()
    {



        var request = new SoapOperationRequest("SetRights", XNamespace.Get(MapperBindNs.Contract))
            .AddParameter("account", new MapperBindAccount
            {
                Status = MapperBindStatus.Active,
                Rights = MapperBindRights.Read | MapperBindRights.Write
            });

        var emitted = _mapper.ToBodies(request);
        MapperBindAssert.Succeeded(emitted);

        var emittedText = emitted.Response
            .Single()
            .Descendants(XName.Get("Rights", MapperBindNs.Contract))
            .Single()
            .Value;


        Assert.AreEqual("Read Write", emittedText);

        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("Account", $"<Rights>{emittedText}</Rights>"));

        var result = _mapper.FromResponse<MapperBindAccount>(response, MapperBindEnvelope.Protocol12);



        MapperBindAssert.Succeeded(result);
        Assert.AreEqual(MapperBindRights.Read | MapperBindRights.Write, result.Response.Rights);
    }
}
