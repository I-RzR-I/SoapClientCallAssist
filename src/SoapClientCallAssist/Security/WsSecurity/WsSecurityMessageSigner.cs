// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-03 00:09
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-12 02:30
//  ***********************************************************************
//  <copyright file="WsSecurityMessageSigner.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using RzR.Extensions.Domain.Primitives;
using RzR.ResultMessage;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Extensions;
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using System.Xml.Linq;
using Messages = SoapClientCallAssist.Helpers.DefaultResultMessageHelper;
using MessageCodes = SoapClientCallAssist.Enums.MessageCodesType;

#endregion

namespace SoapClientCallAssist.Security.WsSecurity
{
    /// <summary>
    ///     Applies WS-Security to a built SOAP envelope by planning the one mode the options select,
    ///     building the Security header in that mode and computing every signature through the
    ///     signature emitter.
    /// </summary>
    public sealed class WsSecurityMessageSigner : ISoapMessageSigner
    {
        /// <inheritdoc />
        public IResult<string> Sign(XElement soapEnvelope, SoapSecurityDto options)
        {
            var signed = SignCore(soapEnvelope, options, null, null, out var material);

            material?.Dispose();

            return signed;
        }

        /// <summary>
        ///     Signs an envelope and mints the single-use key material the response check later
        ///     consumes. The public <see cref="Sign" /> runs the same operation and disposes the
        ///     material on the spot.
        /// </summary>
        /// <param name="soapEnvelope">The built SOAP envelope, not yet serialized.</param>
        /// <param name="options">The security options.</param>
        /// <param name="transportAction">The request's own action, or null.</param>
        /// <param name="endpoint">The endpoint the request is built for, or null.</param>
        /// <param name="material">[out] The key material, or null when unsigned or on failure.</param>
        /// <returns>
        ///     The envelope's wire form, signed when the options enable signing, or a failed result for
        ///     a null envelope, a missing RSA private key or a signing error.
        /// </returns>
        internal IResult<string> SignCore(XElement soapEnvelope, SoapSecurityDto options,
            string transportAction, Uri endpoint, out RequestKeyMaterial material)
        {
            material = null;

            try
            {
                if (options.IsNull() || options!.Enabled.IsFalse())
                    return Result<string>.Success(soapEnvelope?.ToString(SaveOptions.DisableFormatting));

                if (soapEnvelope.IsNull())
                    return InsufficientSigningInput();

                var planned = SoapSecurityPlanner.Plan(options, transportAction, endpoint);
                if (planned.IsSuccess.IsFalse())
                    return planned.Propagate<string>();

                var plan = planned.Response;

                using (var privateKey = ResolvePrivateKey(plan, out var keyError))
                {
                    if (keyError.IsNotNull())
                        return keyError;

                    if (plan.RequiresRsaPrivateKey && privateKey.IsNull())
                        return MissingSigningKey();

                    return Build(soapEnvelope, plan, privateKey, out material);
                }
            }
            catch (Exception ex)
            {
                return Result<string>
                    .Failure(MessageCodes.ER_SEC_SGN.GetDescription(), Messages.GetErrorMessage(MessageCodes.ER_SEC_SGN))
                    .WithOptionalError(ex, "signing the SOAP message");
            }
        }

        /// <summary>
        ///     Resolves the RSA private key of the signing certificate when the plan needs one,
        ///     reporting a key-access exception as a failure.
        /// </summary>
        /// <param name="plan">The plan.</param>
        /// <param name="error">The failure to return, or null when the key was resolved or not needed.</param>
        /// <returns>
        ///     The RSA private key, or null when the plan needs none or the certificate carries none.
        /// </returns>
        private static RSA ResolvePrivateKey(SecurityHeaderPlan plan, out IResult<string> error)
        {
            error = null;

            if (plan.RequiresRsaPrivateKey.IsFalse())
                return null;

            try
            {
                return ResolvePrivateKey(plan.Options.SigningCertificate);
            }
            catch (Exception ex)
            {
                error = Result<string>
                    .Failure(MessageCodes.ER_SEC_KEY.GetDescription(), Messages.GetErrorMessage(MessageCodes.ER_SEC_KEY))
                    .WithOptionalError(ex, "reading the signing certificate's private key");

                return null;
            }
        }

        /// <summary>
        ///     Reads the RSA private key off a certificate.
        /// </summary>
        /// <param name="certificate">The certificate, or null.</param>
        /// <returns>
        ///     The key, or null when the certificate is absent or carries no RSA private key.
        /// </returns>
        private static RSA ResolvePrivateKey(X509Certificate2 certificate)
            => certificate.IsNull() ? null : certificate!.GetRSAPrivateKey();

        /// <summary>
        ///     Loads the envelope compact, with no synthesised indentation and whitespace preserved,
        ///     builds the Security header and mints the key material.
        /// </summary>
        /// <param name="soapEnvelope">The built SOAP envelope, not yet serialized.</param>
        /// <param name="plan">The plan.</param>
        /// <param name="privateKey">The signing certificate's RSA private key, or null when not needed.</param>
        /// <param name="material">The key material of the built request, or null on failure.</param>
        /// <returns>
        ///     The signed envelope, or a failed result when the built envelope does not parse.
        /// </returns>
        private static IResult<string> Build(XElement soapEnvelope, SecurityHeaderPlan plan, 
            RSA privateKey, out RequestKeyMaterial material)
        {
            material = null;

            var document = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };

            try
            {
                document.LoadXml(soapEnvelope.ToString(SaveOptions.DisableFormatting));
            }
            catch (XmlException ex)
            {
                return Result<string>
                    .Failure(MessageCodes.ER_SEC_DOM.GetDescription(), Messages.GetErrorMessage(MessageCodes.ER_SEC_DOM))
                    .WithOptionalError(ex, "parsing the built SOAP envelope for WS-Security signing");
            }

            IResult<WsSecurityHeaderBuildResult> built;

            using (var builder = new WsSecurityHeaderBuilder(document, plan, privateKey))
                built = builder.Build();

            if (built.IsSuccess.IsFalse())
                return built.Propagate<string>();

            using (var result = built.Response)
            {
                material = RequestKeyMaterial.Snapshot(plan, result);

                return Result<string>.Success(result.Wire);
            }
        }

        /// <summary>
        ///     Builds the failure returned when signing is enabled but the envelope is missing.
        /// </summary>
        /// <returns>
        ///     A failed IResult&lt;string&gt;.
        /// </returns>
        private static IResult<string> InsufficientSigningInput()
            => Result<string>.Failure(MessageCodes.V_SEC_001.GetDescription(), Messages.GetValidationMessage(MessageCodes.V_SEC_001));

        /// <summary>
        ///     Builds the failure returned when the plan needs the signing certificate's private key
        ///     and the certificate carries no usable RSA one.
        /// </summary>
        /// <returns>
        ///     A failed IResult&lt;string&gt;.
        /// </returns>
        private static IResult<string> MissingSigningKey()
            => Result<string>.Failure(MessageCodes.V_SEC_002.GetDescription(), Messages.GetValidationMessage(MessageCodes.V_SEC_002));
    }
}
