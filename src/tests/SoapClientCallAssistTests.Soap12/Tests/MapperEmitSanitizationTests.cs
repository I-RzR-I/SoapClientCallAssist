using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Dto.Map;
using SoapClientCallAssistTests.Soap12.Helpers;
using SoapClientCallAssistTests.Soap12.Models;
using System;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests;

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
            Assert.IsFalse(
                text.Contains(EmitThrowingGetterModel.SecretValue, StringComparison.Ordinal),
                "A consumer value reached a result message. A failed result is routinely logged, so the " +
                $"value must never travel with it. Message text was: {text}");

            Assert.IsFalse(
                text.Contains(EmitThrowingGetterModel.BclFormatWording, StringComparison.OrdinalIgnoreCase),
                "The original exception text was forwarded. Any message built over a value can carry that " +
                $"value, so the text must be withheld. Message text was: {text}");
        }
    }

    [TestMethod]
    public void ToBodies_MemberAccessThrows_StillNamesTheMemberAndTheTypeSoTheFailureIsActionable_Test()
    {
        var request = BuildRequest();

        var result = MapperEmitAssert.Mapper.ToBodies(request);

        var messages = MapperEmitAssert.FailsWithCode(result, EmitErrorCode);
        var info = messages[0].Message?.Info ?? string.Empty;

        StringAssert.Contains(
            info,
            "explodingValue",
            "Withholding the value must not also withhold which member failed.");

        StringAssert.Contains(
            info,
            nameof(EmitThrowingGetterModel),
            "Withholding the value must not also withhold which type failed.");
    }

    [TestMethod]
    public void ToBodies_MemberAccessThrows_NamesTheUnderlyingExceptionTypeRatherThanTheReflectionWrapper_Test()
    {
        var request = BuildRequest();

        var result = MapperEmitAssert.Mapper.ToBodies(request);

        MapperEmitAssert.FailsWithCode(result, EmitErrorCode);

        var joined = string.Join(" | ", MapperEmitAssert.AllMessageText(result));

        StringAssert.Contains(
            joined,
            typeof(FormatException).FullName!,
            "The failure should name the cause. Reporting only the reflection wrapper " +
            $"({nameof(System.Reflection.TargetInvocationException)}) tells a caller nothing. " +
            $"All message text was: {joined}");
    }
}
