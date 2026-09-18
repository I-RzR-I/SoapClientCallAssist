namespace SoapTestService;

public static class SoapPrefixSwitch
{
    public const string QueryKey = "prefix";

    public const string DefaultPrefix = "soap";

    private static readonly string[] AllowedPrefixes = ["soap", "s", "env"];

    public static string Resolve(string? requested)
        => Array.Find(AllowedPrefixes, allowed => string.Equals(allowed, requested?.Trim(), StringComparison.OrdinalIgnoreCase))
           ?? DefaultPrefix;
}
