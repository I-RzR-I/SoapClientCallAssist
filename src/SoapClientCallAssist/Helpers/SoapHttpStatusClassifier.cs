// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 20:05
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-11 20:05
//  ***********************************************************************
//  <copyright file="SoapHttpStatusClassifier.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using RzR.Extensions.Domain.Primitives;
using RzR.Extensions.Domain.Text;
using RzR.ResultMessage;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Enums;
using SoapClientCallAssist.Extensions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Messages = SoapClientCallAssist.Helpers.DefaultResultMessageHelper;
using MessageCodes = SoapClientCallAssist.Enums.MessageCodesType;

#endregion

namespace SoapClientCallAssist.Helpers
{
    /// <summary>
    ///     Turns the HTTP status of a received SOAP response into the send result. A 2xx status
    ///     succeeds; any other status fails under its status class code and still carries the
    ///     <see cref="HttpResponseMessage" /> in <see cref="IResult{T}.Response" />.
    /// </summary>
    internal static class SoapHttpStatusClassifier
    {
        /// <summary>
        ///     The longest realm quoted from a <c>WWW-Authenticate</c> challenge, in characters.
        /// </summary>
        private const int MaxRealmLength = 128;

        /// <summary>
        ///     The text placed in the 401 message when the response carries no usable
        ///     <c>WWW-Authenticate</c> challenge.
        /// </summary>
        private const string NoChallenge = "no WWW-Authenticate challenge";

        /// <summary>
        ///     Matches the <c>realm</c> parameter of a challenge, quoted or as a bare token.
        /// </summary>
        private static readonly Regex RealmParameter = new(
            "realm\\s*=\\s*(?:\"(?<quoted>[^\"]*)\"|(?<token>[^\\s,;]+))",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>
        ///     Classifies a received response by its HTTP status.
        /// </summary>
        /// <param name="response">The response the transport delivered.</param>
        /// <returns>
        ///     A successful IResult&lt;HttpResponseMessage&gt; for a 2xx status, otherwise a failed one
        ///     under the code of the status class that still carries the response.
        /// </returns>
        internal static IResult<HttpResponseMessage> Classify(HttpResponseMessage response)
        {
            if (response.IsNull())
                return Result<HttpResponseMessage>.Failure(MessageCodes.ER_BEC_HTTP_STS.GetDescription(), Messages.GetErrorMessage(MessageCodes.ER_BEC_HTTP_STS));

            if (response.IsSuccessStatusCode)
                return Result<HttpResponseMessage>.Success(response);

            var code = CodeFor(response);
            var status = (int)response.StatusCode;

            var text = code == MessageCodes.ER_BEC_HTTP_401
                ? Messages.GetErrorMessage(code).TryFormatWith(DescribeChallenge(response))
                : Messages.GetErrorMessage(code).TryFormatWith(status, response.StatusCode);

            return Result<HttpResponseMessage>.Failure(code.GetDescription(), text).SetResult(response);
        }

        /// <summary>
        ///     Picks the code for a non-2xx response by status, with a 400 or 500 declaring a SOAP media
        ///     type reported as a fault.
        /// </summary>
        /// <param name="response">The non-2xx response.</param>
        /// <returns>
        ///     The code matching the response's status class.
        /// </returns>
        private static MessageCodes CodeFor(HttpResponseMessage response)
        {
            var status = (int)response.StatusCode;

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return MessageCodes.ER_BEC_HTTP_401;

            if (response.StatusCode == HttpStatusCode.Forbidden)
                return MessageCodes.ER_BEC_HTTP_403;

            if ((response.StatusCode == HttpStatusCode.InternalServerError || response.StatusCode == HttpStatusCode.BadRequest)
                && CarriesSoapMediaType(response))
                return MessageCodes.ER_BEC_HTTP_FLT;

            if (status >= 300 && status < 400)
                return MessageCodes.ER_BEC_HTTP_3XX;

            if (status >= 400 && status < 500)
                return MessageCodes.ER_BEC_HTTP_4XX;

            if (status >= 500 && status < 600)
                return MessageCodes.ER_BEC_HTTP_5XX;

            return MessageCodes.ER_BEC_HTTP_STS;
        }

        /// <summary>
        ///     Determines whether the response declares one of the two SOAP media types.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <returns>
        ///     True when the content type is <c>application/soap+xml</c> or <c>text/xml</c>.
        /// </returns>
        private static bool CarriesSoapMediaType(HttpResponseMessage response)
        {
            var mediaType = response.Content?.Headers?.ContentType?.MediaType;

            return mediaType.IsPresent()
                   && (string.Equals(mediaType, SoapMediaType.Soap12.GetDescription(), StringComparison.OrdinalIgnoreCase)
                       || string.Equals(mediaType, SoapMediaType.Soap11.GetDescription(), StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        ///     Renders the <c>WWW-Authenticate</c> challenges of a 401 as scheme and realm only, never
        ///     any other parameter or the body.
        /// </summary>
        /// <param name="response">The 401 response.</param>
        /// <returns>
        ///     The rendered challenges, or a fixed text when there is none.
        /// </returns>
        private static string DescribeChallenge(HttpResponseMessage response)
        {
            try
            {
                var rendered = new List<string>();

                foreach (var challenge in response.Headers.WwwAuthenticate)
                    rendered.Add(Render(challenge));

                return rendered.Count == 0 ? NoChallenge : string.Join(", ", rendered);
            }
            catch (Exception)
            {
                return NoChallenge;
            }
        }

        /// <summary>
        ///     Renders one challenge as its scheme, followed by its realm when it names one.
        /// </summary>
        /// <param name="challenge">The parsed challenge.</param>
        /// <returns>
        ///     The rendered challenge.
        /// </returns>
        private static string Render(AuthenticationHeaderValue challenge)
        {
            if (challenge.Parameter.IsMissing())
                return challenge.Scheme;

            var match = RealmParameter.Match(challenge.Parameter);
            if (match.Success.IsFalse())
                return challenge.Scheme;

            var realm = match.Groups["quoted"].Success ? match.Groups["quoted"].Value : match.Groups["token"].Value;

            return $"{challenge.Scheme} (realm \"{realm.Truncate(MaxRealmLength, true)}\")";
        }
    }
}
