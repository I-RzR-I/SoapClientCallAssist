using System;

namespace SoapClientCallAssistTests.Common;

public static class EnvironmentFlag
{
    public const string AllowEnvironmentSkipVariable = "SOAPCLIENTCALLASSIST_ALLOW_ENVIRONMENT_SKIP";

    private static readonly string[] AffirmativeValues = { "1", "true", "yes" };

    public static bool IsAffirmative(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();

        return Array.Exists(AffirmativeValues, affirmative => string.Equals(affirmative, trimmed, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsSet(string variableName)
        => IsAffirmative(Environment.GetEnvironmentVariable(variableName));

    public static bool AllowsEnvironmentSkip()
        => IsSet(AllowEnvironmentSkipVariable);
}