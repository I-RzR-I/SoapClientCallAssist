// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-07 18:30
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-10 22:10
//  ***********************************************************************
//  <copyright file="SoapHttpClientRegistration.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;

#endregion

namespace SoapClientCallAssist.Helpers
{
    /// <summary>
    ///     Owns the one named <see cref="HttpClient" /> configuration this library sends through, and
    ///     the single fallback container that serves callers who construct a client without dependency
    ///     injection.
    /// </summary>
    internal static class SoapHttpClientRegistration
    {
        /// <summary>
        ///     The name the SOAP <see cref="HttpClient" /> configuration is registered and requested
        ///     under.
        /// </summary>
        internal const string ClientName = "SoapClientCallAssist";

        /// <summary>
        ///     The process-wide container behind <see cref="FallbackFactory" />, built at most once and
        ///     never disposed.
        /// </summary>
        private static readonly Lazy<IHttpClientFactory> LazyFallbackFactory
            = new(BuildFallbackFactory, LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        ///     The shared factory a client constructed without dependency injection sends through.
        /// </summary>
        internal static IHttpClientFactory FallbackFactory => LazyFallbackFactory.Value;

        /// <summary>
        ///     The response compression the library negotiates on its primary handler.
        /// </summary>
        private const DecompressionMethods NegotiatedDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

        /// <summary>
        ///     Registers the named SOAP <see cref="HttpClient" /> and turns on gzip and deflate
        ///     decompression on its primary handler. A primary handler assigned after this call
        ///     needs decompression enabled by its owner.
        /// </summary>
        /// <param name="services">The service collection to register into.</param>
        /// <returns>
        ///     The named client's builder, for further configuration.
        /// </returns>
        internal static IHttpClientBuilder Register(IServiceCollection services)
        {
            var builder = services.AddHttpClient(ClientName);

            services.Configure<HttpClientFactoryOptions>(
                ClientName,
                options => options.HttpMessageHandlerBuilderActions.Add(EnableDecompression));

            return builder;
        }

        /// <summary>
        ///     Enables the negotiated decompression on the client's primary handler when it is an
        ///     <see cref="HttpClientHandler" /> that supports it. Any other handler is left as configured.
        /// </summary>
        /// <param name="builder">The handler builder of the named client.</param>
        private static void EnableDecompression(HttpMessageHandlerBuilder builder)
        {
            if (builder.PrimaryHandler is HttpClientHandler handler && handler.SupportsAutomaticDecompression)
                handler.AutomaticDecompression |= NegotiatedDecompression;
        }

        /// <summary>
        ///     Builds the fallback container and resolves its factory.
        /// </summary>
        /// <returns>
        ///     An IHttpClientFactory.
        /// </returns>
        private static IHttpClientFactory BuildFallbackFactory()
        {
            var services = new ServiceCollection();

            Register(services);

            return services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();
        }
    }
}
