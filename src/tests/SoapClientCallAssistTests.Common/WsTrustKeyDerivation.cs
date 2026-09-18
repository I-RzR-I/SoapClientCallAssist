using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SoapClientCallAssistTests.Common;

public static class WsTrustKeyDerivation
{
    public static byte[] PSha1(byte[] secret, byte[] seed, int length)
    {
        using var hmac = new HMACSHA1(secret);

        var output = new List<byte>();
        var a = seed;

        while (output.Count < length)
        {
            a = hmac.ComputeHash(a);
            output.AddRange(hmac.ComputeHash(a.Concat(seed).ToArray()));
        }

        return output.Take(length).ToArray();
    }

    public static byte[] DeriveKey(byte[] secret, string label, byte[] nonce, int offset, int length)
        => PSha1(secret, Encoding.UTF8.GetBytes(label).Concat(nonce).ToArray(), offset + length).Skip(offset).Take(length).ToArray();
}