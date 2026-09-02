
#nullable disable

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Attributes;
using SoapClientCallAssist.Dto.Map;
using SoapClientCallAssist.Mapping;
using SoapClientCallAssistTests.Soap12.Helpers;
using SoapClientCallAssistTests.Soap12.Models;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests;

[TestClass]
public sealed class MapperFixHardeningTests
{
    private ISoapModelMapper _mapper;

    [TestInitialize]
    public void Initialize() => _mapper = new SoapModelMapper();

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

        Assert.AreEqual(
            1,
            XDocument.Parse(response).Descendants().Count(x => x.Name.LocalName == "Body"),
            "The fixture must carry exactly one element named Body, or the test proves nothing.");


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




[SoapContract(Name = "Charred", Namespace = MapperBindNs.Contract)]
public sealed class MapperFixCharModel
{
    [SoapMember(Name = "Separator", Order = 0)]
    public char Separator { get; set; }
}


[SoapContract(Name = "ReadOnly", Namespace = MapperBindNs.Contract)]
public sealed class MapperFixReadOnlyMember
{
    [SoapMember(Name = "Name", Order = 0)]
    public string Name { get; set; }
    [SoapMember(Name = "Computed", Order = 1)]
    public string Computed => "computed";
}




[SoapContract(Name = "Tolerant", Namespace = MapperBindNs.Contract)]
public sealed class MapperFixUndecoratedReadOnly
{
    [SoapMember(Name = "Name", Order = 0)]
    public string Name { get; set; }
    public string Computed => "computed";
}




internal sealed class MapperFixStubMapper : ISoapModelMapper
{
    public IResult<IEnumerable<XElement>> ToBodies(SoapOperationRequest request)
        => Result<IEnumerable<XElement>>.Success(new List<XElement>());

    public IResult<T> FromResponse<T>(
        string soapResponse, XNamespace protocolNamespace, string soapXmlBodyTag = null)
        => Result<T>.Success(default);
}
