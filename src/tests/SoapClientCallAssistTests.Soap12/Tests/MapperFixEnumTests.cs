
#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Attributes;
using SoapClientCallAssist.Mapping;
using SoapClientCallAssistTests.Soap12.Enums;
using SoapClientCallAssistTests.Soap12.Helpers;
using SoapClientCallAssistTests.Soap12.Models;
using System;

namespace SoapClientCallAssistTests.Soap12.Tests;

[TestClass]
public sealed class MapperFixEnumTests
{
    private ISoapModelMapper _mapper;

    [TestInitialize]
    public void Initialize() => _mapper = new SoapModelMapper();

    private static string RightsResponse(string text)
        => MapperBindEnvelope.Wrap12(MapperBindEnvelope.Payload("Account", $"<Rights>{text}</Rights>"));

    [DataTestMethod]
    [DataRow("Read Write", DisplayName = "the XSD list form a schema declares")]
    [DataRow("Read,Write", DisplayName = "the CLR form")]
    [DataRow("Read, Write", DisplayName = "the CLR form as ToString writes it")]
    [DataRow("Read  Write", DisplayName = "repeated whitespace between the members")]
    public void FromResponse_EveryLexicalFormOfAFlagsList_BindsTheSameValue_Test(string text)
    {

        var response = RightsResponse(text);


        var result = _mapper.FromResponse<MapperBindAccount>(response, MapperBindEnvelope.Protocol12);


        MapperBindAssert.Succeeded(result);
        Assert.AreEqual(MapperBindRights.Read | MapperBindRights.Write, result.Response.Rights);
    }

    [TestMethod]
    public void FromResponse_AllThreeFlagsAsAList_BindsEveryBit_Test()
    {

        var response = RightsResponse("Read Write Admin");


        var result = _mapper.FromResponse<MapperBindAccount>(response, MapperBindEnvelope.Protocol12);


        MapperBindAssert.Succeeded(result);
        Assert.AreEqual(
            MapperBindRights.Read | MapperBindRights.Write | MapperBindRights.Admin,
            result.Response.Rights);
    }

    [TestMethod]
    public void FromResponse_ASingleFlagMemberName_StillBinds_Test()
    {

        var response = RightsResponse("Admin");


        var result = _mapper.FromResponse<MapperBindAccount>(response, MapperBindEnvelope.Protocol12);


        MapperBindAssert.Succeeded(result);
        Assert.AreEqual(MapperBindRights.Admin, result.Response.Rights);
    }

    [TestMethod]
    public void FromResponse_TheZeroMemberOfAFlagsEnum_StillBinds_Test()
    {

        var response = RightsResponse("None");


        var result = _mapper.FromResponse<MapperBindAccount>(response, MapperBindEnvelope.Protocol12);


        MapperBindAssert.Succeeded(result);
        Assert.AreEqual(MapperBindRights.None, result.Response.Rights);
    }


    [DataTestMethod]
    [DataRow("8", DisplayName = "one bit past the declared members")]
    [DataRow("9911", DisplayName = "an arbitrary literal")]
    [DataRow("-1", DisplayName = "every bit set")]
    public void FromResponse_ANumericLiteralSettingUndeclaredBits_IsRejected_Test(string text)
    {

        var response = RightsResponse(text);

        var result = _mapper.FromResponse<MapperBindAccount>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.FailedWithCode(result, MapperBindAssert.BindCode);
        MapperBindAssert.MessagesContain(result, "MapperBindAccount.Rights");
    }

    [DataTestMethod]
    [DataRow("3", DisplayName = "the composite of Read and Write")]
    [DataRow("7", DisplayName = "the composite of every declared member")]
    [DataRow("0", DisplayName = "the zero member")]
    public void FromResponse_ANumericLiteralWithinTheDeclaredBits_IsAccepted_Test(string text)
    {


        var response = RightsResponse(text);
        var result = _mapper.FromResponse<MapperBindAccount>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        Assert.AreEqual((MapperBindRights)int.Parse(text), result.Response.Rights);
    }

    [TestMethod]
    public void FromResponse_AnUnknownMemberNameInAFlagsList_IsRejectedWithoutEchoingIt_Test()
    {


        const string marker = "ZZQ-FLAG-MARKER-6b21";
        var response = RightsResponse($"Read {marker}");
        Assert.IsTrue(response.Contains(marker), "The fixture must really carry the marker value.");


        var result = _mapper.FromResponse<MapperBindAccount>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.FailedWithCode(result, MapperBindAssert.BindCode);
        MapperBindAssert.MessagesDoNotContain(result, marker);
    }

    [TestMethod]
    public void FromResponse_AFlagsEnumOnASignedUnderlyingTypeWithANegativeMember_ReadsItsBits_Test()
    {


        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("Signed", "<Mode>Low High</Mode>"));

        var result = _mapper.FromResponse<MapperFixSignedFlagsModel>(
            response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        Assert.AreEqual(MapperFixSignedRights.Low | MapperFixSignedRights.High, result.Response.Mode);
    }



    [TestMethod]
    public void FromResponse_ASpaceSeparatedListForANonFlagsEnum_IsStillRejected_Test()
    {

        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("Account", "<Status>Active Suspended</Status>"));

        var result = _mapper.FromResponse<MapperBindAccount>(response, MapperBindEnvelope.Protocol12);


        MapperBindAssert.FailedWithCode(result, MapperBindAssert.BindCode);
    }

}



[Flags]
public enum MapperFixSignedRights
{
    None = 0,
    Low = 1,
    High = 2,
    Sign = int.MinValue
}

[SoapContract(Name = "Signed", Namespace = MapperBindNs.Contract)]
public sealed class MapperFixSignedFlagsModel
{
    [SoapMember(Name = "Mode", Order = 0)]
    public MapperFixSignedRights Mode { get; set; }
}
