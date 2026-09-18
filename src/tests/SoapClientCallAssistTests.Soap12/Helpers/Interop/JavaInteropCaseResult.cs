#nullable disable

using System.Collections.Generic;
using System.Linq;

namespace SoapClientCallAssistTests.Soap12.Helpers.Interop;

internal sealed class JavaInteropCaseResult
{

    internal JavaInteropCaseResult(string name) => Name = name;

    internal string Name { get; }

    internal int RegisteredWsuIdCount { get; set; }

    internal string CanonicalizationMethod { get; set; }

    internal string SignatureMethod { get; set; }

    internal JavaInteropVerdict CoreValidity { get; set; } = JavaInteropVerdict.Unreported;

    internal JavaInteropVerdict SignatureValueValidity { get; set; } = JavaInteropVerdict.Unreported;

    internal string HarnessError { get; set; }

    internal List<JavaInteropReferenceResult> References { get; } = new();

    internal string Mode { get; set; }

    internal string JavaRuntime { get; set; }

    internal string KeyWrapAlgorithm { get; set; }

    internal JavaInteropVerdict KeyUnwrap { get; set; } = JavaInteropVerdict.Unreported;

    internal List<JavaInteropDerivedKeyResult> DerivedKeys { get; } = new();

    internal List<JavaInteropDecryptionResult> Decryptions { get; } = new();

    internal string Username { get; set; }

    internal string UsernameTokenId { get; set; }

    internal JavaInteropVerdict UsernameToken { get; set; } = JavaInteropVerdict.Unreported;

    internal string PrimarySignature { get; set; }

    internal string HmacOutputLength { get; set; }

    internal string EndorsingSignatureMethod { get; set; }

    internal JavaInteropVerdict EndorsingCoreValidity { get; set; } = JavaInteropVerdict.Unreported;

    internal JavaInteropVerdict EndorsingSignatureValueValidity { get; set; } = JavaInteropVerdict.Unreported;

    internal string EndorsingReferenceUri { get; set; }

    internal JavaInteropVerdict EndorsingDigestValid { get; set; } = JavaInteropVerdict.Unreported;

    internal string EndorsingTarget { get; set; }

    internal bool IsSymmetric => Mode is not null;

    internal JavaInteropReferenceResult Reference(string uri)
        => References.FirstOrDefault(reference => reference.Uri == uri);

    internal JavaInteropDerivedKeyResult DerivedKey(string id)
        => DerivedKeys.FirstOrDefault(key => key.Id == id);

    public override string ToString()
        => $"case={Name} core={CoreValidity} " +
           $"signedInfo={SignatureValueValidity} " +
           $"c14n={JavaInteropVerdict.Or(CanonicalizationMethod)} " +
           $"signatureMethod={JavaInteropVerdict.Or(SignatureMethod)} wsuIds={RegisteredWsuIdCount} " +
           $"harnessError={JavaInteropVerdict.Or(HarnessError)} " +
           $"references=[{string.Join(" | ", References)}]" +
           (IsSymmetric ? SymmetricText() : string.Empty);

    private string SymmetricText()
        => $" mode={Mode} jdk={JavaInteropVerdict.Or(JavaRuntime)} keyWrap={JavaInteropVerdict.Or(KeyWrapAlgorithm)} " +
           $"keyUnwrap={KeyUnwrap} derivedKeys=[{string.Join(" | ", DerivedKeys)}] " +
           $"decryptions=[{string.Join(" | ", Decryptions)}] username={JavaInteropVerdict.Or(Username)} " +
           $"usernameToken={UsernameToken} primary={JavaInteropVerdict.Or(PrimarySignature)} " +
           $"hmacOutputLength={JavaInteropVerdict.Or(HmacOutputLength)} " +
           $"endorsing={EndorsingCoreValidity}/{EndorsingSignatureValueValidity} " +
           $"endorsingMethod={JavaInteropVerdict.Or(EndorsingSignatureMethod)} " +
           $"endorsingReference={JavaInteropVerdict.Or(EndorsingReferenceUri)} endorsingDigest={EndorsingDigestValid} " +
           $"endorsingTarget={JavaInteropVerdict.Or(EndorsingTarget)}";
}
