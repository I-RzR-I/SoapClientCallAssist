using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using RzR.ResultMessage.Abstractions.Models;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Helpers;

internal static class MapperEmitAssert
{
    internal static ISoapModelMapper Mapper { get; } = new SoapModelMapper();

    internal static XElement SucceedsWithSingleBody(IResult<IEnumerable<XElement>> result)
    {
        Assert.IsNotNull(result, "The mapper returned a null result object.");
        Assert.IsTrue(result.IsSuccess, $"The emit was expected to succeed but failed: {Describe(result)}");
        Assert.IsNotNull(result.Response, "The emit reported success but produced no body collection.");

        var bodies = result.Response.ToArray();

        Assert.AreEqual(
            1,
            bodies.Length,
            "A document/literal wrapped call emits exactly one operation element. " +
            $"Emitted: [{string.Join(", ", bodies.Select(x => x.Name.ToString()))}].");

        Assert.IsNotNull(bodies[0], "The emitted body collection holds a null element.");

        return bodies[0];
    }

    internal static IReadOnlyList<IMessageModel> FailsWithCode(IResult result, string expectedCode)
    {
        Assert.IsNotNull(result, "The mapper returned a null result object.");
        Assert.IsFalse(result.IsSuccess, "The emit was expected to fail but reported success.");
        Assert.IsNotNull(result.Messages, "The failure carries no message collection, so a caller has nothing to act on.");

        var messages = result.Messages.ToList();

        Assert.IsTrue(messages.Count > 0, "The result reported a failure but carried no messages.");

        Assert.AreEqual(
            expectedCode,
            messages[0].Key,
            $"Unexpected failure code on the first message. Full result was: {Describe(result)}");

        return messages;
    }

    internal static void EveryElementIsDefaultQualified(XElement root)
    {
        Assert.IsNotNull(root, "There is no emitted element to inspect.");

        foreach (var element in root.DescendantsAndSelf())
        {
            Assert.AreNotEqual(
                string.Empty,
                element.Name.NamespaceName,
                $"Element '{element.Name.LocalName}' sits in no namespace, so the body rebuild in " +
                "SoapXmlHelper would flatten the whole body to concatenated text.");

            var standalone = XElement.Parse(element.ToString());

            Assert.IsNotNull(
                standalone.Attribute("xmlns"),
                $"Element '{element.Name}' carries no default xmlns when serialized on its own. " +
                "SoapXmlHelper.CheckAndValidateSoapBodies tests exactly this expression and rebuilds " +
                "the body destructively when it is null. Serialized element was: {" + element + "}");

            foreach (var attribute in element.Attributes().Where(x => x.IsNamespaceDeclaration))
                Assert.AreEqual(
                    "xmlns",
                    attribute.Name.LocalName,
                    $"Element '{element.Name}' declares a prefixed namespace ('{attribute.Name}'), which " +
                    "the emitter refuses because the body rebuild only recognises a default declaration.");
        }
    }

    internal static void ChildSequenceIs(XElement parent, params XName[] expected)
    {
        Assert.IsNotNull(parent, "There is no parent element to inspect.");

        var actual = parent.Elements().Select(x => x.Name.ToString()).ToArray();
        var wanted = expected.Select(x => x.ToString()).ToArray();

        Assert.AreEqual(
            string.Join(" | ", wanted),
            string.Join(" | ", actual),
            $"Unexpected child element sequence under '{parent.Name}'.");
    }

    internal static XElement Child(XElement parent, XName name)
    {
        Assert.IsNotNull(parent, "There is no parent element to inspect.");

        var matches = parent.Elements(name).ToArray();

        Assert.AreEqual(
            1,
            matches.Length,
            $"Expected exactly one '{name}' under '{parent.Name}'. " +
            $"Children present: [{string.Join(", ", parent.Elements().Select(x => x.Name.ToString()))}].");

        return matches[0];
    }

    internal static void ChildValueIs(XElement parent, XName name, string expected)
        => Assert.AreEqual(expected, Child(parent, name).Value, $"Unexpected text for element '{name}'.");

    internal static void NoElementNamed(XElement root, XName name)
    {
        Assert.IsNotNull(root, "There is no emitted element to inspect.");

        Assert.IsFalse(
            root.DescendantsAndSelf().Any(x => x.Name == name),
            $"Element '{name}' was emitted but was expected to be omitted. Emitted XML was: {Xml(root)}");
    }

    internal static void XmlDoesNotContain(XElement root, params string[] fragments)
    {
        var xml = Xml(root);

        foreach (var fragment in fragments)
        {
            Assert.IsFalse(
                xml.Contains(fragment, StringComparison.Ordinal),
                $"The emitted XML contains '{fragment}', which must never reach the wire. XML was: {xml}");
        }
    }

    internal static void XmlContains(XElement root, params string[] fragments)
    {
        var xml = Xml(root);

        foreach (var fragment in fragments)
        {
            Assert.IsTrue(
                xml.Contains(fragment, StringComparison.Ordinal),
                $"The emitted XML does not contain '{fragment}'. XML was: {xml}");
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
