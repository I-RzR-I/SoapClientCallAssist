#nullable disable

using System.Security.Cryptography;

namespace SoapClientCallAssistTests.Soap12.Helpers.Security;

internal static class RandomData
{

    internal static byte[] Bytes(int length)
    {
        var bytes = new byte[length];
        RandomNumberGenerator.Fill(bytes);

        return bytes;
    }

    internal static byte[] Nonce() => Bytes(16);
}
