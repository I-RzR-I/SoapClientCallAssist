using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssistTests.Common;
using System;
using System.Collections.Generic;

namespace SoapClientCallAssistTests.Wcf.Helpers;

internal static class ResultLeakSweep
{

    internal static void AssertNoSecret(IResult result, IEnumerable<string> secrets, string what)
    {
        var rendered = Render(result);

        foreach (var secret in secrets)
            Assert.IsFalse(
                rendered.IndexOf(secret, StringComparison.OrdinalIgnoreCase) >= 0,
                $"{what} | {rendered}");
    }

    internal static string Render(IResult result) => ResultRendering.Flat(result);
}
