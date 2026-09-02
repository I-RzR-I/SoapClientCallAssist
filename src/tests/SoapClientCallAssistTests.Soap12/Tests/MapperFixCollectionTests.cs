
#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Attributes;
using SoapClientCallAssist.Mapping;
using SoapClientCallAssistTests.Soap12.Helpers;
using SoapClientCallAssistTests.Soap12.Models;
using System.Linq;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests;

[TestClass]
public sealed class MapperFixCollectionTests
{
    private const string Xsi = "xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"";

    private ISoapModelMapper _mapper;

    [TestInitialize]
    public void Initialize() => _mapper = new SoapModelMapper();

    private static string NilPayload(string localName, string inner)
        => $"<{localName} xmlns=\"{MapperBindNs.Contract}\" {Xsi}>{inner}</{localName}>";

    [TestMethod]
    public void FromResponse_BareSiblingsOfAPrimitive_BindEveryValueInOrder_Test()
    {

        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("Basket", "<Id>7</Id><Tags>a</Tags><Tags>b</Tags>"));


        var siblings = XDocument
            .Parse(response)
            .Descendants(XName.Get("Tags", MapperBindNs.Contract))
            .ToList();
        Assert.AreEqual(2, siblings.Count, "The fixture must really repeat the leaf element.");
        Assert.IsFalse(siblings.Any(x => x.Elements().Any()), "No sibling may hold an element child.");


        var result = _mapper.FromResponse<MapperBindBasket>(response, MapperBindEnvelope.Protocol12);


        MapperBindAssert.Succeeded(result);
        Assert.IsNotNull(result.Response.Tags, "The unwrapped form must not bind a null collection.");
        CollectionAssert.AreEqual(new[] { "a", "b" }, result.Response.Tags);
    }

    [TestMethod]
    public void FromResponse_ASingleBareSiblingCarryingText_BindsOneItemRatherThanAnEmptyCollection_Test()
    {

        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("Basket", "<Id>7</Id><Tags>only</Tags>"));


        var result = _mapper.FromResponse<MapperBindBasket>(response, MapperBindEnvelope.Protocol12);


        MapperBindAssert.Succeeded(result);
        CollectionAssert.AreEqual(new[] { "only" }, result.Response.Tags);
    }

    [TestMethod]
    public void FromResponse_BareSiblingsWithAnEmptyOneAmongThem_KeepThePositionOfTheEmptyValue_Test()
    {

        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("Basket", "<Tags>a</Tags><Tags /><Tags>c</Tags>"));


        var result = _mapper.FromResponse<MapperBindBasket>(response, MapperBindEnvelope.Protocol12);


        MapperBindAssert.Succeeded(result);
        CollectionAssert.AreEqual(new[] { "a", string.Empty, "c" }, result.Response.Tags);
    }

    [TestMethod]
    public void FromResponse_ANilBareSiblingAmongValues_BindsANullItem_Test()
    {

        var response = MapperBindEnvelope.Wrap12(
            NilPayload("Basket", "<Tags>a</Tags><Tags xsi:nil=\"true\" /><Tags>c</Tags>"));


        var result = _mapper.FromResponse<MapperBindBasket>(response, MapperBindEnvelope.Protocol12);


        MapperBindAssert.Succeeded(result);
        CollectionAssert.AreEqual(new[] { "a", null, "c" }, result.Response.Tags);
    }


    [TestMethod]
    public void FromResponse_BareSiblingsOfAComplexType_AreStillReadAsWrappers_Test()
    {


        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload(
                "Basket",
                "<Lines><Line><Sku>A-1</Sku><Qty>2</Qty></Line></Lines>"
                + "<Lines><Line><Sku>B-2</Sku><Qty>5</Qty></Line></Lines>"));


        var result = _mapper.FromResponse<MapperBindBasket>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        Assert.AreEqual(2, result.Response.Lines.Count);
        Assert.AreEqual("A-1", result.Response.Lines[0].Sku);
        Assert.AreEqual("B-2", result.Response.Lines[1].Sku);
    }

    [TestMethod]
    public void FromResponse_WrappersWithElementChildrenAndNoItemName_AreStillReadAsWrappers_Test()
    {


        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload(
                "Basket",
                "<Tags><string>alpha</string></Tags><Tags><string>beta</string></Tags>"));


        var result = _mapper.FromResponse<MapperBindBasket>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        CollectionAssert.AreEqual(new[] { "alpha", "beta" }, result.Response.Tags);
    }

    [TestMethod]
    public void FromResponse_EmptyWrapperWithoutAnItemName_StillBindsAnEmptyCollection_Test()
    {


        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("Basket", "<Id>1</Id><Tags />"));

        var result = _mapper.FromResponse<MapperBindBasket>(response, MapperBindEnvelope.Protocol12);


        MapperBindAssert.Succeeded(result);
        Assert.IsNotNull(result.Response.Tags);
        Assert.AreEqual(0, result.Response.Tags.Length);
    }

    [TestMethod]
    public void FromResponse_AbsentCollectionWithoutAnItemName_StillBindsNull_Test()
    {

        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("Basket", "<Id>1</Id>"));

        var result = _mapper.FromResponse<MapperBindBasket>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        Assert.IsNull(result.Response.Tags, "An absent element leaves the member at its CLR default.");
    }

    [TestMethod]
    public void FromResponse_NilBareCollectionWithoutAnItemName_StillBindsNull_Test()
    {

        var response = MapperBindEnvelope.Wrap12(NilPayload("Basket", "<Tags xsi:nil=\"true\" />"));

        var result = _mapper.FromResponse<MapperBindBasket>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        Assert.IsNull(result.Response.Tags);
    }

    [TestMethod]
    public void FromResponse_BareSiblingsWhereTheItemNameIsDeclared_AreStillReadAsWrappers_Test()
    {


        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("Basket", "<LocationIds>10</LocationIds>"));

        var result = _mapper.FromResponse<MapperBindBasket>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        Assert.IsNotNull(result.Response.LocationIds);
        Assert.AreEqual(0, result.Response.LocationIds.Count);
    }



    [TestMethod]
    public void FromResponse_MixedWrappedAndBareSiblings_AreReadAsWrappers_Test()
    {


        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("Basket", "<Tags><string>alpha</string></Tags><Tags>beta</Tags>"));
        var result = _mapper.FromResponse<MapperBindBasket>(response, MapperBindEnvelope.Protocol12);


        MapperBindAssert.Succeeded(result);
        CollectionAssert.AreEqual(new[] { "alpha" }, result.Response.Tags);
    }

    [TestMethod]
    public void FromResponse_BareSiblingsOfAnIntegerCollection_ConvertEveryValue_Test()
    {

        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("Unwrapped", "<Score>1</Score><Score>2</Score><Score>3</Score>"));


        var result = _mapper.FromResponse<MapperFixUnwrapped>(response, MapperBindEnvelope.Protocol12);


        MapperBindAssert.Succeeded(result);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, result.Response.Scores);
    }

    [TestMethod]
    public void FromResponse_ANilBareSiblingOfANonNullableItemType_IsReportedRatherThanDropped_Test()
    {

        var response = MapperBindEnvelope.Wrap12(
            NilPayload("Unwrapped", "<Score>1</Score><Score xsi:nil=\"true\" />"));


        var result = _mapper.FromResponse<MapperFixUnwrapped>(response, MapperBindEnvelope.Protocol12);



        MapperBindAssert.FailedWithCode(result, MapperBindAssert.NilNonNullableCode);
    }

}



[SoapContract(Name = "Unwrapped", Namespace = MapperBindNs.Contract)]
public sealed class MapperFixUnwrapped
{
    [SoapMember(Name = "Score", Order = 0)]
    public int[] Scores { get; set; }
}
