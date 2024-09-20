// ***********************************************************************
//  Assembly         : RzR.Shared.Services.SoapClientCallAssist
//  Author           : RzR
//  Created On       : 2024-09-12 19:03
// 
//  Last Modified By : RzR
//  Last Modified On : 2024-09-15 18:43
// ***********************************************************************
//  <copyright file="SoapClientEndpointExtensions.cs" company="">
//   Copyright (c) RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using Microsoft.Extensions.DependencyInjection;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Client;
using SoapClientCallAssist.Enums;
using SoapClientCallAssist.Helper;
using System;

#endregion

namespace SoapClientCallAssist
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     A SOAP client endpoint extensions.
    /// </summary>
    /// =================================================================================================
    public static class SoapClientEndpointExtensions
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     An IServiceCollection extension method that registers the SOAP clients endpoint described
        ///     by services.
        /// </summary>
        /// <param name="services">The services to act on.</param>
        /// =================================================================================================
        public static void RegisterSoapClientsEndpoint(this IServiceCollection services)
        {
            services.AddHttpClient();

            services.AddSingleton<Soap11Client>();
            services.AddSingleton<Soap12Client>();

            services.AddSingleton<Func<SoapProtocolType, ISoapClientEndpoint>>(sp => endpointType =>
            {
                return endpointType switch
                {
                    SoapProtocolType.SOAP_1_1 => sp.GetRequiredService<Soap11Client>(),
                    SoapProtocolType.SOAP_1_2 => sp.GetRequiredService<Soap12Client>(),
                    _ => throw new NotImplementedException(
                        string.Format(DefaultResultMessageHelper.ErrorMessages[MessageCodesType.ER_DI_RSCE_001]))
                };
            });
        }
    }
}