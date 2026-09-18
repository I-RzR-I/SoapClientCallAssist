#nullable disable

namespace SoapClientCallAssistTests.Soap12.Helpers.Interop;

internal sealed class SymmetricJavaInteropExpectation
{

    internal SymmetricJavaInteropExpectation(
        string name,
        string bodyReferenceUri,
        string signatureTokenId,
        string encryptionTokenId,
        string librarySignatureKeyBase64,
        string libraryEncryptionKeyBase64,
        string recomputedSignatureKeyBase64,
        string originalSignatureKeyBase64,
        string primarySignatureId,
        string usernameTokenReferenceUri,
        int encryptedDataCount)
    {
        Name = name;
        BodyReferenceUri = bodyReferenceUri;
        SignatureTokenId = signatureTokenId;
        EncryptionTokenId = encryptionTokenId;
        LibrarySignatureKeyBase64 = librarySignatureKeyBase64;
        LibraryEncryptionKeyBase64 = libraryEncryptionKeyBase64;
        RecomputedSignatureKeyBase64 = recomputedSignatureKeyBase64;
        OriginalSignatureKeyBase64 = originalSignatureKeyBase64;
        PrimarySignatureId = primarySignatureId;
        UsernameTokenReferenceUri = usernameTokenReferenceUri;
        EncryptedDataCount = encryptedDataCount;
    }

    internal string Name { get; }

    internal string BodyReferenceUri { get; }

    internal string SignatureTokenId { get; }

    internal string EncryptionTokenId { get; }

    internal string LibrarySignatureKeyBase64 { get; }

    internal string LibraryEncryptionKeyBase64 { get; }

    internal string RecomputedSignatureKeyBase64 { get; }

    internal string OriginalSignatureKeyBase64 { get; }

    internal string PrimarySignatureId { get; }

    internal string UsernameTokenReferenceUri { get; }

    internal int EncryptedDataCount { get; }

    public override string ToString()
        => $"name={Name} body={BodyReferenceUri} signatureToken={SignatureTokenId} " +
           $"encryptionToken={JavaInteropVerdict.Or(EncryptionTokenId)} librarySignatureKey={LibrarySignatureKeyBase64} " +
           $"libraryEncryptionKey={JavaInteropVerdict.Or(LibraryEncryptionKeyBase64)} " +
           $"recomputedSignatureKey={RecomputedSignatureKeyBase64} originalSignatureKey={OriginalSignatureKeyBase64} " +
           $"primarySignatureId={JavaInteropVerdict.Or(PrimarySignatureId)} " +
           $"usernameTokenReference={JavaInteropVerdict.Or(UsernameTokenReferenceUri)} encryptedData={EncryptedDataCount}";
}
