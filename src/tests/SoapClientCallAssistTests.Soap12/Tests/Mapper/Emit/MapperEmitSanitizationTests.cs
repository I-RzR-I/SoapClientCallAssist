using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Dto.Map;
using SoapClientCallAssistTests.Soap12.Helpers.Mapper;
using SoapClientCallAssistTests.Soap12.Models;
using System;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests.Mapper.Emit;

[TestClass]
public class MapperEmitSanitizationTests
{
    private const string EmitErrorCode = "ER-MAP-EMT";

    private static readonly XNamespace Op = MapperEmitNs.OperationNamespace;

    private static SoapOperationRequest BuildRequest()
        => new SoapOperationRequest("SaveThrowingMember", Op)
            .AddParameter("model", new EmitThrowingGetterModel());

    [TestMethod]
    public void ToBodies_MemberAccessThrows_FailsWithAnEmitErrorInsteadOfPropagatingTheException_Test()
    {
        var request = BuildRequest();

        var result = MapperEmitAssert.Mapper.ToBodies(request);

        MapperEmitAssert.FailsWithCode(result, EmitErrorCode);
    }

    [TestMethod]
    public void ToBodies_MemberAccessThrows_LeaksNeitherTheOffendingValueNorTheBclText_Test()
    {
        var request = BuildRequest();

        var result = MapperEmitAssert.Mapper.ToBodies(request);

        MapperEmitAssert.FailsWithCode(result, EmitErrorCode);

        foreach (var text in MapperEmitAssert.AllMessageText(result))
        {
            Assert.IsFalse(text.Contains(EmitThrowingGetterModel.SecretValue, StringComparison.Ordinal), $"{text}");

            Assert.IsFalse(text.Contains(EmitThrowingGetterModel.BclFormatWording, StringComparison.OrdinalIgnoreCase), $"{text}");
        }
    }

    [TestMethod]
    public void ToBodies_MemberAccessThrows_StillNamesTheMemberAndTheTypeSoTheFailureIsActionable_Test()
    {
        var request = BuildRequest();

        var result = MapperEmitAssert.Mapper.ToBodies(request);

        var messages = MapperEmitAssert.FailsWithCode(result, EmitErrorCode);
        var info = messages[0].Message?.Info ?? string.Empty;

        StringAssert.Contains(info, "explodingValue");

        StringAssert.Contains(info, nameof(EmitThrowingGetterModel));
    }

    [TestMethod]
    public void ToBodies_MemberAccessThrows_NamesTheUnderlyingExceptionTypeRatherThanTheReflectionWrapper_Test()
    {
        var request = BuildRequest();

        var result = MapperEmitAssert.Mapper.ToBodies(request);

        MapperEmitAssert.FailsWithCode(result, EmitErrorCode);

        var joined = string.Join(" | ", MapperEmitAssert.AllMessageText(result));

        StringAssert.Contains(joined, typeof(FormatException).FullName!, $"{nameof(System.Reflection.TargetInvocationException)}");
    }
}
