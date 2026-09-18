using Microsoft.Extensions.DependencyInjection;
using SoapClientCallAssist;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Client;
using SoapClientCallAssist.Enums;
using System;
using System.Net.Http;

namespace SoapClientCallAssistTests.Soap12.Helpers.Protocol;

public static class SoapClientFactoryHelper
{

    private static readonly TimeSpan NetworkTimeout = TimeSpan.FromSeconds(15);

    public static ISoapClientEndpoint CreateSoap12Client() => CreateClient(SoapProtocolType.SOAP_1_2);

    public static ISoapClientEndpoint CreateSoap11Client() => CreateClient(SoapProtocolType.SOAP_1_1);

    public static ISoapClientEndpoint CreateDirectSoap12Client() => new Soap12Client();

    public static ISoapClientEndpoint CreateDirectSoap11Client() => new Soap11Client();

    public static ISoapClientEndpoint CreateSoap12ClientSendingThrough(DelegatingHandler handler)
    {
        var services = new ServiceCollection();
        services.RegisterSoapClientsEndpoint();

        services
            .AddHttpClient(SoapClientEndpointExtensions.SoapHttpClientName)
            .AddHttpMessageHandler(() => handler);

        return Resolve(services, SoapProtocolType.SOAP_1_2);
    }

    private static ISoapClientEndpoint CreateClient(SoapProtocolType protocol)
    {
        var services = new ServiceCollection();
        services.RegisterSoapClientsEndpoint();

        var client = Resolve(services, protocol);

        client.SetClientTimeout(NetworkTimeout);

        return client;
    }

    private static ISoapClientEndpoint Resolve(IServiceCollection services, SoapProtocolType protocol)
    {
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<Func<SoapProtocolType, ISoapClientEndpoint>>();

        return factory(protocol);
    }
}
