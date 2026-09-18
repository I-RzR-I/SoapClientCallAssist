
#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Dto.Map;
using SoapClientCallAssist.Mapping;
using SoapClientCallAssistTests.Soap12.Enums;
using SoapClientCallAssistTests.Soap12.Helpers.Mapper;
using SoapClientCallAssistTests.Soap12.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests.Mapper.Bind;

[TestClass]
public sealed class MapperBindRoundTripTests
{
    private static readonly Guid Reference = Guid.Parse("9f1c1a6e-6d4b-4c33-9d0f-1f0a2b3c4d5e");

    private static readonly DateTime ReleasedOn = new DateTime(2024, 3, 5, 6, 7, 8, DateTimeKind.Utc);

    private readonly ISoapModelMapper _mapper = new SoapModelMapper();

    private static MapperBindProduct FullyPopulatedProduct()
        => new MapperBindProduct
        {
            Code = "SKU-1",
            Quantity = 3,
            Price = 19.95m,
            ReleasedOn = ReleasedOn,
            Reference = Reference,
            Status = MapperBindStatus.Active,
            Detail = new MapperBindDetail { Description = "boxed", Weight = 1.5 },
            LocationIds = new List<int> { 10, 20, 30 },
            Lines = new List<MapperBindLine>
            {
                new MapperBindLine { Sku = "A-1", Qty = 2 },
                new MapperBindLine { Sku = "B-2", Qty = 5 }
            }
        };

    private XElement Emit(MapperBindProduct product)
    {
        var request = new SoapOperationRequest("AddProduct", XNamespace.Get(MapperBindNs.Contract))
            .AddParameter("product", product);

        var emitted = _mapper.ToBodies(request);
        MapperBindAssert.Succeeded(emitted);

        var root = emitted.Response.Single();

        Assert.AreEqual(XName.Get("AddProduct", MapperBindNs.Contract), root.Name);
        Assert.IsNotNull(root.Element(XName.Get("product", MapperBindNs.Contract)));

        return root;
    }

    private static void AssertProductsMatch(MapperBindProduct expected, MapperBindProduct actual)
    {
        Assert.IsNotNull(actual);
        Assert.AreEqual(expected.Code, actual.Code);
        Assert.AreEqual(expected.Quantity, actual.Quantity);
        Assert.AreEqual(expected.Price, actual.Price);
        Assert.AreEqual(expected.ReleasedOn.Ticks, actual.ReleasedOn.Ticks);
        Assert.AreEqual(expected.ReleasedOn.Kind, actual.ReleasedOn.Kind);
        Assert.AreEqual(expected.Reference, actual.Reference);
        Assert.AreEqual(expected.Status, actual.Status);

        Assert.IsNotNull(actual.Detail);
        Assert.AreEqual(expected.Detail.Description, actual.Detail.Description);
        Assert.AreEqual(expected.Detail.Weight, actual.Detail.Weight, 0d);

        Assert.IsNotNull(actual.LocationIds);
        CollectionAssert.AreEqual(expected.LocationIds.ToArray(), actual.LocationIds.ToArray());

        Assert.IsNotNull(actual.Lines);
        Assert.AreEqual(expected.Lines.Count, actual.Lines.Count);
        for (var index = 0; index < expected.Lines.Count; index++)
        {
            Assert.AreEqual(expected.Lines[index].Sku, actual.Lines[index].Sku);
            Assert.AreEqual(expected.Lines[index].Qty, actual.Lines[index].Qty);
        }
    }

    [TestMethod]
    public void RoundTrip_FullyPopulatedModelThroughTheOperationWrapper_RebindsEveryMember_Test()
    {
        var product = FullyPopulatedProduct();
        var emitted = Emit(product);
        var response = MapperBindEnvelope.Wrap12(emitted.ToString(SaveOptions.DisableFormatting));

        var result = _mapper.FromResponse<MapperBindAddProductResult>(
            response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        AssertProductsMatch(product, result.Response.Product);
    }

    [TestMethod]
    public void RoundTrip_FullyPopulatedModelBoundDirectlyFromTheParameterElement_RebindsEveryMember_Test()
    {
        var product = FullyPopulatedProduct();
        var parameterElement = Emit(product).Element(XName.Get("product", MapperBindNs.Contract));
        var response = MapperBindEnvelope.Wrap12(parameterElement!.ToString(SaveOptions.DisableFormatting));

        var result = _mapper.FromResponse<MapperBindProduct>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        AssertProductsMatch(product, result.Response);
    }

    [TestMethod]
    public void RoundTrip_EmittedTreeIndentedByTheServiceFormatter_StillRebindsEveryMember_Test()
    {
        var product = FullyPopulatedProduct();
        var indented = Emit(product).ToString(SaveOptions.None);

        Assert.IsTrue(indented.Contains("\n"));

        var response = MapperBindEnvelope.Wrap12("\r\n  " + indented + "\r\n");

        var result = _mapper.FromResponse<MapperBindAddProductResult>(
            response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        AssertProductsMatch(product, result.Response.Product);
    }

    [TestMethod]
    public void RoundTrip_EmptyStringMember_ComesBackAsTheEmptyString_Test()
    {
        var product = FullyPopulatedProduct();
        product.Code = string.Empty;
        var response = MapperBindEnvelope.Wrap12(Emit(product).ToString(SaveOptions.DisableFormatting));

        var result = _mapper.FromResponse<MapperBindAddProductResult>(
            response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        Assert.AreEqual(string.Empty, result.Response.Product.Code);
    }

    [TestMethod]
    public void RoundTrip_NullReferenceMemberOnATypeWithNoInitializer_ComesBackNull_Test()
    {
        var product = FullyPopulatedProduct();
        product.Code = null;
        var emitted = Emit(product);

        Assert.IsNull(emitted.Descendants(XName.Get("Code", MapperBindNs.Contract)).FirstOrDefault());

        var response = MapperBindEnvelope.Wrap12(emitted.ToString(SaveOptions.DisableFormatting));

        var result = _mapper.FromResponse<MapperBindAddProductResult>(
            response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        Assert.IsNull(result.Response.Product.Code);
    }

    [TestMethod]
    public void RoundTrip_EmptyCollection_ComesBackNullAndIsOutsideTheSubset_Test()
    {
        var product = FullyPopulatedProduct();
        product.LocationIds = new List<int>();
        var emitted = Emit(product);

        Assert.IsNull(emitted.Descendants(XName.Get("LocationIds", MapperBindNs.Contract)).FirstOrDefault());

        var response = MapperBindEnvelope.Wrap12(emitted.ToString(SaveOptions.DisableFormatting));

        var result = _mapper.FromResponse<MapperBindAddProductResult>(
            response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        Assert.IsNull(result.Response.Product.LocationIds);
    }

    [TestMethod]
    public void RoundTrip_CollectionHoldingANullItem_LosesThatItemAndIsOutsideTheSubset_Test()
    {
        var product = FullyPopulatedProduct();
        product.Lines = new List<MapperBindLine>
        {
            new MapperBindLine { Sku = "A-1", Qty = 2 },
            null,
            new MapperBindLine { Sku = "B-2", Qty = 5 }
        };

        var emitted = Emit(product);

        Assert.AreEqual(2, emitted.Descendants(XName.Get("Line", MapperBindNs.Contract)).Count());

        var response = MapperBindEnvelope.Wrap12(emitted.ToString(SaveOptions.DisableFormatting));

        var result = _mapper.FromResponse<MapperBindAddProductResult>(
            response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(result);
        Assert.AreEqual(2, result.Response.Product.Lines.Count);
        Assert.AreEqual("A-1", result.Response.Product.Lines[0].Sku);
        Assert.AreEqual("B-2", result.Response.Product.Lines[1].Sku);
    }

    [TestMethod]
    public void RoundTrip_RepeatedForTheSameModel_IsStable_Test()
    {
        var product = FullyPopulatedProduct();
        var response = MapperBindEnvelope.Wrap12(Emit(product).ToString(SaveOptions.DisableFormatting));

        var first = _mapper.FromResponse<MapperBindAddProductResult>(response, MapperBindEnvelope.Protocol12);
        var second = _mapper.FromResponse<MapperBindAddProductResult>(response, MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(first);
        MapperBindAssert.Succeeded(second);
        AssertProductsMatch(product, first.Response.Product);
        AssertProductsMatch(product, second.Response.Product);
        Assert.AreNotSame(first.Response.Product, second.Response.Product);
    }

    [TestMethod]
    public void RoundTrip_ThroughEveryEnvelopePrefix_ProducesTheSameObject_Test()
    {
        var product = FullyPopulatedProduct();
        var payload = Emit(product).ToString(SaveOptions.DisableFormatting);

        var withSoap = _mapper.FromResponse<MapperBindAddProductResult>(
            MapperBindEnvelope.Wrap12(payload, "soap"), MapperBindEnvelope.Protocol12);
        var withS = _mapper.FromResponse<MapperBindAddProductResult>(
            MapperBindEnvelope.Wrap12(payload, "s"), MapperBindEnvelope.Protocol12);
        var withEnv = _mapper.FromResponse<MapperBindAddProductResult>(
            MapperBindEnvelope.Wrap12(payload, "env"), MapperBindEnvelope.Protocol12);

        MapperBindAssert.Succeeded(withSoap);
        MapperBindAssert.Succeeded(withS);
        MapperBindAssert.Succeeded(withEnv);
        AssertProductsMatch(product, withSoap.Response.Product);
        AssertProductsMatch(product, withS.Response.Product);
        AssertProductsMatch(product, withEnv.Response.Product);
    }
}
