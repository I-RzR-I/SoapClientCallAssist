
#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Mapping;
using SoapClientCallAssistTests.Soap12.Helpers;
using SoapClientCallAssistTests.Soap12.Models;
using System.Text;

namespace SoapClientCallAssistTests.Soap12.Tests;

[TestClass]
public sealed class MapperBindContractFailureTests
{
    private const int MaxGraphDepth = 32;

    private ISoapModelMapper _mapper;

    [TestInitialize]
    public void Initialize() => _mapper = new SoapModelMapper();

    private static string NodeChain(int depth)
    {
        var builder = new StringBuilder();
        for (var index = 0; index < depth; index++)
            builder.Append($"<Child><Name>level-{index}</Name>");

        for (var index = 0; index < depth; index++)
            builder.Append("</Child>");

        return MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("Node", "<Name>root</Name>" + builder));
    }

    [TestMethod]
    public void FromResponse_TypeWithNoMappedMembers_ReportsTheNoMappedMembersCode_Test()
    {
        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("Unmapped", "<Anything>x</Anything>"));

        var result = _mapper.FromResponse<MapperBindNoMappedMembers>(
            response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.FailedWithCode(result, "V-MAP-001");
    }

    [TestMethod]
    public void FromResponse_TypeWithAnUnsupportedMemberType_ReportsTheUnsupportedTypeCode_Test()
    {
        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("Unsupported", "<Handle>1</Handle>"));

        var result = _mapper.FromResponse<MapperBindUnsupportedMember>(
            response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.FailedWithCode(result, "V-MAP-002");
    }

    [TestMethod]
    public void FromResponse_TypeDeclaringTheSameWireNameTwice_ReportsTheDuplicateCode_Test()
    {
        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("Duplicated", "<Same>x</Same>"));

        var result = _mapper.FromResponse<MapperBindDuplicateWireNames>(
            response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.FailedWithCode(result, "V-MAP-003");
    }

    [TestMethod]
    public void FromResponse_TypeMixingTheTwoConventions_ReportsTheMixedConventionsCode_Test()
    {
        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("Mixed", "<A>x</A><B>y</B>"));

        var result = _mapper.FromResponse<MapperBindMixedConventions>(
            response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.FailedWithCode(result, "V-MAP-006");
    }

    [TestMethod]
    public void FromResponse_TypeWithoutAPublicParameterlessConstructor_ReportsTheInstantiationCode_Test()
    {
        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("NoCtor", "<Id>1</Id>"));

        var result = _mapper.FromResponse<MapperBindNoDefaultCtor>(
            response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.FailedWithCode(result, "V-MAP-008");
    }

    [TestMethod]
    public void FromResponse_RecursiveContractWithinTheGraphDepthCap_Binds_Test()
    {
        var response = NodeChain(5);

        var result = _mapper.FromResponse<MapperBindNode>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        Assert.AreEqual("root", result.Response.Name);
        Assert.AreEqual("level-0", result.Response.Child.Name);
        Assert.AreEqual("level-1", result.Response.Child.Child.Name);
    }

    [TestMethod]
    public void FromResponse_RecursiveContractBeyondTheGraphDepthCap_ReportsTheDepthCode_Test()
    {
        var response = NodeChain(MaxGraphDepth + 8);

        var result = _mapper.FromResponse<MapperBindNode>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.FailedWithCode(result, "V-MAP-007");
    }
}
