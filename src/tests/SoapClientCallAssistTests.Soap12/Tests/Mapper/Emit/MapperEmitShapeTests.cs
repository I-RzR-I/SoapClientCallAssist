using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Dto.Map;
using SoapClientCallAssist.Mapping;
using SoapClientCallAssistTests.Soap12.Helpers.Mapper;
using SoapClientCallAssistTests.Soap12.Models;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests.Mapper.Emit;

[TestClass]
public class MapperEmitShapeTests
{
    private static readonly XNamespace Op = MapperEmitNs.OperationNamespace;

    [TestMethod]
    public void ToBodies_FlatScalarModel_EmitsOneChildPerMemberInDeclaredOrder_Test()
    {
        var request = new SoapOperationRequest("AddFlatRecord", Op)
            .AddParameter("record", new EmitFlatRecord { Code = "ABC-1", Quantity = 7, Active = true });

        var result = MapperEmitAssert.Mapper.ToBodies(request);

        var root = MapperEmitAssert.SucceedsWithSingleBody(result);

        Assert.AreEqual(Op + "AddFlatRecord", root.Name);

        var record = MapperEmitAssert.Child(root, Op + "record");

        MapperEmitAssert.ChildSequenceIs(record, Op + "code", Op + "quantity", Op + "active");
        MapperEmitAssert.ChildValueIs(record, Op + "code", "ABC-1");
        MapperEmitAssert.ChildValueIs(record, Op + "quantity", "7");
        MapperEmitAssert.ChildValueIs(record, Op + "active", "true");
    }

    [TestMethod]
    public void ToBodies_FlatScalarModel_EmitsATreeTheBodyRebuildWillNotFlatten_Test()
    {
        var request = new SoapOperationRequest("AddFlatRecord", Op)
            .AddParameter("record", new EmitFlatRecord { Code = "ABC-1", Quantity = 7, Active = true });

        var result = MapperEmitAssert.Mapper.ToBodies(request);

        MapperEmitAssert.EveryElementIsDefaultQualified(MapperEmitAssert.SucceedsWithSingleBody(result));
    }

    [TestMethod]
    public void ToBodies_NestedComplexMember_EmitsTheNestedElementWithItsOwnChildren_Test()
    {
        var request = new SoapOperationRequest("PlaceOrder", Op)
            .AddParameter("order", new EmitOrder
            {
                OrderNumber = "ORD-9",
                ShipTo = new EmitAddress { City = "Chisinau", PostCode = "MD-2001" }
            });

        var result = MapperEmitAssert.Mapper.ToBodies(request);

        var root = MapperEmitAssert.SucceedsWithSingleBody(result);
        var order = MapperEmitAssert.Child(root, Op + "order");

        MapperEmitAssert.ChildSequenceIs(order, Op + "orderNumber", Op + "shipTo");
        MapperEmitAssert.ChildValueIs(order, Op + "orderNumber", "ORD-9");

        var shipTo = MapperEmitAssert.Child(order, Op + "shipTo");

        MapperEmitAssert.ChildSequenceIs(shipTo, Op + "city", Op + "postCode");
        MapperEmitAssert.ChildValueIs(shipTo, Op + "city", "Chisinau");
        MapperEmitAssert.ChildValueIs(shipTo, Op + "postCode", "MD-2001");

        MapperEmitAssert.EveryElementIsDefaultQualified(root);
    }

    [TestMethod]
    public void ToBodies_SecondParameterIsAListOfInt_EmitsAWrapperOfQualifiedIntItems_Test()
    {
        var request = new SoapOperationRequest("AddRecordWithLocations", Op)
            .AddParameter("record", new EmitFlatRecord { Code = "ABC-2", Quantity = 1, Active = false })
            .AddParameter("associatedLocationIds", new List<int> { 11, 22, 33 });

        var result = MapperEmitAssert.Mapper.ToBodies(request);

        var root = MapperEmitAssert.SucceedsWithSingleBody(result);

        MapperEmitAssert.ChildSequenceIs(root, Op + "record", Op + "associatedLocationIds");

        var ids = MapperEmitAssert.Child(root, Op + "associatedLocationIds");

        MapperEmitAssert.ChildSequenceIs(ids, Op + "int", Op + "int", Op + "int");

        CollectionAssert.AreEqual(new[] { "11", "22", "33" }, ids.Elements().Select(x => x.Value).ToArray());
    }

    [TestMethod]
    public void ToBodies_SecondParameterIsAListOfInt_EmitsEveryItemNamespaceQualified_Test()
    {
        var request = new SoapOperationRequest("AddRecordWithLocations", Op)
            .AddParameter("record", new EmitFlatRecord { Code = "ABC-2", Quantity = 1, Active = false })
            .AddParameter("associatedLocationIds", new List<int> { 11, 22, 33 });

        var result = MapperEmitAssert.Mapper.ToBodies(request);

        var root = MapperEmitAssert.SucceedsWithSingleBody(result);

        MapperEmitAssert.EveryElementIsDefaultQualified(root);

        foreach (var item in MapperEmitAssert.Child(root, Op + "associatedLocationIds").Elements())
            Assert.AreEqual(MapperEmitNs.Operation, item.Name.NamespaceName, $"{item.Name}");
    }

    [TestMethod]
    public void ToBodies_CollectionOfComplexItems_NamesEachItemAfterItsContract_Test()
    {
        var request = new SoapOperationRequest("SubmitBasket", Op)
            .AddParameter("basket", new EmitBasket
            {
                BasketId = "BSK-1",
                Lines =
                {
                    new EmitBasketLine { Sku = "SKU-A", Count = 2 },
                    new EmitBasketLine { Sku = "SKU-B", Count = 5 }
                }
            });

        var result = MapperEmitAssert.Mapper.ToBodies(request);

        var root = MapperEmitAssert.SucceedsWithSingleBody(result);
        var basket = MapperEmitAssert.Child(root, Op + "basket");

        MapperEmitAssert.ChildSequenceIs(basket, Op + "basketId", Op + "lines");

        var lines = MapperEmitAssert.Child(basket, Op + "lines");

        MapperEmitAssert.ChildSequenceIs(lines, Op + "basketLine", Op + "basketLine");

        var first = lines.Elements().First();
        var second = lines.Elements().Last();

        MapperEmitAssert.ChildValueIs(first, Op + "sku", "SKU-A");
        MapperEmitAssert.ChildValueIs(first, Op + "count", "2");
        MapperEmitAssert.ChildValueIs(second, Op + "sku", "SKU-B");
        MapperEmitAssert.ChildValueIs(second, Op + "count", "5");

        MapperEmitAssert.EveryElementIsDefaultQualified(root);
    }
}
