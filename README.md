> **Note** This repository is developed using .netstandard2.0.

| Name     | Details |
|----------|----------|
| SoapClientCallAssist | [![NuGet Version](https://img.shields.io/nuget/v/SoapClientCallAssist.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/SoapClientCallAssist/) [![Nuget Downloads](https://img.shields.io/nuget/dt/SoapClientCallAssist.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/SoapClientCallAssist)|

This repository provides a simple solution to call/invoke `SOAP` (`WCF`, `ASMX`) service using only basic definitions.

Through basic definition is meaning: `Action`, `XML`, `HTTP method` and others.

If you ever worked with `SOAP` services, you know that every service needed to be called must be added as a service reference and with a few configurations can be invoked in dependence on the specifications. 
But whenever the source service is changed/added or deleted you will get an exception if changes affect something you can see (methods definition, result, etc).

A single solution, in this case, is to update the service reference(configuration) from the source `URL` or update the configuration from `WSDL`, but in some cases, it may be tough, impossible, complicated, or too may take much time to obtain WSDL or access to the resource.

So the basic idea is to avoid this dependency on the `WSDL` service definition and build requests with minimum required info. This repository is created to improve your experience in a positive direction and make your work easy with productive time spent.

On top of the raw envelope builder, the library also ships an attribute-driven model mapper (`ISoapModelMapper`). Instead of composing `XElement` trees by hand for every request, you decorate a plain C# class with `[SoapContract]` and `[SoapMember]` and let the mapper turn it into the SOAP body, then bind the response straight back into a model of your choice.

Outgoing messages can also be signed with WS-Security, the certificate, the signed parts and the algorithms all configured per request through `SoapSecurityDto`. Verifying the signature on the response is a separate, explicit call and is never done for you, see the [WS-Security section](docs/usage.md#ws-security) of the usage doc.

The same `SoapSecurityDto` selects one of four WS-Security modes, and refuses by code any combination it cannot build: the asymmetric X.509 signature above, a token-only header (a `UsernameToken` with a text or digest password, or a SAML assertion), the WS-Security 1.1 symmetric binding a WCF `wsHttpBinding` in Message mode speaks (encrypted key, derived keys, HMAC signature, optional body and signature encryption), and WS-SecureConversation sessions issued and cancelled through `SoapSecureConversationClient`. See [Security header modes](docs/usage.md#ws-security-modes), [UsernameToken](docs/usage.md#username-token), [Symmetric binding](docs/usage.md#symmetric-binding), [SAML](docs/usage.md#saml) and [Secure conversation](docs/usage.md#secure-conversation).

A response to a symmetric or secure-conversation request is checked against the request that was sent, through the standalone `ISoapResponseSecurity` service, whose `DecryptAndVerify` decrypts and verifies in one atomic operation and whose per-request key material is single-use; see [Response security](docs/usage.md#response-security). A send that gets a non-2xx status is a failed result that still carries the `HttpResponseMessage`, under a code naming the status class, see the [send contract](docs/usage.md#send-contract).

A client certificate, proxy or custom handler is attached through the named `HttpClient` the library sends through, see the [HTTP transport section](docs/usage.md#http-transport). Transport authentication (mTLS with a pinned server certificate, Negotiate/NTLM with the process identity; Kerberos is not provided) is configured on that same handler, see [Transport authentication](docs/usage.md#transport-authentication).

The test suites, the real WCF and IIS Express hosts they run against, and the environment variables they read are described under [Test environment](docs/usage.md#test-environment).

To understand more efficiently how you can use available functionalities please consult the [using documentation/file](docs/usage.md).

**In case you wish to use it in your project, u can install the package from <a href="https://www.nuget.org/packages/SoapClientCallAssist" target="_blank">nuget.org</a>** or specify what version you want:

> `Install-Package SoapClientCallAssist -Version x.x.x.x`

## Content
1. [USING](docs/usage.md)
1. [CHANGELOG](docs/CHANGELOG.md)
1. [BRANCH-GUIDE](docs/branch-guide.md)