#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Helpers.Security;

internal static class KeyMaterialReflection
{

    private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    internal static readonly Type BuilderType = WsSecurityFoundationTestSupport.LibraryType("SoapClientCallAssist.Security.WsSecurity.WsSecurityHeaderBuilder");

    internal static readonly Type PlannerType = WsSecurityFoundationTestSupport.LibraryType("SoapClientCallAssist.Security.SoapSecurityPlanner");

    internal static readonly Type BuildResultType = WsSecurityFoundationTestSupport.LibraryType("SoapClientCallAssist.Security.WsSecurity.WsSecurityHeaderBuildResult");

    internal static readonly Type KeySourceType = WsSecurityFoundationTestSupport.LibraryType("SoapClientCallAssist.Security.SymmetricKeySource");

    internal static readonly Type RequestMaterialType = WsSecurityFoundationTestSupport.LibraryType("SoapClientCallAssist.Security.RequestKeyMaterial");

    internal static readonly Type ConsumedMaterialType = WsSecurityFoundationTestSupport.LibraryType("SoapClientCallAssist.Security.ConsumedKeyMaterial");

    internal static object Plan(SoapSecurityDto security, string action = WsSecurityTestSupport.Action, Uri endpoint = null)
    {
        var plan = PlannerType.GetMethod("Plan", Any).Invoke(null, new object[] { security, action, endpoint ?? WsSecurityTestSupport.Endpoint });

        Assert.IsTrue(((IResult)plan).IsSuccess, NegativeTestSupport.Describe((IResult)plan));

        return Response(plan);
    }

    internal static XmlDocument Envelope(params XElement[] bodies)
    {
        var soap = XNamespace.Get("http://www.w3.org/2003/05/soap-envelope");
        var envelope = new XElement(
            soap + "Envelope",
            new XAttribute(XNamespace.Xmlns + "soap", soap.NamespaceName),
            new XElement(soap + "Header"),
            new XElement(soap + "Body", bodies.Length > 0 ? bodies : new[] { WsSecurityTestSupport.DefaultBody() }));

        var document = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
        document.LoadXml(envelope.ToString(SaveOptions.DisableFormatting));

        return document;
    }

    internal static IDisposable Builder(XmlDocument document, object plan, RSA privateKey)
        => (IDisposable)Activator.CreateInstance(BuilderType, Any, null, new[] { document, plan, privateKey }, null);

    internal static IResult Build(IDisposable builder)
        => (IResult)BuilderType.GetMethod("Build", Any).Invoke(builder, Array.Empty<object>());

    internal static object Response(object result)
        => result.GetType().GetProperty("Response", Any).GetValue(result);

    internal static object Field(object instance, string name)
    {
        var field = instance.GetType().GetField(name, Any);

        Assert.IsNotNull(field, $"{instance.GetType().Name} | {name}");

        return field.GetValue(instance);
    }

    internal static object KeySourceOf(IDisposable builder) => Field(builder, "_symmetricKeySource");

    internal static byte[] SecretOf(object keySource) => (byte[])Field(keySource, "_secret");

    internal static object MaterialOf(HttpRequestMessage request)
    {
        var material = WsSecurityFoundationTestSupport.KeyMaterialOf(request);

        Assert.IsNotNull(material);

        return material;
    }

    internal static IDisposable Consume(object material)
    {
        var arguments = new object[] { null };
        var consumed = (bool)RequestMaterialType.GetMethod("TryConsume", Any).Invoke(material, arguments);

        Assert.IsTrue(consumed);

        return (IDisposable)arguments[0];
    }

    internal static bool TryConsume(object material)
        => (bool)RequestMaterialType.GetMethod("TryConsume", Any).Invoke(material, new object[] { null });

    internal static IReadOnlyList<(string Name, byte[] Bytes)> SecretArraysOf(object instance)
    {
        var arrays = new List<(string, byte[])>();

        foreach (var field in instance.GetType().GetFields(Any).Where(field => !field.IsStatic))
            Collect(arrays, field.Name, field.GetValue(instance));

        return arrays;
    }

    internal static object Reconstruct(object material, IReadOnlyDictionary<string, object> overrides)
    {
        var constructor = RequestMaterialType.GetConstructors(Any).Single();

        var arguments = constructor.GetParameters()
            .Select(parameter => overrides.TryGetValue(parameter.Name, out var value) ? value : Clone(Field(material, "_" + parameter.Name)))
            .ToArray();

        return constructor.Invoke(arguments);
    }

    private static object Clone(object value)
        => value switch
        {
            byte[] bytes => (byte[])bytes.Clone(),
            byte[][] keys => keys.Select(key => (byte[])key.Clone()).ToArray(),
            _ => value
        };

    internal static HttpRequestMessage Plant(object material)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, WsSecurityTestSupport.Endpoint);
        request.Options.Set(new HttpRequestOptionsKey<object>(SoapClientCallAssist.SoapClientEndpointExtensions.RequestSecurityStateKey), material);

        return request;
    }

    internal static void AssertAllZero(IReadOnlyList<(string Name, byte[] Bytes)> arrays, string what)
    {
        Assert.IsTrue(arrays.Count > 0, what);

        var live = arrays.Where(entry => entry.Bytes.Any(value => value != 0)).Select(entry => entry.Name).ToList();

        Assert.AreEqual(0, live.Count, $"{what} | {string.Join(", ", live)}");
    }

    private static void Collect(List<(string, byte[])> arrays, string name, object value)
    {
        switch (value)
        {
            case byte[] bytes:
                arrays.Add((name, bytes));

                break;

            case byte[][] keys:
                for (var index = 0; index < keys.Length; index++)
                    arrays.Add(($"{name}[{index}]", keys[index]));

                break;
        }
    }
}
