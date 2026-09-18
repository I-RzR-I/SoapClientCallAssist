#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SoapClientCallAssistTests.Soap12.Helpers.Interop;

internal static class JavaInteropAssert
{

    internal static void AssertValid(JavaInteropVerdict verdict, string what, string because)
    {
        AssertNotAnError(verdict, what, because);

        Assert.IsTrue(verdict.IsValid, $"{because} | {what} | {verdict}");
    }

    internal static void AssertInvalid(JavaInteropVerdict verdict, string what, string because)
    {
        AssertNotAnError(verdict, what, because);

        Assert.IsTrue(verdict.IsInvalid, $"{because} | {what} | {verdict}");
    }

    internal static void AssertError(JavaInteropVerdict verdict, string what, string because)
    {
        Assert.IsFalse(verdict.IsUnreported, $"{because} | {what}");

        Assert.IsTrue(verdict.IsError, $"{because} | {what} | {verdict}");
    }

    internal static void AssertNotAnError(JavaInteropVerdict verdict, string what, string because)
    {
        Assert.IsFalse(verdict.IsError, $"{what} | {JavaInteropVerdict.Or(verdict.ErrorDetail)} | {because}");
    }
}
