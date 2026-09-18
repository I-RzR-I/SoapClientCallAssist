
#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Mapping;
using SoapClientCallAssistTests.Soap12.Helpers.Mapper;
using SoapClientCallAssistTests.Soap12.Models;
using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests.Mapper.Bind;

[TestClass]
public sealed class MapperBindHappyPathTests
{
    private readonly ISoapModelMapper _mapper = new SoapModelMapper();

    [TestMethod]
    public void FromResponse_FlatScalarPayload_BindsEveryScalarMember_Test()
    {
        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload(
                "Flat",
                "<Id>42</Id>"
                + "<Name>widget</Name>"
                + "<Ratio>12.5</Ratio>"
                + "<Active>true</Active>"
                + "<Reference>9f1c1a6e-6d4b-4c33-9d0f-1f0a2b3c4d5e</Reference>"
                + "<CreatedOn>2024-03-05T06:07:08Z</CreatedOn>"));

        var result = _mapper.FromResponse<MapperBindFlat>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        Assert.AreEqual(42, result.Response.Id);
        Assert.AreEqual("widget", result.Response.Name);
        Assert.AreEqual(12.5m, result.Response.Ratio);
        Assert.IsTrue(result.Response.Active);
        Assert.AreEqual(Guid.Parse("9f1c1a6e-6d4b-4c33-9d0f-1f0a2b3c4d5e"), result.Response.Reference);
        Assert.AreEqual(
            new DateTime(2024, 3, 5, 6, 7, 8, DateTimeKind.Utc).Ticks,
            result.Response.CreatedOn.Ticks);
        Assert.AreEqual(DateTimeKind.Utc, result.Response.CreatedOn.Kind);
    }

    [TestMethod]
    public void FromResponse_ScalarTextIsParsedCultureInvariantly_UsesXmlRules_Test()
    {
        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("Flat", "<Ratio>1234.56</Ratio><Active>1</Active>"));

        var result = _mapper.FromResponse<MapperBindFlat>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        Assert.AreEqual(1234.56m, result.Response.Ratio);

        Assert.IsTrue(result.Response.Active);
    }

    [TestMethod]
    public void FromResponse_UnderACommaDecimalCulture_StillReadsXmlLexicalForms_Test()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload(
                "Flat",
                "<Ratio>1234.56</Ratio><CreatedOn>2024-03-05T06:07:08Z</CreatedOn>"));

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            var result = _mapper.FromResponse<MapperBindFlat>(response, MapperBindEnvelope.Protocol12);

            MapperBindAssert.Succeeded(result);
            Assert.AreEqual(1234.56m, result.Response.Ratio);
            Assert.AreEqual(
                new DateTime(2024, 3, 5, 6, 7, 8, DateTimeKind.Utc).Ticks,
                result.Response.CreatedOn.Ticks);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [TestMethod]
    public void FromResponse_ScalarTextCarriesSurroundingWhitespace_TrimsValuesButKeepsStrings_Test()
    {
        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("Flat", "<Id>  42  </Id><Name>  padded  </Name>"));

        var result = _mapper.FromResponse<MapperBindFlat>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        Assert.AreEqual(42, result.Response.Id);

        Assert.AreEqual("  padded  ", result.Response.Name);
    }

    [TestMethod]
    public void FromResponse_NestedComplexMember_BindsTheChildObject_Test()
    {
        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload(
                "Order",
                "<Code>ORD-1</Code>"
                + "<Customer><FullName>Ada</FullName><Age>36</Age></Customer>"));

        var result = _mapper.FromResponse<MapperBindOrder>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        Assert.AreEqual("ORD-1", result.Response.Code);
        Assert.IsNotNull(result.Response.Customer);
        Assert.AreEqual("Ada", result.Response.Customer.FullName);
        Assert.AreEqual(36, result.Response.Customer.Age);
    }

    [TestMethod]
    public void FromResponse_MembersDeclaringAPath_BindThroughTheNamedContainers_Test()
    {
        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload(
                "Pathed",
                "<Id>7</Id>"
                + "<Address>"
                + "<Primary><City>Riga</City><Zip>LV-1001</Zip></Primary>"
                + "<Backup><City>Vilnius</City></Backup>"
                + "</Address>"));

        var result = _mapper.FromResponse<MapperBindPathed>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        Assert.AreEqual(7, result.Response.Id);
        Assert.AreEqual("Riga", result.Response.City);
        Assert.AreEqual("LV-1001", result.Response.Zip);

        Assert.AreEqual("Vilnius", result.Response.BackupCity);
    }

    [TestMethod]
    public void FromResponse_PathSegmentIsMissing_LeavesTheMemberAtItsDefaultWithoutFailing_Test()
    {
        var response = MapperBindEnvelope.Wrap12(MapperBindEnvelope.Payload("Pathed", "<Id>7</Id>"));

        var result = _mapper.FromResponse<MapperBindPathed>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        Assert.AreEqual(7, result.Response.Id);
        Assert.IsNull(result.Response.City);
        Assert.IsNull(result.Response.Zip);
    }

    [TestMethod]
    public void FromResponse_PathSegmentsMatchByLocalNameOnly_IgnoreTheContainerNamespace_Test()
    {
        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload(
                "Pathed",
                "<Id>7</Id>"
                + "<Address xmlns=\"urn:some-other-namespace\">"
                + "<Primary><City>Riga</City><Zip>LV-1001</Zip></Primary>"
                + "</Address>"));

        var result = _mapper.FromResponse<MapperBindPathed>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        Assert.AreEqual("Riga", result.Response.City);
        Assert.AreEqual("LV-1001", result.Response.Zip);
    }

    [TestMethod]
    public void FromResponse_CollectionOfPrimitives_BindsEveryItemInOrder_Test()
    {
        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload(
                "Basket",
                "<Id>1</Id>"
                + "<LocationIds><int>10</int><int>20</int><int>30</int></LocationIds>"));

        var result = _mapper.FromResponse<MapperBindBasket>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, result.Response.LocationIds.ToArray());
    }

    [TestMethod]
    public void FromResponse_CollectionWithoutAnItemName_TakesEveryElementChildAsAnItem_Test()
    {
        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload(
                "Basket",
                "<Tags><string>alpha</string><anything>beta</anything></Tags>"));

        var result = _mapper.FromResponse<MapperBindBasket>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        CollectionAssert.AreEqual(new[] { "alpha", "beta" }, result.Response.Tags);
    }

    [TestMethod]
    public void FromResponse_CollectionOfComplexItems_BindsEachItemObject_Test()
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
        Assert.AreEqual(2, result.Response.Lines[0].Qty);
        Assert.AreEqual("B-2", result.Response.Lines[1].Sku);
        Assert.AreEqual(5, result.Response.Lines[1].Qty);
    }

    [TestMethod]
    public void FromResponse_CollectionWrapperCarriesForeignItemNames_SkipsThemWhenAnItemNameIsDeclared_Test()
    {
        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload(
                "Basket",
                "<LocationIds><int>10</int><other>999</other><int>20</int></LocationIds>"));

        var result = _mapper.FromResponse<MapperBindBasket>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        CollectionAssert.AreEqual(new[] { 10, 20 }, result.Response.LocationIds.ToArray());
    }

    [TestMethod]
    public void FromResponse_EmptyCollectionWrapper_BindsAnEmptyCollectionRatherThanNull_Test()
    {
        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("Basket", "<Id>1</Id><LocationIds />"));

        var result = _mapper.FromResponse<MapperBindBasket>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        Assert.IsNotNull(result.Response.LocationIds);
        Assert.AreEqual(0, result.Response.LocationIds.Count);
    }

    [TestMethod]
    public void FromResponse_AbsentCollectionWrapper_LeavesTheCollectionNull_Test()
    {
        var response = MapperBindEnvelope.Wrap12(MapperBindEnvelope.Payload("Basket", "<Id>1</Id>"));

        var result = _mapper.FromResponse<MapperBindBasket>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        Assert.IsNull(result.Response.LocationIds);
    }

    [TestMethod]
    public void FromResponse_RepeatedCollectionWrappers_MergeIntoOneCollection_Test()
    {
        var payload = MapperBindEnvelope.Payload(
            "Basket",
            "<LocationIds><int>10</int></LocationIds><LocationIds><int>20</int></LocationIds>");
        var response = MapperBindEnvelope.Wrap12(payload);

        var result = _mapper.FromResponse<MapperBindBasket>(response, MapperBindEnvelope.Protocol12);

        Assert.AreEqual(
            2,
            XDocument.Parse(response)
                .Descendants(XName.Get("LocationIds", MapperBindNs.Contract))
                .Count());

        MapperBindAssert.Succeeded(result);
        CollectionAssert.AreEqual(new[] { 10, 20 }, result.Response.LocationIds.ToArray());
    }

    [TestMethod]
    public void FromResponse_DataMemberDecoratedType_BindsThroughTheDataContractFallback_Test()
    {
        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload("Record", "<Id>11</Id><Label>fallback</Label>"));

        var result = _mapper.FromResponse<MapperBindDataRecord>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        Assert.AreEqual(11, result.Response.Id);
        Assert.AreEqual("fallback", result.Response.Label);
    }

    [TestMethod]
    public void FromResponse_DataMemberTypeWithIgnoredAndUnmappedMembers_LeavesThemUnbound_Test()
    {
        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload(
                "Record",
                "<Id>11</Id><Secret>should-not-bind</Secret><Unmapped>should-not-bind</Unmapped>"));

        var result = _mapper.FromResponse<MapperBindDataRecord>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        Assert.AreEqual(11, result.Response.Id);
        Assert.IsNull(result.Response.Secret);
        Assert.IsNull(result.Response.Unmapped);
    }

    [TestMethod]
    public void FromResponse_UnrecognisedElement_IsIgnoredWithoutFailing_Test()
    {
        var response = MapperBindEnvelope.Wrap12(
            MapperBindEnvelope.Payload(
                "Flat",
                "<Id>42</Id><SomethingTheContractNeverDeclared>x</SomethingTheContractNeverDeclared>"));

        var result = _mapper.FromResponse<MapperBindFlat>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        Assert.AreEqual(42, result.Response.Id);
    }

    [TestMethod]
    public void FromResponse_PayloadElementsInAForeignNamespace_StillBindByLocalName_Test()
    {
        var response = MapperBindEnvelope.Wrap12(
            "<Flat xmlns=\"urn:a-namespace-the-model-never-declares\"><Id>42</Id><Name>widget</Name></Flat>");

        var result = _mapper.FromResponse<MapperBindFlat>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        Assert.AreEqual(42, result.Response.Id);
        Assert.AreEqual("widget", result.Response.Name);
    }

    [TestMethod]
    public void FromResponse_NullProtocolNamespace_StillLocatesTheBody_Test()
    {
        var response = MapperBindEnvelope.Wrap12(MapperBindEnvelope.Payload("Flat", "<Id>42</Id>"));

        var result = _mapper.FromResponse<MapperBindFlat>(response, null);

        MapperBindAssert.Succeeded(result);
        Assert.AreEqual(42, result.Response.Id);
    }

    [TestMethod]
    public void FromResponse_CalledTwiceForTheSameType_ReturnsIndependentInstances_Test()
    {
        var first = MapperBindEnvelope.Wrap12(MapperBindEnvelope.Payload("Flat", "<Id>1</Id><Name>one</Name>"));
        var second = MapperBindEnvelope.Wrap12(MapperBindEnvelope.Payload("Flat", "<Id>2</Id>"));

        var firstResult = _mapper.FromResponse<MapperBindFlat>(first, MapperBindEnvelope.Protocol12);
        var secondResult = _mapper.FromResponse<MapperBindFlat>(second, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(firstResult);
        MapperBindAssert.Succeeded(secondResult);
        Assert.AreNotSame(firstResult.Response, secondResult.Response);
        Assert.AreEqual(1, firstResult.Response.Id);
        Assert.AreEqual("one", firstResult.Response.Name);
        Assert.AreEqual(2, secondResult.Response.Id);
        Assert.IsNull(secondResult.Response.Name);
    }
}
