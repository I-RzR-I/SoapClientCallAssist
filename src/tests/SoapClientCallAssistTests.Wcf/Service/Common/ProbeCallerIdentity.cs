using System.IdentityModel.Claims;
using System.ServiceModel;

namespace SoapClientCallAssistTests.Wcf.Service.Common;

public static class ProbeCallerIdentity
{

    public static WcfCallerIdentity Current()
    {
        var context = ServiceSecurityContext.Current;

        if (context is null || context.IsAnonymous)
            return new WcfCallerIdentity();

        foreach (var claimSet in context.AuthorizationContext.ClaimSets)
            if (claimSet is X509CertificateClaimSet certificateClaims)
                return new WcfCallerIdentity
                {
                    AuthenticationType = WcfProbeContract.X509AuthenticationType,
                    Name = certificateClaims.X509Certificate.Thumbprint
                };

        return new WcfCallerIdentity
        {
            AuthenticationType = context.PrimaryIdentity.AuthenticationType,
            Name = context.PrimaryIdentity.Name
        };
    }
}
