// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2024-09-12 19:03
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-12 09:30
//  ***********************************************************************
//  <copyright file="SoapClientEndpointExtensions.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Client;
using SoapClientCallAssist.Enums;
using SoapClientCallAssist.Extensions;
using SoapClientCallAssist.Helpers;
using SoapClientCallAssist.Mapping;
using SoapClientCallAssist.Security;
using SoapClientCallAssist.Security.WsSecurity;
using System;

#endregion

namespace SoapClientCallAssist
{
    /// <summary>
    ///     Registers the SOAP clients and their supporting services in a service collection.
    /// </summary>
    public static class SoapClientEndpointExtensions
    {
        /// <summary>
        ///     (Immutable)
        ///     The name of the <see cref="System.Net.Http.HttpClient" /> configuration every SOAP client
        ///     sends through. A caller adds a handler, proxy, certificate or retry policy by configuring
        ///     this name.
        /// </summary>
        public const string SoapHttpClientName = SoapHttpClientRegistration.ClientName;

        /// <summary>
        ///     (Immutable)
        ///     The <see cref="System.Net.Http.HttpRequestMessage.Properties" /> key under which a
        ///     request built with message security carries the single-use snapshot
        ///     <see cref="ISoapResponseSecurity" /> checks the response against. The value is opaque and
        ///     consumed by the first check.
        /// </summary>
        public const string RequestSecurityStateKey = "SoapClientCallAssist.RequestSecurityState";

        /// <summary>
        ///     Registers both SOAP clients as singletons behind a
        ///     <c>Func&lt;SoapProtocolType, ISoapClientEndpoint&gt;</c>, with the named HTTP client, the
        ///     model mapper and the WS-Security services.
        /// </summary>
        /// <param name="services">The service collection to register into.</param>
        public static void RegisterSoapClientsEndpoint(this IServiceCollection services)
        {
            SoapHttpClientRegistration.Register(services);

            services.AddSingleton<Soap11Client>();
            services.AddSingleton<Soap12Client>();

            services.TryAddSingleton<ISoapModelMapper, SoapModelMapper>();
            services.TryAddSingleton<ISoapMessageVerifier, WsSecurityMessageVerifier>();
            services.TryAddSingleton<ISoapResponseSecurity, WsSecurityResponseSecurity>();
            services.TryAddSingleton<SoapSecureConversationClient>();

            services.AddSingleton<Func<SoapProtocolType, ISoapClientEndpoint>>(sp => endpointType =>
            {
                return endpointType switch
                {
                    SoapProtocolType.SOAP_1_1 => sp.GetRequiredService<Soap11Client>(),
                    SoapProtocolType.SOAP_1_2 => sp.GetRequiredService<Soap12Client>(),
                    _ => throw new NotImplementedException(
                        DefaultResultMessageHelper.GetErrorMessage(MessageCodesType.ER_DI_RSCE_001).TryFormatWith(endpointType))
                };
            });
        }
    }
}