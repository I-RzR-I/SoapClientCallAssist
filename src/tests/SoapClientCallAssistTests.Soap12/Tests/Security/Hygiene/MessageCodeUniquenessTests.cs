#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.Hygiene;

[TestClass]
public sealed class MessageCodeUniquenessTests
{

    private static readonly Type CodesType = WsSecurityFoundationTestSupport.LibraryType("SoapClientCallAssist.Enums.MessageCodesType");

    [TestMethod]
    public void MessageCodesType_EveryMemberCarriesADescriptionThatIsUniqueAcrossTheEnum_Test()
    {
        var descriptions = Members().Select(member => (member.Name, Code: Description(member))).ToList();

        var missing = descriptions.Where(entry => string.IsNullOrWhiteSpace(entry.Code)).Select(entry => entry.Name).ToList();

        Assert.AreEqual(0, missing.Count, string.Join(", ", missing));

        var duplicated = descriptions
            .GroupBy(entry => entry.Code, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key} <- {string.Join(", ", group.Select(entry => entry.Name))}")
            .ToList();

        Assert.AreEqual(0, duplicated.Count, string.Join("; ", duplicated));
    }

    [TestMethod]
    public void MessageCodesType_EveryMemberHasExactlyOneMessageAcrossTheTwoDictionaries_Test()
    {
        var helper = WsSecurityFoundationTestSupport.LibraryType("SoapClientCallAssist.Helpers.DefaultResultMessageHelper");

        var errors = Keys(helper, "ErrorMessages");
        var validations = Keys(helper, "ValidationMessages");

        var unmapped = Members().Select(member => member.GetValue(null))
            .Where(code => !errors.Contains(code) && !validations.Contains(code))
            .ToList();

        Assert.AreEqual(0, unmapped.Count, string.Join(", ", unmapped));

        var twice = errors.Intersect(validations).ToList();

        Assert.AreEqual(0, twice.Count, string.Join(", ", twice));
    }

    [TestMethod]
    public void MessageCodesType_ReservedWsSecurityRanges_OnlyCarryTheCodesOfTheirConcern_Test()
    {
        var numbers = Members()
            .Select(Description)
            .Where(code => code.StartsWith("V-SEC-0", StringComparison.Ordinal))
            .Select(code => int.Parse(code.Substring("V-SEC-".Length)))
            .ToList();

        var ranges = new (int From, int To)[] { (20, 29), (30, 49), (50, 59), (60, 79), (80, 89), (90, 99) };

        foreach (var number in numbers.Where(number => number >= 20))
        {
            Assert.IsTrue(ranges.Any(range => number >= range.From && number <= range.To), $"{number:000}");
        }

        foreach (var opener in new[] { 20, 30, 50, 60, 80, 90 })
            Assert.IsTrue(numbers.Contains(opener), $"{opener:000} | {string.Join(", ", numbers)}");
    }

    private static IEnumerable<FieldInfo> Members()
        => CodesType.GetFields(BindingFlags.Public | BindingFlags.Static);

    private static string Description(FieldInfo member)
        => member.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()?.Description;

    private static HashSet<object> Keys(Type helper, string fieldName)
    {
        var field = helper.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

        Assert.IsNotNull(field, fieldName);

        return new HashSet<object>(((IDictionary)field.GetValue(null)).Keys.Cast<object>());
    }
}
