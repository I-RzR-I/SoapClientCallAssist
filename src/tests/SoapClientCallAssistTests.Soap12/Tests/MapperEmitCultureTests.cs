using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Dto.Map;
using SoapClientCallAssistTests.Soap12.Helpers;
using SoapClientCallAssistTests.Soap12.Models;
using System;
using System.Globalization;
using System.Threading;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests;

[TestClass]
public class MapperEmitCultureTests
{
    private static readonly XNamespace Op = MapperEmitNs.OperationNamespace;

    [TestMethod]
    public void ToBodies_UnderGermanCulture_EmitsInvariantTextForEveryScalar_Test()
    {
        var originalCulture = Thread.CurrentThread.CurrentCulture;
        var originalUiCulture = Thread.CurrentThread.CurrentUICulture;

        try
        {
            var german = new CultureInfo("de-DE");

            Thread.CurrentThread.CurrentCulture = german;
            Thread.CurrentThread.CurrentUICulture = german;

            Assert.AreEqual(
                "1234,56",
                1234.56m.ToString(CultureInfo.CurrentCulture),
                "The de-DE culture did not take effect, so this test would prove nothing.");

            var request = new SoapOperationRequest("SaveCulture", Op)
                .AddParameter("model", new EmitCultureModel
                {
                    Amount = 1234.56m,
                    Ratio = 1234.56d,
                    OccurredOn = new DateTime(2024, 3, 5, 6, 7, 8, DateTimeKind.Utc),
                    Elapsed = TimeSpan.FromMinutes(90)
                });

            var result = MapperEmitAssert.Mapper.ToBodies(request);

            var root = MapperEmitAssert.SucceedsWithSingleBody(result);
            var model = MapperEmitAssert.Child(root, Op + "model");

            MapperEmitAssert.ChildValueIs(model, Op + "amount", "1234.56");
            MapperEmitAssert.ChildValueIs(model, Op + "ratio", "1234.56");
            MapperEmitAssert.ChildValueIs(model, Op + "occurredOn", "2024-03-05T06:07:08Z");
            MapperEmitAssert.ChildValueIs(model, Op + "elapsed", "PT1H30M");
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = originalCulture;
            Thread.CurrentThread.CurrentUICulture = originalUiCulture;
        }
    }

    [TestMethod]
    public void ToBodies_UnderGermanCulture_NeverWritesADecimalComma_Test()
    {
        var originalCulture = Thread.CurrentThread.CurrentCulture;
        var originalUiCulture = Thread.CurrentThread.CurrentUICulture;

        try
        {
            var german = new CultureInfo("de-DE");

            Thread.CurrentThread.CurrentCulture = german;
            Thread.CurrentThread.CurrentUICulture = german;

            Assert.AreEqual(
                "1234,56",
                1234.56m.ToString(CultureInfo.CurrentCulture),
                "The de-DE culture did not take effect, so this test would prove nothing.");

            var request = new SoapOperationRequest("SaveCulture", Op)
                .AddParameter("model", new EmitCultureModel
                {
                    Amount = 1234.56m,
                    Ratio = 1234.56d,
                    OccurredOn = new DateTime(2024, 3, 5, 6, 7, 8, DateTimeKind.Utc),
                    Elapsed = TimeSpan.FromMinutes(90)
                });

            var result = MapperEmitAssert.Mapper.ToBodies(request);

            var root = MapperEmitAssert.SucceedsWithSingleBody(result);

            MapperEmitAssert.XmlDoesNotContain(root, "1234,56");
            MapperEmitAssert.EveryElementIsDefaultQualified(root);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = originalCulture;
            Thread.CurrentThread.CurrentUICulture = originalUiCulture;
        }
    }
}
