using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using RzR.ResultMessage.Abstractions.Models;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Helpers.Mapper;

internal static class MapperEmitAssert
{
    internal static ISoapModelMapper Mapper { get; } = new SoapModelMapper();

    internal static XElement SucceedsWithSingleBody(IResult<IEnumerable<XElement>> result)
    {
        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsSuccess, Describe(result));
        Assert.IsNotNull(result.Response);

        var bodies = result.Response.ToArray();

        Assert.AreEqual(1, bodies.Length, string.Join(", ", bodies.Select(x => x.Name.ToString())));

        Assert.IsNotNull(bodies[0]);

        return bodies[0];
    }

    internal static IReadOnlyList<IMessageModel> FailsWithCode(IResult result, string expectedCode)
    {
        Assert.IsNotNull(result);
        Assert.IsFalse(result.IsSuccess);
        Assert.IsNotNull(result.Messages);

        var messages = result.Messages.ToList();

        Assert.IsTrue(messages.Count > 0);

        Assert.AreEqual(expectedCode, messages[0].Key, Describe(result));

        return messages;
    }

    internal static void EveryElementIsDefaultQualified(XElement root)
    {
        Assert.IsNotNull(root);

        foreach (var element in root.DescendantsAndSelf())
        {
            Assert.AreNotEqual(string.Empty, element.Name.NamespaceName, $"{element.Name.LocalName}");

            var standalone = XElement.Parse(element.ToString());

            Assert.IsNotNull(standalone.Attribute("xmlns"), $"{element.Name} | {element}");

            foreach (var attribute in element.Attributes().Where(x => x.IsNamespaceDeclaration))
                Assert.AreEqual("xmlns", attribute.Name.LocalName, $"{element.Name} | {attribute.Name}");
        }
    }

    internal static void ChildSequenceIs(XElement parent, params XName[] expected)
    {
        Assert.IsNotNull(parent);

        var actual = parent.Elements().Select(x => x.Name.ToString()).ToArray();
        var wanted = expected.Select(x => x.ToString()).ToArray();

        Assert.AreEqual(string.Join(" | ", wanted), string.Join(" | ", actual), $"{parent.Name}");
    }

    internal static XElement Child(XElement parent, XName name)
    {
        Assert.IsNotNull(parent);

        var matches = parent.Elements(name).ToArray();

        Assert.AreEqual(1, matches.Length, $"{name} | {parent.Name} | {string.Join(", ", parent.Elements().Select(x => x.Name.ToString()))}");

        return matches[0];
    }

    internal static void ChildValueIs(XElement parent, XName name, string expected)
        => Assert.AreEqual(expected, Child(parent, name).Value, $"{name}");

    internal static void NoElementNamed(XElement root, XName name)
    {
        Assert.IsNotNull(root);

        Assert.IsFalse(root.DescendantsAndSelf().Any(x => x.Name == name), $"{name} | {Xml(root)}");
    }

    internal static void XmlDoesNotContain(XElement root, params string[] fragments)
    {
        var xml = Xml(root);

        foreach (var fragment in fragments)
        {
            Assert.IsFalse(xml.Contains(fragment, StringComparison.Ordinal), $"{fragment} | {xml}");
        }
    }

    internal static void XmlContains(XElement root, params string[] fragments)
    {
        var xml = Xml(root);

        foreach (var fragment in fragments)
        {
            Assert.IsTrue(xml.Contains(fragment, StringComparison.Ordinal), $"{fragment} | {xml}");
        }
    }

    internal static IReadOnlyList<string> AllMessageText(IResult result)
    {
        var text = new List<string>();

        if (result?.Messages is null)
            return text;

        foreach (var message in result.Messages)
        {
            text.Add(message.Key ?? string.Empty);
            text.Add($"{message}");
            text.Add(message.Message?.Info ?? string.Empty);

            var details = message.Message?.Details;
            if (details is null)
                continue;

            for (var index = 0; index < details.Count; index++)
                text.Add($"{details[index]}");
        }

        return text;
    }

    internal static string Describe(IResult result)
    {
        if (result?.Messages is null)
            return "<no messages>";

        var rendered = result.Messages
            .Select(message => $"[{message.Key ?? "<null key>"}] {message.Message?.Info ?? "<null info>"}")
            .ToArray();

        return rendered.Length == 0 ? "<no messages>" : string.Join(" | ", rendered);
    }

    internal static string Xml(XElement root) => root.ToString(SaveOptions.DisableFormatting);
}
