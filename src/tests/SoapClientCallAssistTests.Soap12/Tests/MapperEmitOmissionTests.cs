using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Dto.Map;
using SoapClientCallAssistTests.Soap12.Helpers;
using SoapClientCallAssistTests.Soap12.Models;
using System.Linq;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests;

[TestClass]
public class MapperEmitOmissionTests
{
    private static readonly XNamespace Op = MapperEmitNs.OperationNamespace;

    private static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";

    private static SoapOperationRequest BuildRequest()
        => new SoapOperationRequest("SaveOmission", Op)
            .AddParameter("model", new EmitOmissionModel
            {
                PresentValue = "kept",
                NullText = null,
                NullNumber = null,
                NullComplex = null,
                EmptyCollection = new()
            })
            .AddParameter("omittedParameter", null);

    [TestMethod]
    public void ToBodies_MembersAndParameterAreNullOrEmpty_EmitsOnlyThePopulatedOnes_Test()
    {
        var request = BuildRequest();

        var result = MapperEmitAssert.Mapper.ToBodies(request);

        var root = MapperEmitAssert.SucceedsWithSingleBody(result);

        MapperEmitAssert.ChildSequenceIs(root, Op + "model");

        MapperEmitAssert.ChildSequenceIs(MapperEmitAssert.Child(root, Op + "model"), Op + "presentValue");
        MapperEmitAssert.ChildValueIs(MapperEmitAssert.Child(root, Op + "model"), Op + "presentValue", "kept");
    }

    [TestMethod]
    public void ToBodies_NullReferenceMember_IsOmitted_Test()
    {
        var request = BuildRequest();

        var result = MapperEmitAssert.Mapper.ToBodies(request);

        MapperEmitAssert.NoElementNamed(MapperEmitAssert.SucceedsWithSingleBody(result), Op + "nullText");
    }

    [TestMethod]
    public void ToBodies_NullNullableValueMember_IsOmitted_Test()
    {
        var request = BuildRequest();

        var result = MapperEmitAssert.Mapper.ToBodies(request);

        MapperEmitAssert.NoElementNamed(MapperEmitAssert.SucceedsWithSingleBody(result), Op + "nullNumber");
    }

    [TestMethod]
    public void ToBodies_NullComplexMember_IsOmittedAndItsContractIsNeverResolved_Test()
    {
        var request = BuildRequest();

        var result = MapperEmitAssert.Mapper.ToBodies(request);

        var root = MapperEmitAssert.SucceedsWithSingleBody(result);

        MapperEmitAssert.NoElementNamed(root, Op + "nullComplex");
        MapperEmitAssert.NoElementNamed(root, Op + "childValue");
    }

    [TestMethod]
    public void ToBodies_EmptyCollectionMember_IsOmittedRatherThanEmittedAsAnEmptyWrapper_Test()
    {
        var request = BuildRequest();

        var result = MapperEmitAssert.Mapper.ToBodies(request);

        MapperEmitAssert.NoElementNamed(MapperEmitAssert.SucceedsWithSingleBody(result), Op + "emptyCollection");
    }

    [TestMethod]
    public void ToBodies_NullParameter_IsOmitted_Test()
    {
        var request = BuildRequest();

        var result = MapperEmitAssert.Mapper.ToBodies(request);

        MapperEmitAssert.NoElementNamed(MapperEmitAssert.SucceedsWithSingleBody(result), Op + "omittedParameter");
    }

    [TestMethod]
    public void ToBodies_AnythingOmitted_WritesNoNilAttributeAnywhere_Test()
    {
        var request = BuildRequest();

        var result = MapperEmitAssert.Mapper.ToBodies(request);

        var root = MapperEmitAssert.SucceedsWithSingleBody(result);

        var nilAttributes = root
            .DescendantsAndSelf()
            .SelectMany(element => element.Attributes())
            .Where(attribute => attribute.Name == Xsi + "nil"
                                || attribute.Name.LocalName.Equals("nil", System.StringComparison.Ordinal))
            .Select(attribute => $"{attribute.Parent?.Name}/@{attribute.Name}")
            .ToArray();

        Assert.AreEqual(
            0,
            nilAttributes.Length,
            $"Version one omits an absent value; it must not emit xsi:nil. Found: [{string.Join(", ", nilAttributes)}].");

        MapperEmitAssert.XmlDoesNotContain(root, "nil", "XMLSchema-instance");
    }
}
