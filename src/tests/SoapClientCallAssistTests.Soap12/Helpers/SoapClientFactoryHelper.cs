using Microsoft.Extensions.DependencyInjection;
using SoapClientCallAssist;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Client;
using SoapClientCallAssist.Enums;
using System;

namespace SoapClientCallAssistTests.Soap12.Helpers;

public static class SoapClientFactoryHelper
{

    private static readonly TimeSpan NetworkTimeout = TimeSpan.FromSeconds(15);

    public static ISoapClientEndpoint CreateSoap12Client() => CreateClient(SoapProtocolType.SOAP_1_2);

    public static ISoapClientEndpoint CreateSoap11Client() => CreateClient(SoapProtocolType.SOAP_1_1);

    public static ISoapClientEndpoint CreateDirectSoap12Client() => new Soap12Client();

    public static ISoapClientEndpoint CreateDirectSoap11Client() => new Soap11Client();

    private static ISoapClientEndpoint CreateClient(SoapProtocolType protocol)
    {
        var services = new ServiceCollection();
        services.RegisterSoapClientsEndpoint();

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<Func<SoapProtocolType, ISoapClientEndpoint>>();

        var client = factory(protocol);

        client.SetClientTimeout(NetworkTimeout);

        return client;
    }
}
