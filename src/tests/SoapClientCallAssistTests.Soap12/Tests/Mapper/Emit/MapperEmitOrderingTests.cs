using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Dto.Map;
using SoapClientCallAssist.Mapping;
using SoapClientCallAssistTests.Soap12.Helpers.Mapper;
using SoapClientCallAssistTests.Soap12.Models;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests.Mapper.Emit;

[TestClass]
public class MapperEmitOrderingTests
{
    private static readonly XNamespace Op = MapperEmitNs.OperationNamespace;

    [TestMethod]
    public void ToBodies_InheritedModel_EmitsBaseDeclaredMembersBeforeDerivedOnes_Test()
    {
        var request = new SoapOperationRequest("SaveOrdering", Op)
            .AddParameter("model", new EmitOrderingDerived
            {
                BaseAlpha = "a",
                BaseBeta = "b",
                DerivedGamma = "g",
                DerivedDelta = "d"
            });

        var result = MapperEmitAssert.Mapper.ToBodies(request);

        var model = MapperEmitAssert.Child(MapperEmitAssert.SucceedsWithSingleBody(result), Op + "model");

        MapperEmitAssert.ChildSequenceIs(
            model,
            Op + "baseAlpha",
            Op + "baseBeta",
            Op + "derivedGamma",
            Op + "derivedDelta");
    }

    [TestMethod]
    public void ToBodies_InheritedModel_CarriesTheValueOfEveryInheritedAndDeclaredMember_Test()
    {
        var request = new SoapOperationRequest("SaveOrdering", Op)
            .AddParameter("model", new EmitOrderingDerived
            {
                BaseAlpha = "a",
                BaseBeta = "b",
                DerivedGamma = "g",
                DerivedDelta = "d"
            });

        var result = MapperEmitAssert.Mapper.ToBodies(request);

        var root = MapperEmitAssert.SucceedsWithSingleBody(result);
        var model = MapperEmitAssert.Child(root, Op + "model");

        MapperEmitAssert.ChildValueIs(model, Op + "baseAlpha", "a");
        MapperEmitAssert.ChildValueIs(model, Op + "baseBeta", "b");
        MapperEmitAssert.ChildValueIs(model, Op + "derivedGamma", "g");
        MapperEmitAssert.ChildValueIs(model, Op + "derivedDelta", "d");

        MapperEmitAssert.EveryElementIsDefaultQualified(root);
    }
}
