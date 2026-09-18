using Microsoft.Extensions.DependencyInjection;
using SoapClientCallAssist;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using System;
using System.Net.Http;
using System.Text;

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
           client.BuildRequest(HttpMethod.Post, 
               new BuildSoapRequestDto()
               {
                   Client = new HttpClientDto(new Uri("http://env.local"), Encoding.UTF8)
               });

           Console.ReadKey();
        }
    }
}