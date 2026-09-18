#region U S I N G

using Microsoft.Extensions.DependencyInjection;
using SoapClientCallAssist;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Enums;
using System;

#endregion

namespace SoapClientCallAssistTests.Helpers
{
    public static class SoapClientFactory
    {
        public static ServiceProvider BuildProvider()
        {
            var services = new ServiceCollection();
            services.RegisterSoapClientsEndpoint();

            return services.BuildServiceProvider();
        }

        public static Func<SoapProtocolType, ISoapClientEndpoint> CreateClientFactory()
        {
            return BuildProvider().GetRequiredService<Func<SoapProtocolType, ISoapClientEndpoint>>();
        }
    }
}
