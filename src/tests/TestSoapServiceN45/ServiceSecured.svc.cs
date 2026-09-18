using System;
using System.Collections.Generic;
using System.IdentityModel.Claims;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Activation;
using TestSoapServiceN45.Dto;

namespace TestSoapServiceN45
{
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public sealed class ServiceSecured : IServiceSecured
    {
        public const string AnonymousAuthenticationType = "Anonymous";

        public const string X509AuthenticationType = "X509";

        public string Echo(string value)
        {
            return value;
        }

        public CallerIdentity WhoAmI()
        {
            var context = ServiceSecurityContext.Current;
            var identity = new CallerIdentity
            {
                AuthenticationType = AnonymousAuthenticationType,
                Name = string.Empty,
                CertificateThumbprint = string.Empty
            };

            if (context == null || context.IsAnonymous)
                return identity;

            var chosen = context.PrimaryIdentity;
            object listed;

            if (context.AuthorizationContext.Properties.TryGetValue("Identities", out listed))
            {
                var identities = listed as IList<IIdentity>;
                if (identities != null && identities.Count > 0)
                {
                    chosen = identities[0];

                    foreach (var candidate in identities)
                    {
                        if (!string.Equals(candidate.AuthenticationType, X509AuthenticationType, StringComparison.Ordinal))
                        {
                            chosen = candidate;
                            break;
                        }
                    }
                }
            }

            identity.AuthenticationType = chosen.AuthenticationType;
            identity.Name = chosen.Name;

            foreach (var claimSet in context.AuthorizationContext.ClaimSets)
            {
                var certificateClaims = claimSet as X509CertificateClaimSet;
                if (certificateClaims != null)
                {
                    identity.CertificateThumbprint = certificateClaims.X509Certificate.Thumbprint;
                    break;
                }
            }

            return identity;
        }
    }
}
