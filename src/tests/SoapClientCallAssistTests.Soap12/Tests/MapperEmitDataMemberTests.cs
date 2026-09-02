using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Dto.Map;
using SoapClientCallAssistTests.Soap12.Helpers;
using SoapClientCallAssistTests.Soap12.Models;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests;

[TestClass]
public class MapperEmitDataMemberTests
{
    private static readonly XNamespace Op = MapperEmitNs.OperationNamespace;

    private static readonly XNamespace Explicit = MapperEmitNs.ExplicitNamespace;

    [TestMethod]
    public void ToBodies_DataMemberModel_MapsDecoratedMembersAndHonoursTheDeclaredName_Test()
    {
        var request = new SoapOperationRequest("SaveFallback", Op)
            .AddParameter("model", new EmitDataMemberInheritedNamespace
            {
                Alpha = "a-value",
                Beta = "b-value",
                IgnoredMember = "must-not-appear",
                UndecoratedMember = "must-not-appear-either"
            });

        var result = MapperEmitAssert.Mapper.ToBodies(request);

        var root = MapperEmitAssert.SucceedsWithSingleBody(result);
        var model = MapperEmitAssert.Child(root, Op + "model");

        MapperEmitAssert.ChildSequenceIs(model, Op + "alpha", Op + "Beta");
        MapperEmitAssert.ChildValueIs(model, Op + "alpha", "a-value");
        MapperEmitAssert.ChildValueIs(model, Op + "Beta", "b-value");

        MapperEmitAssert.EveryElementIsDefaultQualified(root);
    }

    [TestMethod]
    public void ToBodies_DataMemberModel_HonoursIgnoreDataMemberAndSkipsUndecoratedProperties_Test()
    {
        var request = new SoapOperationRequest("SaveFallback", Op)
            .AddParameter("model", new EmitDataMemberInheritedNamespace
            {
                Alpha = "a-value",
                Beta = "b-value",
                IgnoredMember = "must-not-appear",
                UndecoratedMember = "must-not-appear-either"
            });

        var result = MapperEmitAssert.Mapper.ToBodies(request);

        var root = MapperEmitAssert.SucceedsWithSingleBody(result);

        MapperEmitAssert.NoElementNamed(root, Op + "ignoredOnWire");
        MapperEmitAssert.NoElementNamed(root, Op + nameof(EmitDataMemberInheritedNamespace.IgnoredMember));
        MapperEmitAssert.NoElementNamed(root, Op + nameof(EmitDataMemberInheritedNamespace.UndecoratedMember));

        MapperEmitAssert.XmlDoesNotContain(root, "must-not-appear", "must-not-appear-either");
    }

    [TestMethod]
    public void ToBodies_DataContractWithNoNamespace_InheritsTheCallSiteNamespace_Test()
    {
        var request = new SoapOperationRequest("SaveFallback", Op)
            .AddParameter("model", new EmitDataMemberInheritedNamespace { Alpha = "a-value", Beta = "b-value" });

        var result = MapperEmitAssert.Mapper.ToBodies(request);

        var root = MapperEmitAssert.SucceedsWithSingleBody(result);
        var model = MapperEmitAssert.Child(root, Op + "model");

        foreach (var child in model.Elements())
            Assert.AreEqual(
                MapperEmitNs.Operation,
                child.Name.NamespaceName,
                $"Member '{child.Name.LocalName}' left the call-site namespace.");

        MapperEmitAssert.XmlDoesNotContain(root, MapperEmitNs.DataContractDefaultPrefix);
    }

    [TestMethod]
    public void ToBodies_DataContractWithExplicitNamespace_EmitsItsMembersInThatNamespace_Test()
    {
        var request = new SoapOperationRequest("SaveExplicitFallback", Op)
            .AddParameter("model", new EmitDataMemberExplicitNamespace { Gamma = "g-value", Delta = "d-value" });

        var result = MapperEmitAssert.Mapper.ToBodies(request);

        var root = MapperEmitAssert.SucceedsWithSingleBody(result);

        var model = MapperEmitAssert.Child(root, Op + "model");

        MapperEmitAssert.ChildSequenceIs(model, Explicit + "gamma", Explicit + "delta");
        MapperEmitAssert.ChildValueIs(model, Explicit + "gamma", "g-value");
        MapperEmitAssert.ChildValueIs(model, Explicit + "delta", "d-value");

        MapperEmitAssert.EveryElementIsDefaultQualified(root);
    }

    [TestMethod]
    public void ToBodies_DataContractWithExplicitName_UsesItAsThePerItemElementName_Test()
    {
        var request = new SoapOperationRequest("SaveFallbackItems", Op)
            .AddParameter("model", new EmitDataMemberOwner
            {
                Items =
                {
                    new EmitDataMemberExplicitNamespace { Gamma = "g1", Delta = "d1" },
                    new EmitDataMemberExplicitNamespace { Gamma = "g2", Delta = "d2" }
                }
            });

        var result = MapperEmitAssert.Mapper.ToBodies(request);

        var root = MapperEmitAssert.SucceedsWithSingleBody(result);
        var items = MapperEmitAssert.Child(MapperEmitAssert.Child(root, Op + "model"), Op + "items");

        MapperEmitAssert.ChildSequenceIs(items, Explicit + "explicitContract", Explicit + "explicitContract");
        MapperEmitAssert.XmlDoesNotContain(root, nameof(EmitDataMemberExplicitNamespace));

        MapperEmitAssert.EveryElementIsDefaultQualified(root);
    }
}
