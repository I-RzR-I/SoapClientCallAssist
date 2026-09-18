
#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Mapping;
using SoapClientCallAssistTests.Soap12.Helpers.Mapper;
using SoapClientCallAssistTests.Soap12.Models;
using System.Linq;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests.Mapper.Bind;

[TestClass]
public sealed class MapperFixCollectionTests
{
    private const string Xsi = "xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"";

    private readonly ISoapModelMapper _mapper = new SoapModelMapper();

    private IResult<MapperBindBasket> BindUnreadableComplexCollection()
        => _mapper.FromResponse<MapperBindBasket>(
            MapperBindEnvelope.Wrap12(MapperBindEnvelope.Payload("Basket", "<Lines>unreadable</Lines>")),
            MapperBindEnvelope.Protocol12);

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
        Assert.AreEqual(2, siblings.Count);
        Assert.IsFalse(siblings.Any(x => x.Elements().Any()));


        var result = _mapper.FromResponse<MapperBindBasket>(response, MapperBindEnvelope.Protocol12);


        MapperBindAssert.Succeeded(result);
        Assert.IsNotNull(result.Response.Tags);
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
        Assert.IsNull(result.Response.Tags);
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
    public void FromResponse_ABareSiblingWhereTheItemNameIsDeclared_IsReadAsAnItem_Test()
    {


        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("Basket", "<LocationIds>10</LocationIds>"));

        var result = _mapper.FromResponse<MapperBindBasket>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        Assert.IsNotNull(result.Response.LocationIds);
        CollectionAssert.AreEqual(new[] { 10 }, result.Response.LocationIds.ToArray());
    }

    [TestMethod]
    public void FromResponse_BareSiblingsWhereTheItemNameIsDeclared_AreReadAsItems_Test()
    {
        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload(
                "Basket", "<LocationIds>10</LocationIds><LocationIds>20</LocationIds>"));

        var result = _mapper.FromResponse<MapperBindBasket>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        CollectionAssert.AreEqual(new[] { 10, 20 }, result.Response.LocationIds.ToArray());
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

    [TestMethod]
    public void FromResponse_RepeatedComplexElementsCarryingItemMembers_BindOneItemEach_Test()
    {
        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload(
                "Basket",
                "<Lines><Sku>A-1</Sku><Qty>2</Qty></Lines>"
                + "<Lines><Sku>B-2</Sku><Qty>5</Qty></Lines>"));

        var result = _mapper.FromResponse<MapperBindBasket>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        Assert.AreEqual(2, result.Response.Lines.Count);
        Assert.AreEqual("A-1", result.Response.Lines[0].Sku);
        Assert.AreEqual(2, result.Response.Lines[0].Qty);
        Assert.AreEqual("B-2", result.Response.Lines[1].Sku);
        Assert.AreEqual(5, result.Response.Lines[1].Qty);
    }

    [TestMethod]
    public void FromResponse_ASingleComplexElementCarryingItemMembers_BindsOneItem_Test()
    {
        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("Basket", "<Lines><Sku>A-1</Sku><Qty>2</Qty></Lines>"));

        var result = _mapper.FromResponse<MapperBindBasket>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        Assert.AreEqual(1, result.Response.Lines.Count);
        Assert.AreEqual("A-1", result.Response.Lines[0].Sku);
        Assert.AreEqual(2, result.Response.Lines[0].Qty);
    }

    [TestMethod]
    public void FromResponse_RepeatedComplexElementsWithoutADeclaredItemName_BindOneItemEach_Test()
    {
        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload(
                "Shipment",
                "<Lines><Sku>A-1</Sku><Qty>2</Qty></Lines>"
                + "<Lines><Sku>B-2</Sku><Qty>5</Qty></Lines>"));

        var result = _mapper.FromResponse<MapperFixShipment>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        Assert.AreEqual(2, result.Response.Lines.Count);
        Assert.AreEqual("A-1", result.Response.Lines[0].Sku);
        Assert.AreEqual("B-2", result.Response.Lines[1].Sku);
    }

    [TestMethod]
    public void FromResponse_AWrapperOfComplexItemsWithoutADeclaredItemName_StillBindsEveryItem_Test()
    {
        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload(
                "Shipment",
                "<Lines>"
                + "<Line><Sku>A-1</Sku><Qty>2</Qty></Line>"
                + "<Line><Sku>B-2</Sku><Qty>5</Qty></Line>"
                + "</Lines>"));

        var result = _mapper.FromResponse<MapperFixShipment>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        Assert.AreEqual(2, result.Response.Lines.Count);
        Assert.AreEqual("A-1", result.Response.Lines[0].Sku);
        Assert.AreEqual("B-2", result.Response.Lines[1].Sku);
    }

    [TestMethod]
    public void FromResponse_AWrapperOfSimpleItemsUnderADeclaredItemName_StillBindsEveryItem_Test()
    {
        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload(
                "Basket", "<LocationIds><int>1</int><int>2</int></LocationIds>"));

        var result = _mapper.FromResponse<MapperBindBasket>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        CollectionAssert.AreEqual(new[] { 1, 2 }, result.Response.LocationIds.ToArray());
    }

    [TestMethod]
    public void FromResponse_AWrapperOfComplexItemsUnderADeclaredItemName_StillBindsEveryItem_Test()
    {
        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload(
                "Basket",
                "<Lines>"
                + "<Line><Sku>A-1</Sku><Qty>2</Qty></Line>"
                + "<Line><Sku>B-2</Sku><Qty>5</Qty></Line>"
                + "</Lines>"));

        var result = _mapper.FromResponse<MapperBindBasket>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        Assert.AreEqual(2, result.Response.Lines.Count);
        Assert.AreEqual("A-1", result.Response.Lines[0].Sku);
        Assert.AreEqual("B-2", result.Response.Lines[1].Sku);
    }

    [TestMethod]
    public void FromResponse_AnEmptyWrapperUnderADeclaredItemName_StillBindsAnEmptyCollection_Test()
    {
        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("Basket", "<Id>1</Id><LocationIds />"));

        var result = _mapper.FromResponse<MapperBindBasket>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        Assert.IsNotNull(result.Response.LocationIds);
        Assert.AreEqual(0, result.Response.LocationIds.Count);
    }

    [TestMethod]
    public void FromResponse_AnEmptyWrapperOfComplexItems_StillBindsAnEmptyCollection_Test()
    {
        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("Basket", "<Id>1</Id><Lines />"));

        var result = _mapper.FromResponse<MapperBindBasket>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        Assert.IsNotNull(result.Response.Lines);
        Assert.AreEqual(0, result.Response.Lines.Count);
    }

    [TestMethod]
    public void FromResponse_AWrapperWhoseChildrenMatchNoDeclaredItemName_IsReportedRatherThanBoundEmpty_Test()
    {
        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("Basket", "<LocationIds><other>999</other></LocationIds>"));

        var result = _mapper.FromResponse<MapperBindBasket>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.FailedWithCode(result, MapperBindAssert.CollectionShapeCode);
        MapperBindAssert.MessagesDoNotContain(result, "999", "other");
    }

    [TestMethod]
    public void FromResponse_AComplexCollectionElementCarryingOnlyText_IsReportedRatherThanBoundEmpty_Test()
    {
        var result = BindUnreadableComplexCollection();

        MapperBindAssert.FailedWithCode(result, MapperBindAssert.CollectionShapeCode);
        MapperBindAssert.MessagesDoNotContain(result, "unreadable");
    }

    [TestMethod]
    public void FromResponse_AShapeMismatch_NamesTheMemberItCouldNotBind_Test()
    {
        var result = BindUnreadableComplexCollection();

        MapperBindAssert.FailedWithCode(result, MapperBindAssert.CollectionShapeCode);
        MapperBindAssert.MessagesContain(result, "MapperBindBasket.Lines");
    }

}
