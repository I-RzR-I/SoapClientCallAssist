using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace TestSoapServiceN45.Security
{
    public static class NonceReplayCache
    {
        private static readonly ConcurrentDictionary<string, DateTime> Seen =
            new ConcurrentDictionary<string, DateTime>(StringComparer.Ordinal);

        public static bool TryRegister(string nonce, DateTime now, TimeSpan retention)
        {
            Prune(now);

            return Seen.TryAdd(nonce, now + retention);
        }

        private static void Prune(DateTime now)
        {
            var expired = new List<string>();

            foreach (var entry in Seen)
                if (entry.Value <= now)
                    expired.Add(entry.Key);

            DateTime ignored;
            foreach (var key in expired)
                Seen.TryRemove(key, out ignored);
        }
    }
}
