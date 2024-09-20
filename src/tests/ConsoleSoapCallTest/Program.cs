using Microsoft.Extensions.DependencyInjection;
using SoapClientCallAssist;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Enums;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Xml.Linq;

#pragma disable CS0017

namespace ConsoleSoapCallTest
{
    internal class Program
    {
        public static void Main()
        {
            Console.WriteLine("Hello World!");

            var services = new ServiceCollection();
            services.RegisterSoapClientsEndpoint();
            var sp = services.BuildServiceProvider();

           var clientFactory = sp.GetRequiredService<Func<SoapProtocolType, ISoapClientEndpoint>>();

           var client = clientFactory(SoapProtocolType.SOAP_1_1);
           var request = client.BuildRequest(HttpMethod.Post, new Uri("http://env.local"),
               new List<XElement>());

           Console.ReadKey();
        }
    }
}