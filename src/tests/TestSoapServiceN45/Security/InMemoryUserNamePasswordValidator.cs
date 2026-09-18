using System;
using System.IdentityModel.Selectors;
using System.IdentityModel.Tokens;

namespace TestSoapServiceN45.Security
{
    public sealed class InMemoryUserNamePasswordValidator : UserNamePasswordValidator
    {
        public override void Validate(string userName, string password)
        {
            string expected;

            if (!TestCredentials.TryGetPassword(userName, out expected) || !string.Equals(expected, password, StringComparison.Ordinal))
            {
                SecuredDiagnosticsLog.Write(nameof(InMemoryUserNamePasswordValidator), "Refused user name token for '" + userName + "' (password not quoted).");

                throw new SecurityTokenValidationException("The user name or password is not known.");
            }

            SecuredDiagnosticsLog.Write(nameof(InMemoryUserNamePasswordValidator), "Accepted user name token for '" + userName + "'.");
        }
    }
}
