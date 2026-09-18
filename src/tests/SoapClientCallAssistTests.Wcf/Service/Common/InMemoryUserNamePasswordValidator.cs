using System;
using System.Collections.Generic;
using System.IdentityModel.Selectors;
using System.IdentityModel.Tokens;

namespace SoapClientCallAssistTests.Wcf.Service.Common;

public sealed class InMemoryUserNamePasswordValidator : UserNamePasswordValidator
{

    private readonly IReadOnlyDictionary<string, string> _users;

    public InMemoryUserNamePasswordValidator(IReadOnlyDictionary<string, string> users) => _users = users;

    public override void Validate(string userName, string password)
    {
        if (userName is null || password is null)
            throw new ArgumentNullException(userName is null ? nameof(userName) : nameof(password));

        if (!_users.TryGetValue(userName, out var expected) || !string.Equals(expected, password, StringComparison.Ordinal))
            throw new SecurityTokenValidationException("The user name or password is not known.");
    }
}
