using System;
using System.Collections.Generic;

namespace TestSoapServiceN45.Security
{
    public static class TestCredentials
    {
        public const string KnownUserName = "alice";

        public const string KnownPassword = "secret";

        public const string UnicodeUserName = "unicode-user";

        public const string UnicodePassword = "pässwörd€-日本";

        public const string TrustedClientThumbprint = "0690EB0DA269DBE6B24A98E1047B1E3A427D4DEE";

        private static readonly Dictionary<string, string> Users = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { KnownUserName, KnownPassword },
            { UnicodeUserName, UnicodePassword }
        };

        private static readonly HashSet<string> TrustedThumbprints = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            TrustedClientThumbprint
        };

        public static bool TryGetPassword(string userName, out string password)
        {
            password = null;

            return userName != null && Users.TryGetValue(userName, out password);
        }

        public static bool IsTrustedThumbprint(string thumbprint)
        {
            return thumbprint != null && TrustedThumbprints.Contains(thumbprint);
        }
    }
}
