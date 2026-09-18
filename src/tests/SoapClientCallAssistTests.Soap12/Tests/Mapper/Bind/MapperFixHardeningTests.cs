
#nullable disable

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Dto.Map;
using SoapClientCallAssist.Mapping;
using SoapClientCallAssistTests.Soap12.Helpers.Mapper;
using SoapClientCallAssistTests.Soap12.Models;
using System.Linq;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests.Mapper.Bind;

[TestClass]
public sealed class MapperFixHardeningTests
{
    private readonly ISoapModelMapper _mapper = new SoapModelMapper();

    [DataTestMethod]
    [DataRow("Body", DisplayName = "the unprefixed body name")]
    [DataRow("s:Body", DisplayName = "a prefix the envelope does not use")]
    public void FromResponse_BodyTagOverrideNamingTheRealBody_StillBinds_Test(string bodyTag)
    {

        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("Flat", "<Id>42</Id>"), "env");


        var result = _mapper.FromResponse<MapperBindFlat>(
            response, MapperBindEnvelope.Protocol12, bodyTag);


        MapperBindAssert.Succeeded(result);
        Assert.AreEqual(42, result.Response.Id);
    }

    [TestMethod]
    public void FromResponse_BodyTagOverrideNamingABodyOfTheOtherProtocol_StillBinds_Test()
    {

        var response = MapperBindEnvelope.Wrap11(MapperBindEnvelope.Payload("Flat", "<Id>42</Id>"));


        var result = _mapper.FromResponse<MapperBindFlat>(
            response, MapperBindEnvelope.Protocol12, "Body");


        MapperBindAssert.Succeeded(result);
        Assert.AreEqual(42, result.Response.Id);
    }

    [TestMethod]
    public void FromResponse_BodyTagOverrideNamingABodyOutsideBothSoapNamespaces_ReportsNoSingleBody_Test()
    {

        var response =
            $"<Envelope xmlns=\"{MapperBindNs.Contract}\">"
            + "<Body><Id>42</Id></Body>"
            + "</Envelope>";

        Assert.AreEqual(1, XDocument.Parse(response).Descendants().Count(x => x.Name.LocalName == "Body"));


        var result = _mapper.FromResponse<MapperBindFlat>(
            response, MapperBindEnvelope.Protocol12, "Body");


        MapperBindAssert.FailedWithCode(result, MapperBindAssert.NoSingleBodyCode);
    }


    [TestMethod]
    public void FromResponse_CharMemberHoldingASpace_BindsTheSpace_Test()
    {


        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("Charred", "<Separator> </Separator>"));

        var result = _mapper.FromResponse<MapperFixCharModel>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        Assert.AreEqual(' ', result.Response.Separator);
    }

    [TestMethod]
    public void FromResponse_CharMemberHoldingAnOrdinaryLetter_StillBinds_Test()
    {

        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("Charred", "<Separator>x</Separator>"));

        var result = _mapper.FromResponse<MapperFixCharModel>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        Assert.AreEqual('x', result.Response.Separator);
    }



    [TestMethod]
    public void FromResponse_MappedMemberWithNoPublicSetter_IsReportedRatherThanDropped_Test()
    {

        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("ReadOnly", "<Name>sent</Name><Computed>sent</Computed>"));

        var result = _mapper.FromResponse<MapperFixReadOnlyMember>(response, MapperBindEnvelope.Protocol12);


        MapperBindAssert.FailedWithCode(result, "V-MAP-002");
        MapperBindAssert.MessagesContain(result, nameof(MapperFixReadOnlyMember.Computed));
    }

    [TestMethod]
    public void ToBodies_MappedMemberWithNoPublicSetter_IsReportedOnTheEmitSideToo_Test()
    {


        var request = new SoapOperationRequest("Save", XNamespace.Get(MapperBindNs.Contract))
            .AddParameter("model", new MapperFixReadOnlyMember { Name = "n" });

        var result = _mapper.ToBodies(request);


        MapperBindAssert.FailedWithCode(result, "V-MAP-002");
    }

    [TestMethod]
    public void FromResponse_UndecoratedPropertyWithNoPublicSetter_IsStillIgnored_Test()
    {


        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("Tolerant", "<Name>sent</Name>"));

        var result = _mapper.FromResponse<MapperFixUndecoratedReadOnly>(
            response, MapperBindEnvelope.Protocol12);


        MapperBindAssert.Succeeded(result);
        Assert.AreEqual("sent", result.Response.Name);
    }


    [TestMethod]
    public void RegisterSoapClientsEndpoint_WhenTheConsumerRegisteredItsOwnMapper_KeepsIt_Test()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ISoapModelMapper, MapperFixStubMapper>();


        services.RegisterSoapClientsEndpoint();

        using var provider = services.BuildServiceProvider();
        Assert.IsInstanceOfType<MapperFixStubMapper>(provider.GetRequiredService<ISoapModelMapper>());
    }
    [TestMethod]
    public void RegisterSoapClientsEndpoint_WhenTheConsumerRegisteredNothing_SuppliesTheDefaultMapper_Test()
    {

        var services = new ServiceCollection();


        services.RegisterSoapClientsEndpoint();


        using var provider = services.BuildServiceProvider();
        Assert.IsInstanceOfType<SoapModelMapper>(provider.GetRequiredService<ISoapModelMapper>());
    }
}
