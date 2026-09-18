#nullable disable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SoapClientCallAssistTests.Soap12.Helpers.Wire;

internal static class WsSecurityWireNormaliser
{

    internal const string CreatedToken = "{created}";

    internal const string ExpiresToken = "{expires}";

    internal const string DigestToken = "{digest}";

    internal const string SignatureToken = "{sig}";

    internal const string BinarySecurityTokenToken = "{bst}";

    private const string OptionalPrefix = @"(?:[\w.-]+:)?";

    private static readonly Regex IdValue = new(
        @"(?<=\s" + OptionalPrefix + @"Id="")[^""]*(?="")|(?<=\sURI=""#)[^""]*(?="")",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly (Regex Element, string Token)[] TextReplacements =
    {
        (TextOf("Created"), CreatedToken),
        (TextOf("Expires"), ExpiresToken),
        (TextOf("DigestValue"), DigestToken),
        (TextOf("SignatureValue"), SignatureToken),
        (TextOf("BinarySecurityToken"), BinarySecurityTokenToken)
    };

    internal static string Normalise(string wire)
    {
        if (wire is null)
            return null;

        var tokens = new Dictionary<string, string>(StringComparer.Ordinal);

        var normalised = IdValue.Replace(wire, match => TokenFor(match.Value, tokens));

        foreach (var (element, token) in TextReplacements)
            normalised = element.Replace(normalised, "${open}" + token + "${close}");

        return normalised;
    }

    private static string IdToken(int ordinal) => "{id:" + ordinal.ToString(CultureInfo.InvariantCulture) + "}";

    private static string TokenFor(string id, Dictionary<string, string> tokens)
    {
        if (tokens.TryGetValue(id, out var token))
            return token;

        token = IdToken(tokens.Count + 1);
        tokens.Add(id, token);

        return token;
    }

    private static Regex TextOf(string localName)
        => new(
            @"(?<open><" + OptionalPrefix + localName + @"(?:\s[^>]*)?>)[^<]*(?<close></" + OptionalPrefix + localName + ">)",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);
}
