using SoapClientCallAssistTests.Wcf.Service.Common;
using System.Security.Claims;
using System.ServiceModel;

namespace SoapClientCallAssistTests.Wcf.Service.Saml;

[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
public sealed class SamlProbeService : ISamlProbeService
{

    public SamlCallerClaims WhoAmI()
    {
        var principal = OperationContext.Current?.ClaimsPrincipal;
        var identity = principal?.Identity as ClaimsIdentity;

        if (identity is null || !identity.IsAuthenticated)
            return new SamlCallerClaims { AuthenticationType = WcfProbeContract.AnonymousAuthenticationType };

        return new SamlCallerClaims
        {
            AuthenticationType = identity.AuthenticationType ?? string.Empty,
            Name = identity.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty,
            Role = identity.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty,
            Issuer = identity.FindFirst(ClaimTypes.Name)?.Issuer ?? string.Empty
        };
    }
}
