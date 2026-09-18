#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Client;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Security.WsSecurity;
using SoapClientCallAssistTests.Soap12.Helpers.Certificates;
using SoapClientCallAssistTests.Soap12.Helpers.Protocol;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.Hygiene;

[TestClass]
public sealed class WsSecurityStatelessnessTests
{

    private const int InterleavedBuilds = 50;

    private const string SecurityNamespace = "SoapClientCallAssist.Security";

    private const BindingFlags Declared = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    private static readonly string[] AllowedInstanceFieldNames = { "_clientFactory", "_responseVerifier", "_clientTimeoutTicks", "_responseSecurity" };

    private static readonly Type[] ClientTypes = { typeof(Soap11Client), typeof(Soap12Client), typeof(BaseEndpointClient) };

    private static readonly Type[] RequestPathTypes =
    {
        typeof(Soap11Client),
        typeof(Soap12Client),
        typeof(BaseEndpointClient),
        typeof(WsSecurityMessageSigner),
        typeof(WsSecurityMessageVerifier),
        typeof(WsSecurityResponseSecurity)
    };

    private static readonly string[] ForbiddenInstanceFieldTypeNames =
    {
        "SoapClientCallAssist.Security.SymmetricKeySource",
        "SoapClientCallAssist.Security.RequestKeyMaterial",
        "SoapClientCallAssist.Security.ConsumedKeyMaterial"
    };

    private static readonly Type[] ForbiddenInstanceFieldTypes = { typeof(byte[]), typeof(X509Certificate2), typeof(RSA), typeof(HMAC), typeof(SoapSecureConversationSession) };

    private static readonly Type[] PerBuildForbiddenFieldTypes = { typeof(byte[]), typeof(X509Certificate2), typeof(SoapSecureConversationSession) };

    private static readonly Type[] ForbiddenGenericDefinitions =
    {
        typeof(IDictionary<,>), typeof(IReadOnlyDictionary<,>), typeof(Dictionary<,>), typeof(ConditionalWeakTable<,>), typeof(AsyncLocal<>), typeof(ThreadLocal<>), typeof(Lazy<>)
    };

    private static readonly Type[] ImmutableScalarTypes = { typeof(string), typeof(TimeSpan), typeof(DateTimeOffset), typeof(XNamespace), typeof(XName) };

    private static readonly Type[] AllowedStaticCollectionDefinitions =
    {
        typeof(HashSet<>), typeof(IReadOnlyList<>), typeof(IReadOnlyCollection<>), typeof(IReadOnlySet<>), typeof(ReadOnlyCollection<>)
    };

    [TestMethod]
    public async Task BuildRequest_FiftyInterleavedBuildsWithTwoCertificatesOnOneSingletonClient_KeepEveryWireBoundToItsOwnOptions_Test()
    {
        using var certificateA = CertificateScenarioSupport.CreateRsaCertificate("CN=Scca.Tests.Stateless.A", 2048);
        using var certificateB = CertificateScenarioSupport.CreateRsaCertificate("CN=Scca.Tests.Stateless.B", 2048);

        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var optionsA = Options(certificateA);
        var optionsB = Options(certificateB);

        var outcomes = await Task.WhenAll(
            Enumerable.Range(0, InterleavedBuilds).Select(index => Task.Run(() =>
            {
                var expected = index % 2 == 0 ? certificateA : certificateB;
                var options = index % 2 == 0 ? optionsA : optionsB;

                var built = client.BuildRequest(
                    System.Net.Http.HttpMethod.Post,
                    new BuildSoapRequestDto
                    {
                        Client = new HttpClientDto(WsSecurityTestSupport.Endpoint),
                        Envelope = new SoapEnvelopeDto(new[] { WsSecurityTestSupport.Body("p" + index) }, null, WsSecurityTestSupport.Action),
                        Security = options
                    });

                return (Index: index, Expected: expected, Wire: WsSecurityTestSupport.Wire(built, $"{index}"));
            })));

        var strayed = outcomes
            .Where(outcome => CertificateScenarioSupport.BinarySecurityToken(outcome.Wire) != Convert.ToBase64String(outcome.Expected.RawData))
            .Select(outcome => outcome.Index)
            .ToList();

        Assert.AreEqual(0, strayed.Count, string.Join(", ", strayed));

        var nonces = outcomes
            .Select(outcome => WsSecurityFoundationTestSupport.ChildText(
                WsSecurityFoundationTestSupport.UsernameTokenElement(WsSecurityTestSupport.ParseWire(outcome.Wire)), "Nonce"))
            .ToList();

        Assert.AreEqual(InterleavedBuilds, nonces.Distinct(StringComparer.Ordinal).Count());

        foreach (var outcome in outcomes)
        {
            WsSecurityAssert.Accepted(
                new WsSecurityMessageVerifier().Verify(outcome.Wire, outcome.Expected),
                $"{outcome.Index}");
        }
    }

    [TestMethod]
    public void EverySecurityTypeAndClient_DeclaresNoStaticFieldThatCouldRetainStateAcrossRequests_Test()
    {
        var swept = SweptTypes().ToList();

        Assert.IsTrue(swept.Count >= 30, $"{swept.Count} | {string.Join(", ", swept.Select(type => type.Name))}");

        var offending = swept.SelectMany(StaticOffenders).ToList();

        Assert.AreEqual(0, offending.Count, string.Join(", ", offending));
    }

    [TestMethod]
    public void RequestPathTypes_DeclareNoInstanceFieldThatCouldRetainPerRequestState_Test()
    {
        var offending = RequestPathTypes.SelectMany(type => InstanceOffenders(type, AllowedInstanceFieldNames)).ToList();

        Assert.AreEqual(0, offending.Count, string.Join(", ", offending));
    }

    [TestMethod]
    public void PerBuildTypes_DeclareNoRawSecretField_Test()
    {
        var builder = WsSecurityFoundationTestSupport.LibraryType("SoapClientCallAssist.Security.WsSecurityHeaderBuilder");

        var offending = builder.GetFields(Declared)
            .Where(field => !field.IsStatic)
            .Where(field => PerBuildForbiddenFieldTypes.Contains(field.FieldType) || IsForbiddenMap(field.FieldType) || ForbiddenInstanceFieldTypeNames.Skip(1).Contains(field.FieldType.FullName))
            .Select(Describe)
            .ToList();

        Assert.AreEqual(0, offending.Count, string.Join(", ", offending));
    }

    [TestMethod]
    public void Sweep_FlagsAPlantedStaticSecretAndAPlantedInstanceSecret_Test()
    {
        var planted = StaticOffenders(typeof(PlantedStaticSecret)).ToList();

        CollectionAssert.AreEquivalent(
            new[] { Describe(typeof(PlantedStaticSecret).GetField("Leak", Declared)), Describe(typeof(PlantedStaticSecret).GetField("Cache", Declared)) },
            planted);

        var instance = InstanceOffenders(typeof(PlantedInstanceSecret), AllowedInstanceFieldNames).ToList();

        CollectionAssert.AreEquivalent(
            new[]
            {
                Describe(typeof(PlantedInstanceSecret).GetField("_key", Declared)),
                Describe(typeof(PlantedInstanceSecret).GetField("_certificate", Declared)),
                Describe(typeof(PlantedInstanceSecret).GetField("_rsa", Declared)),
                Describe(typeof(PlantedInstanceSecret).GetField("_map", Declared)),
                Describe(typeof(PlantedInstanceSecret).GetField("_local", Declared))
            },
            instance);
    }

    private static IEnumerable<Type> SweptTypes()
        => WsSecurityFoundationTestSupport.LibraryAssembly.GetTypes()
            .Where(type => type.Namespace == SecurityNamespace || ClientTypes.Contains(type))
            .Where(type => !type.IsDefined(typeof(CompilerGeneratedAttribute), false));

    private static IEnumerable<string> StaticOffenders(Type type)
        => type.GetFields(Declared)
            .Where(field => field.IsStatic)
            .Where(field => !field.IsLiteral)
            .Where(field => !field.IsInitOnly || !IsImmutableStaticType(field.FieldType))
            .Select(Describe);

    private static IEnumerable<string> InstanceOffenders(Type type, IReadOnlyCollection<string> allowedNames)
        => type.GetFields(Declared)
            .Where(field => !field.IsStatic)
            .Where(field => !allowedNames.Contains(field.Name))
            .Where(field => IsForbiddenInstanceType(field.FieldType))
            .Select(Describe);

    private static bool IsForbiddenInstanceType(Type fieldType)
        => ForbiddenInstanceFieldTypes.Any(forbidden => forbidden.IsAssignableFrom(fieldType))
           || ForbiddenInstanceFieldTypeNames.Contains(fieldType.FullName)
           || IsForbiddenMap(fieldType);

    private static bool IsForbiddenMap(Type fieldType)
        => typeof(IDictionary).IsAssignableFrom(fieldType)
           || fieldType.IsGenericType && ForbiddenGenericDefinitions.Contains(fieldType.GetGenericTypeDefinition())
           || fieldType.GetInterfaces().Any(iface => iface.IsGenericType && ForbiddenGenericDefinitions.Contains(iface.GetGenericTypeDefinition()));

    private static bool IsImmutableStaticType(Type type)
    {
        if (IsImmutableElement(type))
            return true;

        if (type.IsArray)
            return IsImmutableElement(type.GetElementType());

        return type.IsGenericType
               && AllowedStaticCollectionDefinitions.Contains(type.GetGenericTypeDefinition())
               && type.GetGenericArguments().All(IsImmutableElement);
    }

    private static bool IsImmutableElement(Type type)
    {
        if (type.IsPrimitive || type.IsEnum || ImmutableScalarTypes.Contains(type))
            return true;

        return type.IsGenericType
               && type.FullName is not null
               && type.FullName.StartsWith("System.ValueTuple`", StringComparison.Ordinal)
               && type.GetGenericArguments().All(IsImmutableElement);
    }

    private static string Describe(FieldInfo field) => $"{field.DeclaringType?.Name}.{field.Name}: {field.FieldType.Name}";

    private static SoapSecurityDto Options(X509Certificate2 certificate)
        => new()
        {
            Enabled = true,
            SigningCertificate = certificate,
            UsernameToken = WsSecurityFoundationTestSupport.UsernameToken()
        };

    private sealed class PlantedStaticSecret
    {
        public const int Constant = 1;

        public static readonly string[] Table = { "a" };

        public static byte[] Leak = new byte[16];

        public static readonly Dictionary<string, byte[]> Cache = new();
    }

    private sealed class PlantedInstanceSecret
    {
        private readonly byte[] _key = new byte[16];

        private readonly X509Certificate2 _certificate = null;

        private readonly RSA _rsa = null;

        private readonly Dictionary<string, string> _map = new();

        private readonly AsyncLocal<string> _local = new();

        private readonly string _name = "plain";

        private readonly object _responseVerifier = null;

        public override string ToString() => $"{_key.Length}{_certificate}{_rsa}{_map.Count}{_local.Value}{_name}{_responseVerifier}";
    }
}
