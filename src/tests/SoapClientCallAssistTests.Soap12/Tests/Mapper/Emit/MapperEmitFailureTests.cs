using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Dto.Map;
using SoapClientCallAssistTests.Soap12.Helpers.Mapper;
using SoapClientCallAssistTests.Soap12.Models;
using System.Linq;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests.Mapper.Emit;

[TestClass]
public class MapperEmitFailureTests
{
    private const string EmitErrorCode = "ER-MAP-EMT";

    private const string NoMappedMembersCode = "V-MAP-001";

    private const string UnsupportedMemberTypeCode = "V-MAP-002";

    private const string DuplicateWireNameCode = "V-MAP-003";

    private const string MissingNamespaceCode = "V-MAP-004";

    private static readonly XNamespace Op = MapperEmitNs.OperationNamespace;

    [TestMethod]
    public void ToBodies_TypeWithNoMappedMembers_FailsWithNoMappedMembers_Test()
    {
        var request = new SoapOperationRequest("SaveUnmapped", Op)
            .AddParameter("model", new EmitNoMappedMembers { Anything = "value" });

        var result = MapperEmitAssert.Mapper.ToBodies(request);

        var messages = MapperEmitAssert.FailsWithCode(result, NoMappedMembersCode);

        StringAssert.Contains(messages[0].Message?.Info ?? string.Empty, nameof(EmitNoMappedMembers));
    }

    [TestMethod]
    public void ToBodies_MemberOfAnUnsupportedType_FailsWithUnsupportedMemberType_Test()
    {
        var request = new SoapOperationRequest("SaveUnsupported", Op)
            .AddParameter("model", new EmitUnsupportedMemberType { Supported = "value" });

        var result = MapperEmitAssert.Mapper.ToBodies(request);

        var messages = MapperEmitAssert.FailsWithCode(result, UnsupportedMemberTypeCode);

        StringAssert.Contains(messages[0].Message?.Info ?? string.Empty, nameof(EmitUnsupportedMemberType.Unsupported));
    }

    [TestMethod]
    public void ToBodies_TwoMembersWithTheSameWireName_FailsWithDuplicateWireName_Test()
    {
        var request = new SoapOperationRequest("SaveDuplicate", Op)
            .AddParameter("model", new EmitDuplicateWireNames { First = "one", Second = "two" });

        var result = MapperEmitAssert.Mapper.ToBodies(request);

        var messages = MapperEmitAssert.FailsWithCode(result, DuplicateWireNameCode);

        StringAssert.Contains(messages[0].Message?.Info ?? string.Empty, "sameName");
    }

    [TestMethod]
    public void ToBodies_TwoParametersWithTheSameName_FailsWithDuplicateWireName_Test()
    {
        var request = new SoapOperationRequest("SaveDuplicateParameters", Op)
            .AddParameter("sameParameter", "one")
            .AddParameter("sameParameter", "two");

        var result = MapperEmitAssert.Mapper.ToBodies(request);

        var messages = MapperEmitAssert.FailsWithCode(result, DuplicateWireNameCode);

        StringAssert.Contains(messages[0].Message?.Info ?? string.Empty, "sameParameter");
    }

    [TestMethod]
    public void ToBodies_OperationNamespaceIsEmpty_FailsWithMissingNamespace_Test()
    {
        var request = new SoapOperationRequest("SaveUnqualified", XNamespace.Get(string.Empty))
            .AddParameter("model", new EmitFlatRecord { Code = "ABC-3", Quantity = 1, Active = true });

        var result = MapperEmitAssert.Mapper.ToBodies(request);

        var messages = MapperEmitAssert.FailsWithCode(result, MissingNamespaceCode);

        StringAssert.Contains(messages[0].Message?.Info ?? string.Empty, nameof(SoapOperationRequest.OperationNamespace));
    }

    [TestMethod]
    public void ToBodies_OperationNamespaceIsNull_FailsWithMissingNamespace_Test()
    {
        var request = new SoapOperationRequest("SaveUnqualified", null)
            .AddParameter("model", new EmitFlatRecord { Code = "ABC-4", Quantity = 1, Active = true });

        var result = MapperEmitAssert.Mapper.ToBodies(request);

        MapperEmitAssert.FailsWithCode(result, MissingNamespaceCode);
    }

    [TestMethod]
    public void ToBodies_NullRequest_FailsWithAnEmitErrorRatherThanThrowing_Test()
    {
        var result = MapperEmitAssert.Mapper.ToBodies(null);

        MapperEmitAssert.FailsWithCode(result, EmitErrorCode);
    }

    [TestMethod]
    public void ToBodies_MissingOperationName_FailsWithAnEmitError_Test()
    {
        var request = new SoapOperationRequest(string.Empty, Op)
            .AddParameter("model", new EmitFlatRecord { Code = "ABC-5", Quantity = 1, Active = true });

        var result = MapperEmitAssert.Mapper.ToBodies(request);

        var messages = MapperEmitAssert.FailsWithCode(result, EmitErrorCode);

        StringAssert.Contains(messages[0].Message?.Info ?? string.Empty, nameof(SoapOperationRequest.OperationName));
    }

    [TestMethod]
    public void ToBodies_ParameterWithNoName_FailsWithAnEmitError_Test()
    {
        var request = new SoapOperationRequest("SaveNamelessParameter", Op)
            .AddParameter(string.Empty, "value");

        var result = MapperEmitAssert.Mapper.ToBodies(request);

        MapperEmitAssert.FailsWithCode(result, EmitErrorCode);
    }

    [TestMethod]
    public void ToBodies_AnyFailure_ProducesNoPartialBody_Test()
    {
        var request = new SoapOperationRequest("SaveUnmapped", Op)
            .AddParameter("model", new EmitNoMappedMembers { Anything = "value" });

        var result = MapperEmitAssert.Mapper.ToBodies(request);

        MapperEmitAssert.FailsWithCode(result, NoMappedMembersCode);

        Assert.IsTrue(
            result.Response is null || !result.Response.Any(),
            string.Join(", ", (result.Response ?? Enumerable.Empty<XElement>()).Select(x => x.Name.ToString())));
    }
}
