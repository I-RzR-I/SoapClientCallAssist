#nullable disable

using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Soap12.Helpers.Security;

namespace SoapClientCallAssistTests.Soap12.Helpers.Wire;

public sealed class WsSecurityWireGoldenCase
{

    public string Name { get; init; }

    public SoapProtocolType Protocol { get; init; }

    public bool SignBody { get; init; } = true;

    public bool IncludeTimestamp { get; init; } = true;

    public bool SignTimestamp { get; init; } = true;

    public bool MustUnderstand { get; init; } = true;

    public string SecurityActor { get; init; }

    public SoapCanonicalizationType Canonicalization { get; init; } = SoapCanonicalizationType.ExclusiveC14N;

    public SoapSignatureAlgorithmType SignatureAlgorithm { get; init; } = SoapSignatureAlgorithmType.RsaSha256;

    public SoapDigestAlgorithmType DigestAlgorithm { get; init; } = SoapDigestAlgorithmType.Sha256;

    public bool ExtraHeader { get; init; }

    internal SoapSecurityDto Security()
        => WsSecurityTestSupport.Security(security =>
        {
            security.SignBody = SignBody;
            security.IncludeTimestamp = IncludeTimestamp;
            security.SignTimestamp = SignTimestamp;
            security.MustUnderstand = MustUnderstand;
            security.SecurityActor = SecurityActor;
            security.Canonicalization = Canonicalization;
            security.SignatureAlgorithm = SignatureAlgorithm;
            security.DigestAlgorithm = DigestAlgorithm;
            security.AdditionalSignedElementIds = ExtraHeader ? new[] { WsSecurityWireGoldenMatrix.ExtraHeaderId } : null;
        });

    public override string ToString() => Name;
}
