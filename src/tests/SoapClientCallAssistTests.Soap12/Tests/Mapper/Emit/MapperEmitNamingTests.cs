using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Dto.Map;
using SoapClientCallAssistTests.Soap12.Helpers.Mapper;
using SoapClientCallAssistTests.Soap12.Models;
using System.Linq;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests.Mapper.Emit;

[TestClass]
public class MapperEmitNamingTests
{
    private static readonly XNamespace Op = MapperEmitNs.OperationNamespace;

    private static readonly XNamespace Product = MapperEmitNs.ProductNamespace;

    [TestMethod]
    public void ToBodies_MembersWithCustomNames_EmitsTheWireNames_Test()
    {
        var request = new SoapOperationRequest("SendRenamed", Op)
            .AddParameter("payload", new EmitRenamedOwner
            {
                ClrScalarProperty = "scalar-value",
                ClrCollectionProperty =
                {
                    new EmitRenamedItem { ClrItemProperty = "item-value" }
                }
            });

        var result = MapperEmitAssert.Mapper.ToBodies(request);

        var root = MapperEmitAssert.SucceedsWithSingleBody(result);
        var payload = MapperEmitAssert.Child(root, Op + "payload");

        MapperEmitAssert.ChildSequenceIs(payload, Op + "scalarOnWire", Op + "collectionOnWire");
        MapperEmitAssert.ChildValueIs(payload, Op + "scalarOnWire", "scalar-value");

        var collection = MapperEmitAssert.Child(payload, Op + "collectionOnWire");
        var item = MapperEmitAssert.Child(collection, Op + "itemOnWire");

        MapperEmitAssert.ChildValueIs(item, Op + "itemScalarOnWire", "item-value");
    }

    [TestMethod]
    public void ToBodies_MembersWithCustomNames_LeavesNoClrIdentifierInTheXml_Test()
    {
        var request = new SoapOperationRequest("SendRenamed", Op)
            .AddParameter("payload", new EmitRenamedOwner
            {
                ClrScalarProperty = "scalar-value",
                ClrCollectionProperty =
                {
                    new EmitRenamedItem { ClrItemProperty = "item-value" }
                }
            });

        var result = MapperEmitAssert.Mapper.ToBodies(request);

        var root = MapperEmitAssert.SucceedsWithSingleBody(result);

        MapperEmitAssert.XmlDoesNotContain(
            root,
            nameof(EmitRenamedOwner),
            nameof(EmitRenamedItem),
            nameof(EmitRenamedOwner.ClrScalarProperty),
            nameof(EmitRenamedOwner.ClrCollectionProperty),
            nameof(EmitRenamedItem.ClrItemProperty));
    }

    [TestMethod]
    public void ToBodies_ContractWithCustomName_UsesItAsThePerItemElementName_Test()
    {
        var request = new SoapOperationRequest("SendRenamed", Op)
            .AddParameter("payload", new EmitRenamedOwner
            {
                ClrScalarProperty = "scalar-value",
                ClrCollectionProperty =
                {
                    new EmitRenamedItem { ClrItemProperty = "first" },
                    new EmitRenamedItem { ClrItemProperty = "second" }
                }
            });

        var result = MapperEmitAssert.Mapper.ToBodies(request);

        var root = MapperEmitAssert.SucceedsWithSingleBody(result);
        var collection = MapperEmitAssert.Child(
            MapperEmitAssert.Child(root, Op + "payload"),
            Op + "collectionOnWire");

        MapperEmitAssert.ChildSequenceIs(collection, Op + "itemOnWire", Op + "itemOnWire");
    }

    [TestMethod]
    public void ToBodies_CollectionMemberWithItemName_OverridesThePerItemElementName_Test()
    {
        var request = new SoapOperationRequest("SendIds", Op)
            .AddParameter("payload", new EmitItemNameOwner { ClrIdsProperty = { 4, 9 } });

        var result = MapperEmitAssert.Mapper.ToBodies(request);

        var root = MapperEmitAssert.SucceedsWithSingleBody(result);
        var ids = MapperEmitAssert.Child(MapperEmitAssert.Child(root, Op + "payload"), Op + "idsOnWire");

        MapperEmitAssert.ChildSequenceIs(ids, Op + "identifier", Op + "identifier");

        CollectionAssert.AreEqual(new[] { "4", "9" }, ids.Elements().Select(x => x.Value).ToArray());

        MapperEmitAssert.NoElementNamed(root, Op + "int");
        MapperEmitAssert.XmlDoesNotContain(root, nameof(EmitItemNameOwner.ClrIdsProperty));

        MapperEmitAssert.EveryElementIsDefaultQualified(root);
    }

    [TestMethod]
    public void ToBodies_NestedContractWithItsOwnNamespace_EmitsItsMembersInThatNamespace_Test()
    {
        var request = new SoapOperationRequest("AddCatalogueEntry", Op)
            .AddParameter("entry", new EmitCatalogueEntry
            {
                EntryId = "CAT-1",
                Product = new EmitProductContract { Name = "Widget", Price = 19.5m }
            });

        var result = MapperEmitAssert.Mapper.ToBodies(request);

        var root = MapperEmitAssert.SucceedsWithSingleBody(result);
        var entry = MapperEmitAssert.Child(root, Op + "entry");

        MapperEmitAssert.ChildSequenceIs(entry, Op + "entryId", Op + "product");

        var product = MapperEmitAssert.Child(entry, Op + "product");

        MapperEmitAssert.ChildSequenceIs(product, Product + "name", Product + "price");
        MapperEmitAssert.ChildValueIs(product, Product + "name", "Widget");
        MapperEmitAssert.ChildValueIs(product, Product + "price", "19.5");
    }

    [TestMethod]
    public void ToBodies_NestedContractWithItsOwnNamespace_DeclaresBothNamespacesOnTheWire_Test()
    {
        var request = new SoapOperationRequest("AddCatalogueEntry", Op)
            .AddParameter("entry", new EmitCatalogueEntry
            {
                EntryId = "CAT-2",
                Product = new EmitProductContract { Name = "Widget", Price = 19.5m }
            });

        var result = MapperEmitAssert.Mapper.ToBodies(request);

        var root = MapperEmitAssert.SucceedsWithSingleBody(result);

        MapperEmitAssert.XmlContains(
            root,
            $"xmlns=\"{MapperEmitNs.Operation}\"",
            $"xmlns=\"{MapperEmitNs.Product}\"");

        MapperEmitAssert.EveryElementIsDefaultQualified(root);
    }
}
