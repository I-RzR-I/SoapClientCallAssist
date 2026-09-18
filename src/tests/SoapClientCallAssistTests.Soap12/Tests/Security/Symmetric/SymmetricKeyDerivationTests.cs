#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using System;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.Symmetric;

[TestClass]
public sealed class SymmetricKeyDerivationTests
{

    private const string WcfSecretFebruary2005 = "Lt6f7rm2EHZBCi4ZAFWpQBRWWYe4iTkSNQ2sRX5sf6Y=";

    private const string WcfSecretDecember2005 = "rgR9419NUR0ZN3+Co88JhNc7pwvjEd9mZxpYXwqDhiQ=";

    private const string WcfWrappedKeyFebruary2005 =
        "qZLseQFGllgIC6cGyYfGxzOaxQf8OX9lD823DwAwdD/6vxtPhtYcI85wDBMA5WvIWl340LFT6aebnykZah1Xa3InLR4TlKGKEgHKNgJQg2gQp38IJjqstIN3W5bHQqRpoyFTenha/3fN2WAvwxUXd8hrdOKoMA3qyZvUfWbOOCsaJ513tMbxrHVsXSAl3dNv8yiIZkDSjza8q3OA/CoyuvOg9KOGfWT2rkcUjFj47ZY8mXuAZX5pkHLumFQlSzdgtNNyT555e7paqCwmoXqrETocfWN07Y4VAQDDj4ekLbGExi0MBzG4AFtlUPMobiM6HyiEYyB10JZCi+jxyP6GVw==";

    private const string WcfEncryptedKeySha1February2005 = "5PJ8wm0edp+BiwtEOIYqprryOho=";

    private const string WcfServiceCertificateDer =
        "MIIC6DCCAdCgAwIBAgIIaxBSzki/KCAwDQYJKoZIhvcNAQELBQAwNDEyMDAGA1UEAxMpU29hcENsaWVudENhbGxBc3Npc3QuV2NmLlN5bW1ldHJpY1NlcnZpY2UwHhcNMjYwOTExMTkwODM2WhcNMjYwOTEyMTkxMzM2WjA0MTIwMAYDVQQDEylTb2FwQ2xpZW50Q2FsbEFzc2lzdC5XY2YuU3ltbWV0cmljU2VydmljZTCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBAKp7UOVBrw16NjRo+b7eOXiiJgaxbCIbSJO3t0tIvCJ5uW85BodVtSmSlJNAJdpDwMSTJarXn6biu6jHg7no++XVwLPPz6yxzRc3daYxjVubIRNzCmdcfcGxAZJDBXF4O1VViQQptmoNJ4JGD5Cw6GfujV0cOLD4GJ1znFJ9HmU+lmdjUZKllgjOSQdV5KcIwBBK8wPLLTVCSNxxbnMXPDE2mPOhfolzq5UavkiE1I/cxN2Mt7eqVDDCJBKo8vUkfQJ5aVbUQQ4wrzVZgOO+Nculn+iBoBmvp3fMWpx47aO345bUGAnF35OY+A3lP8NH5BPbSBv6ThNGQzulckoHmZ0CAwEAATANBgkqhkiG9w0BAQsFAAOCAQEANL9KLQDmvEIdVBFHflqLf8Pm1Qy51rBBmnoJ88RABzBqMH7ZszqwHHxlKX+NKOLZ7Jb4qwjrHKqSMRBg03MPK1Zaog7ruODZVGugIbKXzIdC8LO6hIa+gbX7wmf3CYnha8BmaLUZdr5epOK2OWmEVAcWCsSL8++gWONJF95kapdx+aKVNJp0uJ5D+mutXs9gLCZKCpfyMhdJfJElKXDIZSR/5lNWDTsXzEovKduXr7NhHNCUd/1GAX1PuxVWMlIcGxDDZfKyWSWEhvUtlNIwczo64MhwWUKo8AwCKDC+L9D+7M7HN2uUJkqjEHNVW6M9qI5kOlbz7mR4TTKHaWOyEg==";

    private const string WcfThumbprintSha1KeyIdentifier = "q6bASEOnbQhQAiiyYBN2P5gRHTs=";

    private const string DsNamespace = "http://www.w3.org/2000/09/xmldsig#";

    private const string SignedInfoRequestFebruary2005 =
        "<SignedInfo xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><CanonicalizationMethod Algorithm=\"http://www.w3.org/2001/10/xml-exc-c14n#\"/><SignatureMethod Algorithm=\"http://www.w3.org/2001/04/xmldsig-more#hmac-sha256\"/><Reference URI=\"#_3\"><Transforms><Transform Algorithm=\"http://www.w3.org/2001/10/xml-exc-c14n#\"/></Transforms><DigestMethod Algorithm=\"http://www.w3.org/2001/04/xmlenc#sha256\"/><DigestValue>+irXVmOBIttf69ZIKWwhrNDsnkDQP4fqig11BNsfipQ=</DigestValue></Reference><Reference URI=\"#_4\"><Transforms><Transform Algorithm=\"http://www.w3.org/2001/10/xml-exc-c14n#\"/></Transforms><DigestMethod Algorithm=\"http://www.w3.org/2001/04/xmlenc#sha256\"/><DigestValue>XPdKg7memtBEAXfyR43QkuRKPpG4NEloigvbSmCgkPo=</DigestValue></Reference><Reference URI=\"#_5\"><Transforms><Transform Algorithm=\"http://www.w3.org/2001/10/xml-exc-c14n#\"/></Transforms><DigestMethod Algorithm=\"http://www.w3.org/2001/04/xmlenc#sha256\"/><DigestValue>Ju2gcT/jqKFmOBwWUvCzS51UIPuPKvPI56JILbxOpIU=</DigestValue></Reference><Reference URI=\"#_6\"><Transforms><Transform Algorithm=\"http://www.w3.org/2001/10/xml-exc-c14n#\"/></Transforms><DigestMethod Algorithm=\"http://www.w3.org/2001/04/xmlenc#sha256\"/><DigestValue>Zbc2ae+QgSHB9JHRbVpVumjD5+xjyUD69mjsjLWaSLQ=</DigestValue></Reference><Reference URI=\"#_7\"><Transforms><Transform Algorithm=\"http://www.w3.org/2001/10/xml-exc-c14n#\"/></Transforms><DigestMethod Algorithm=\"http://www.w3.org/2001/04/xmlenc#sha256\"/><DigestValue>HsSxdCkaBfRyUqM5C90Qf9kqsVbWLRlpRS2AFu67B7E=</DigestValue></Reference><Reference URI=\"#uuid-a0b6e901-b5a7-463e-826f-4470a38efd27-2\"><Transforms><Transform Algorithm=\"http://www.w3.org/2001/10/xml-exc-c14n#\"/></Transforms><DigestMethod Algorithm=\"http://www.w3.org/2001/04/xmlenc#sha256\"/><DigestValue>4Ce8cRon85Tjzn9yCwPbwWcWquXFwY3LCMY8D93tNnk=</DigestValue></Reference></SignedInfo>";

    private const string SignedInfoResponseFebruary2005 =
        "<SignedInfo xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><CanonicalizationMethod Algorithm=\"http://www.w3.org/2001/10/xml-exc-c14n#\"/><SignatureMethod Algorithm=\"http://www.w3.org/2001/04/xmldsig-more#hmac-sha256\"/><Reference URI=\"#_4\"><Transforms><Transform Algorithm=\"http://www.w3.org/2001/10/xml-exc-c14n#\"/></Transforms><DigestMethod Algorithm=\"http://www.w3.org/2001/04/xmlenc#sha256\"/><DigestValue>Lap5RBl224f/Pr8UMejU8bkjS6UJ1ANcRimds3O+yTU=</DigestValue></Reference><Reference URI=\"#_5\"><Transforms><Transform Algorithm=\"http://www.w3.org/2001/10/xml-exc-c14n#\"/></Transforms><DigestMethod Algorithm=\"http://www.w3.org/2001/04/xmlenc#sha256\"/><DigestValue>M3dFlihtw1wojEhKv3aaRudyqJOTy2Oqt3cTrjtrt2E=</DigestValue></Reference><Reference URI=\"#_6\"><Transforms><Transform Algorithm=\"http://www.w3.org/2001/10/xml-exc-c14n#\"/></Transforms><DigestMethod Algorithm=\"http://www.w3.org/2001/04/xmlenc#sha256\"/><DigestValue>BS/7yVp+JK6fUBeJ71I7jyiigy0739DhIoxvYVMenyw=</DigestValue></Reference><Reference URI=\"#uuid-a0b6e901-b5a7-463e-826f-4470a38efd27-3\"><Transforms><Transform Algorithm=\"http://www.w3.org/2001/10/xml-exc-c14n#\"/></Transforms><DigestMethod Algorithm=\"http://www.w3.org/2001/04/xmlenc#sha256\"/><DigestValue>x07K4G6uSSdmJ8KpU8t1BaC18iGI1BSsekepVKDJSIk=</DigestValue></Reference><Reference URI=\"#_1\"><Transforms><Transform Algorithm=\"http://www.w3.org/2001/10/xml-exc-c14n#\"/></Transforms><DigestMethod Algorithm=\"http://www.w3.org/2001/04/xmlenc#sha256\"/><DigestValue>zsBVAOQfyxQBIGCnA3B1dZyYkqy1gVZ+yj5Id+MMczg=</DigestValue></Reference><Reference URI=\"#_2\"><Transforms><Transform Algorithm=\"http://www.w3.org/2001/10/xml-exc-c14n#\"/></Transforms><DigestMethod Algorithm=\"http://www.w3.org/2001/04/xmlenc#sha256\"/><DigestValue>5Ph4LmyNA0gCMbDkHM2amgmfleQqVx//6vmnzchb6jo=</DigestValue></Reference></SignedInfo>";

    private const string SignedInfoResponseDecember2005 =
        "<SignedInfo xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><CanonicalizationMethod Algorithm=\"http://www.w3.org/2001/10/xml-exc-c14n#\"/><SignatureMethod Algorithm=\"http://www.w3.org/2001/04/xmldsig-more#hmac-sha256\"/><Reference URI=\"#_2\"><Transforms><Transform Algorithm=\"http://www.w3.org/2001/10/xml-exc-c14n#\"/></Transforms><DigestMethod Algorithm=\"http://www.w3.org/2001/04/xmlenc#sha256\"/><DigestValue>2Mr64lnPfKjp2CkefGNlPLOL3dQjKkO7+iSU4q16Wc0=</DigestValue></Reference><Reference URI=\"#_3\"><Transforms><Transform Algorithm=\"http://www.w3.org/2001/10/xml-exc-c14n#\"/></Transforms><DigestMethod Algorithm=\"http://www.w3.org/2001/04/xmlenc#sha256\"/><DigestValue>2kjXK+N25eC4AmzYxspxzo82HQfc+XuNvelSJG2/k6A=</DigestValue></Reference><Reference URI=\"#_4\"><Transforms><Transform Algorithm=\"http://www.w3.org/2001/10/xml-exc-c14n#\"/></Transforms><DigestMethod Algorithm=\"http://www.w3.org/2001/04/xmlenc#sha256\"/><DigestValue>/Do2Y4Zju0S3ILl2oOw1s/VUbUGuZOL/aPuLw4EBYCI=</DigestValue></Reference><Reference URI=\"#uuid-a0b6e901-b5a7-463e-826f-4470a38efd27-12\"><Transforms><Transform Algorithm=\"http://www.w3.org/2001/10/xml-exc-c14n#\"/></Transforms><DigestMethod Algorithm=\"http://www.w3.org/2001/04/xmlenc#sha256\"/><DigestValue>NAvgzN2E2zGRjl+hp5b8xQO2Ev+2Xk5nG63sDZ0iyNM=</DigestValue></Reference></SignedInfo>";

    private static readonly Type Derivation = WsSecurityFoundationTestSupport.LibraryType("SoapClientCallAssist.Security.WsSecurityKeyDerivation");

    [DataTestMethod]
    [DataRow(WcfSecretFebruary2005, "vaaw85DIeVVZzx+KeqkbvA==", 0, 24, "tYVI/uGxqoc/Llfb9b55HXKOM3frQYgs", SignedInfoRequestFebruary2005, "CHUBWrsYFK/+Tdz/8jj7kbfbGySSBDGGD+dh763hEWo=", DisplayName = "WCF wsHttpBinding request, SC Feb 2005, certificate credential, primary signature")]
    [DataRow(WcfSecretFebruary2005, "J6ifkS7rIl2lWJVCk9HIpw==", 0, 24, "TOYErXTYVcBUpIknlq39nYUBmaKhSRsR", SignedInfoResponseFebruary2005, "cQltR4MSRA44arm9jzvjEjAz3p4GWTKqUHikpRaxv1o=", DisplayName = "WCF wsHttpBinding response, SC Feb 2005, same secret, response nonce")]
    [DataRow(WcfSecretDecember2005, "INLDr9nvqtrhGW54AagmFQ==", 0, 24, "jHOx1jFcYx2fgLXlu+THf403ZJXKzq3w", SignedInfoResponseDecember2005, "kGBI4HW3gXKnC7XALotgMA5BaBQH+1aiO0NgpTmPMXw=", DisplayName = "WCF ws2007HttpBinding response, SC Dec 2005, username credential")]
    public void DeriveKey_KnownAnswersCapturedFromWcf_ReproduceTheKeyAndWcfsOwnSignatureValue_Test(
        string secretBase64, string nonceBase64, int offset, int length, string expectedKeyBase64, string signedInfoXml, string wcfSignatureValueBase64)
    {
        var secret = Convert.FromBase64String(secretBase64);
        var nonce = Convert.FromBase64String(nonceBase64);

        var key = DeriveKey(secret, SymmetricTestSupport.DefaultLabel, nonce, offset, length);

        Assert.AreEqual(expectedKeyBase64, Convert.ToBase64String(key));
        Assert.AreEqual(expectedKeyBase64, Convert.ToBase64String(SymmetricTestSupport.DeriveKey(secret, SymmetricTestSupport.DefaultLabel, nonce, offset, length)));

        var signedInfo = SymmetricTestSupport.NewDocument(signedInfoXml).DocumentElement;

        using var hmac = new HMACSHA256(key);

        Assert.AreEqual(wcfSignatureValueBase64, Convert.ToBase64String(hmac.ComputeHash(SymmetricTestSupport.ExclusiveC14N(signedInfo))));
    }

    [TestMethod]
    public void EncryptedKeySha1_OverTheWrappedKeyBytes_EqualsTheKeyIdentifierWcfEchoedInItsResponse_Test()
        => Assert.AreEqual(WcfEncryptedKeySha1February2005, Convert.ToBase64String(SymmetricTestSupport.Sha1(Convert.FromBase64String(WcfWrappedKeyFebruary2005))));

    [TestMethod]
    public void ThumbprintSha1_OverTheServiceCertificateDer_EqualsTheKeyIdentifierWcfEmitted_Test()
        => Assert.AreEqual(WcfThumbprintSha1KeyIdentifier, Convert.ToBase64String(SymmetricTestSupport.Sha1(Convert.FromBase64String(WcfServiceCertificateDer))));

    [TestMethod]
    public void PSha1_MatchesTheRfc2246ExpansionForSeveralOutputLengths_Test()
    {
        var secret = Encoding.ASCII.GetBytes("secret");
        var seed = Encoding.ASCII.GetBytes("seed");

        foreach (var length in new[] { 1, 19, 20, 21, 40, 41, 100 })
        {
            var expected = SymmetricTestSupport.PSha1(secret, seed, length);
            var actual = (byte[])Derivation.GetMethod("PSha1", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { secret, seed, length });

            CollectionAssert.AreEqual(expected, actual, $"{length}");
        }
    }

    [TestMethod]
    public void DeriveKey_WithAnOffset_TakesTheBytesAfterTheOffsetOfTheSameStream_Test()
    {
        var secret = Convert.FromBase64String(WcfSecretFebruary2005);
        var nonce = Convert.FromBase64String("vaaw85DIeVVZzx+KeqkbvA==");

        var stream = SymmetricTestSupport.DeriveKey(secret, SymmetricTestSupport.DefaultLabel, nonce, 0, 56);
        var offsetKey = DeriveKey(secret, SymmetricTestSupport.DefaultLabel, nonce, 24, 32);

        CollectionAssert.AreEqual(stream.Skip(24).Take(32).ToArray(), offsetKey);
    }

    [TestMethod]
    public void ComputeKey_IsPSha1OfTheRequestorEntropyKeyedOverTheIssuerEntropy_Test()
    {
        var requestor = Enumerable.Range(1, 32).Select(index => (byte)index).ToArray();
        var issuer = Enumerable.Range(100, 32).Select(index => (byte)index).ToArray();

        var computed = (byte[])Derivation.GetMethod("ComputeKey", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { requestor, issuer, 32 });

        CollectionAssert.AreEqual(SymmetricTestSupport.PSha1(requestor, issuer, 32), computed);
    }

    [DataTestMethod]
    [DataRow(false, true, 0, 24, DisplayName = "no secret")]
    [DataRow(true, false, 0, 24, DisplayName = "no nonce")]
    [DataRow(true, true, -1, 24, DisplayName = "negative offset")]
    [DataRow(true, true, 0, 0, DisplayName = "zero length")]
    [DataRow(true, true, 0, 257, DisplayName = "length over the cap")]
    [DataRow(true, true, 257, 24, DisplayName = "offset over the cap")]
    public void DeriveKey_WithInputsThatDescribeNoKey_ReturnsNullRatherThanThrowing_Test(bool secret, bool nonce, int offset, int length)
        => Assert.IsNull(DeriveKey(secret ? new byte[32] : null, SymmetricTestSupport.DefaultLabel, nonce ? new byte[16] : null, offset, length));

    private static byte[] DeriveKey(byte[] secret, string label, byte[] nonce, int offset, int length)
        => (byte[])Derivation.GetMethod("DeriveKey", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { secret, label, nonce, offset, length });
}
