// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 21:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-12 02:30
//  ***********************************************************************
//  <copyright file="WsSecurityHeaderBuilder.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using RzR.Extensions.Domain.Collections;
using RzR.Extensions.Domain.Primitives;
using RzR.Extensions.Domain.Text;
using RzR.ResultMessage;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssist.Extensions;
using SoapClientCallAssist.Helpers;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Xml;
using Messages = SoapClientCallAssist.Helpers.DefaultResultMessageHelper;
using MessageCodes = SoapClientCallAssist.Enums.MessageCodesType;

#endregion

namespace SoapClientCallAssist.Security.WsSecurity
{
    /// <summary>
    ///     Builds the <c>wsse:Security</c> header of one request from its plan. The core owns the
    ///     document, id allocator, Security header and order tables, while each concern that fills the
    ///     header lives behind a <c>partial void</c> hook.
    /// </summary>
    internal sealed partial class WsSecurityHeaderBuilder : IDisposable
    {
        /// <summary>
        ///     The element order of the asymmetric X.509 binding, with the optional tokens between the
        ///     timestamp and the signature.
        /// </summary>
        private static readonly SecurityHeaderElementKind[] AsymmetricX509Order =
        {
            SecurityHeaderElementKind.BinarySecurityToken,
            SecurityHeaderElementKind.Timestamp,
            SecurityHeaderElementKind.UsernameToken,
            SecurityHeaderElementKind.SamlAssertion,
            SecurityHeaderElementKind.PrimarySignature
        };

        /// <summary>
        ///     The element order of the symmetric binding keyed by an encrypted key. The encrypted
        ///     key precedes the derived key tokens that reference it, and the endorsing signature is
        ///     last.
        /// </summary>
        private static readonly SecurityHeaderElementKind[] SymmetricEncryptedKeyOrder =
        {
            SecurityHeaderElementKind.Timestamp,
            SecurityHeaderElementKind.EncryptedKey,
            SecurityHeaderElementKind.DerivedKeyTokens,
            SecurityHeaderElementKind.ReferenceList,
            SecurityHeaderElementKind.EncryptedUsernameToken,
            SecurityHeaderElementKind.BinarySecurityToken,
            SecurityHeaderElementKind.PrimarySignature,
            SecurityHeaderElementKind.EndorsingSignature
        };

        /// <summary>
        ///     The element order of the secure conversation binding, the encrypted key order with the
        ///     security context token in the key's place, before the derived key tokens that reference
        ///     it by URI.
        /// </summary>
        private static readonly SecurityHeaderElementKind[] SecureConversationOrder =
        {
            SecurityHeaderElementKind.Timestamp,
            SecurityHeaderElementKind.SecurityContextToken,
            SecurityHeaderElementKind.DerivedKeyTokens,
            SecurityHeaderElementKind.ReferenceList,
            SecurityHeaderElementKind.EncryptedUsernameToken,
            SecurityHeaderElementKind.BinarySecurityToken,
            SecurityHeaderElementKind.PrimarySignature,
            SecurityHeaderElementKind.EndorsingSignature
        };

        /// <summary>
        ///     The element order of a token-only header, which carries no signature.
        /// </summary>
        private static readonly SecurityHeaderElementKind[] TokenOnlyOrder =
        {
            SecurityHeaderElementKind.Timestamp,
            SecurityHeaderElementKind.UsernameToken,
            SecurityHeaderElementKind.SamlAssertion
        };

        /// <summary>
        ///     The envelope document the header is built into.
        /// </summary>
        private readonly XmlDocument _document;

        /// <summary>
        ///     The plan the header is built from.
        /// </summary>
        private readonly SecurityHeaderPlan _plan;

        /// <summary>
        ///     The signing certificate's RSA private key, borrowed from the signer for the duration of
        ///     the build, or null when the plan needs none.
        /// </summary>
        private readonly RSA _rsaPrivateKey;

        /// <summary>
        ///     The id allocator of this build.
        /// </summary>
        private readonly WsuIdAllocator _ids = new();

        /// <summary>
        ///     The SOAP envelope namespace of the document.
        /// </summary>
        private readonly string _envelopeNamespace;

        /// <summary>
        ///     The ids of the elements emitted into the Security header that the primary signature
        ///     covers, in emission order.
        /// </summary>
        private readonly List<string> _referenceIds = new();

        /// <summary>
        ///     The caller-supplied additional ids, resolved and validated before anything is emitted.
        /// </summary>
        private readonly List<string> _additionalIds = new();

        /// <summary>
        ///     The ids of the addressing headers, covered after the additional ids.
        /// </summary>
        private readonly List<string> _addressingIds = new();

        /// <summary>
        ///     Every nonce the build generated, base64 encoded as written on the wire.
        /// </summary>
        private readonly List<string> _nonces = new();

        /// <summary>
        ///     The SOAP Header the Security header sits in.
        /// </summary>
        private XmlElement _header;

        /// <summary>
        ///     The single Security header of the actor.
        /// </summary>
        private XmlElement _security;

        /// <summary>
        ///     The <c>wsu:Id</c> of the embedded certificate token, or null when none was emitted.
        /// </summary>
        private string _binarySecurityTokenId;

        /// <summary>
        ///     The <c>KeyInfo</c> clause the primary RSA signature carries in place of the certificate
        ///     token reference, or null to reference the embedded token. Set by the SAML partial on a
        ///     holder-of-key confirmation.
        /// </summary>
        private KeyInfoClause _primaryKeyInfoClause;

        /// <summary>
        ///     The <c>wsa:MessageID</c> emitted, or null.
        /// </summary>
        private string _messageId;

        /// <summary>
        ///     The primary signature once emitted.
        /// </summary>
        private EmittedSignature _primarySignature;

        /// <summary>
        ///     The outcome the last hook reported, or null when the hook has no implementation.
        /// </summary>
        private IResult _hookOutcome;

        /// <summary>
        ///     Initializes a new instance of the <see cref="WsSecurityHeaderBuilder" /> class.
        /// </summary>
        /// <param name="document">The envelope document, loaded with whitespace preserved.</param>
        /// <param name="plan">The plan to build.</param>
        /// <param name="rsaPrivateKey">The borrowed RSA private key, or null if not needed.</param>
        internal WsSecurityHeaderBuilder(XmlDocument document, SecurityHeaderPlan plan, RSA rsaPrivateKey)
        {
            _document = document;
            _plan = plan;
            _rsaPrivateKey = rsaPrivateKey;
            _envelopeNamespace = document.DocumentElement?.NamespaceURI;
        }

        /// <summary>
        ///     Gets the security options the plan was resolved from.
        /// </summary>
        private SoapSecurityDto Options => _plan.Options;

        /// <summary>
        ///     Resolves the element order table of a mode.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <returns>
        ///     The order the Security header's children are emitted in.
        /// </returns>
        private static IReadOnlyList<SecurityHeaderElementKind> ElementOrderFor(SoapSecurityModeType mode)
        {
            switch (mode)
            {
                case SoapSecurityModeType.SymmetricEncryptedKey:
                    return SymmetricEncryptedKeyOrder;
                case SoapSecurityModeType.SecureConversation:
                    return SecureConversationOrder;
                case SoapSecurityModeType.TokenOnly:
                    return TokenOnlyOrder;
                default:
                    return AsymmetricX509Order;
            }
        }

        /// <summary>
        ///     Builds the header by locating the Body, ensuring the Header and Security header, and
        ///     walking the mode's order table, disposing the symmetric key source on every exit.
        /// </summary>
        /// <returns>
        ///     Success carrying the built envelope and its response key material, otherwise the first
        ///     stage's failure.
        /// </returns>
        internal IResult<WsSecurityHeaderBuildResult> Build()
        {
            try
            {
                return BuildCore();
            }
            finally
            {
                Dispose();
            }
        }

        /// <summary>
        ///     Zeroes the secret and nonces of a symmetric build still held by the builder. Idempotent.
        /// </summary>
        public void Dispose() => _symmetricKeySource?.Dispose();

        /// <summary>
        ///     Runs the build without disposing the key source; see <see cref="Build" />.
        /// </summary>
        /// <returns>
        ///     Success carrying the built envelope and its response key material, otherwise the first
        ///     stage's failure.
        /// </returns>
        private IResult<WsSecurityHeaderBuildResult> BuildCore()
        {
            var body = LocateBody();
            if (body.IsNull())
                return InsufficientSigningInput().Propagate<WsSecurityHeaderBuildResult>();

            _header = EnsureHeader();
            _security = FindSecurityHeader() ?? CreateSecurityHeader();

            var additional = ResolveAdditionalIds(Options.AdditionalSignedElementIds, body);
            if (additional.IsSuccess.IsFalse())
                return additional.Propagate<WsSecurityHeaderBuildResult>();

            if (_plan.EmitsSignature && Options.SignBody)
                _referenceIds.Add(_ids.Stamp(_document, body, "id"));

            if (_plan.IncludesAddressing)
                EmitAddressing();

            foreach (var kind in ElementOrderFor(_plan.Mode))
            {
                var emitted = Emit(kind);
                if (emitted.IsSuccess.IsFalse())
                    return emitted.Propagate<WsSecurityHeaderBuildResult>();
            }

            _hookOutcome = null;
            AfterSignatures();
            if (_hookOutcome.IsNotNull() && _hookOutcome!.IsSuccess.IsFalse())
                return _hookOutcome.Propagate<WsSecurityHeaderBuildResult>();

            byte[] encryptedKeySha1 = null;
            byte[] sessionSecret = null;
            CollectSymmetricMaterial(ref encryptedKeySha1, ref sessionSecret);

            return Result<WsSecurityHeaderBuildResult>.Success(new WsSecurityHeaderBuildResult(
                _document.OuterXml, _primarySignature?.SignatureValue, _nonces.ToArray(),
                _messageId, encryptedKeySha1, sessionSecret));
        }

        /// <summary>
        ///     Emits one kind of element if the options call for it.
        /// </summary>
        /// <param name="kind">The kind to emit.</param>
        /// <returns>
        ///     Success when the element was emitted or not called for, otherwise the concern's failure.
        /// </returns>
        private IResult Emit(SecurityHeaderElementKind kind)
        {
            switch (kind)
            {
                case SecurityHeaderElementKind.BinarySecurityToken:
                    if (_plan.EmitsBinarySecurityToken)
                        EmitBinarySecurityToken();

                    return Result.Success();

                case SecurityHeaderElementKind.Timestamp:
                    if (Options.IncludeTimestamp)
                        EmitTimestamp();

                    return Result.Success();

                case SecurityHeaderElementKind.UsernameToken:
                    if (Options.UsernameToken.IsNotNull() && _plan.EncryptUsernameToken.IsFalse())
                        EmitUsernameToken();

                    return Result.Success();

                case SecurityHeaderElementKind.SamlAssertion:
                    if (Options.SamlToken.IsNull())
                        return Result.Success();

                    _hookOutcome = null;
                    EmitSamlAssertion();

                    return HookOutcome(MessageCodes.V_SEC_080);

                case SecurityHeaderElementKind.EncryptedKey:
                    if (_plan.Mode != SoapSecurityModeType.SymmetricEncryptedKey)
                        return Result.Success();

                    _hookOutcome = null;
                    EmitEncryptedKey();

                    return HookOutcome(MessageCodes.V_SEC_034);

                case SecurityHeaderElementKind.SecurityContextToken:
                    if (_plan.Mode != SoapSecurityModeType.SecureConversation)
                        return Result.Success();

                    _hookOutcome = null;
                    EmitSecurityContextToken();

                    return HookOutcome(MessageCodes.V_SEC_060);

                case SecurityHeaderElementKind.DerivedKeyTokens:
                    if (_plan.IsSymmetricFamily.IsFalse())
                        return Result.Success();

                    _hookOutcome = null;
                    EmitDerivedKeyTokens();

                    return HookOutcome(SymmetricNotAvailableCode());

                case SecurityHeaderElementKind.ReferenceList:
                    if (_plan.IsSymmetricFamily.IsFalse() || (Options.Encryption.IsNull() && _plan.EncryptUsernameToken.IsFalse()))
                        return Result.Success();

                    _hookOutcome = null;
                    EmitReferenceList();

                    return HookOutcome(MessageCodes.V_SEC_050);

                case SecurityHeaderElementKind.EncryptedUsernameToken:
                    if (_plan.EncryptUsernameToken.IsFalse())
                        return Result.Success();

                    _hookOutcome = null;
                    EmitEncryptedUsernameToken();

                    return HookOutcome(MessageCodes.V_SEC_050);

                case SecurityHeaderElementKind.PrimarySignature:
                    return _plan.EmitsSignature ? EmitPrimarySignature() : Result.Success();

                case SecurityHeaderElementKind.EndorsingSignature:
                    return _plan.EmitsEndorsingSignature ? EmitEndorsingSignature() : Result.Success();

                default:
                    return Result.Success();
            }
        }

        /// <summary>
        ///     Emits the primary signature over everything the build has covered so far: the Body, the
        ///     timestamp, the tokens that asked to be signed, the caller's additional ids and the
        ///     addressing headers, in that order.
        /// </summary>
        /// <returns>
        ///     Success once the signature is emitted, or a failed result when nothing is covered,
        ///     otherwise the emitter's or key hook's failure.
        /// </returns>
        private IResult EmitPrimarySignature()
        {
            var references = new List<string>(_referenceIds);
            references.AddRange(_additionalIds);
            references.AddRange(_addressingIds);

            if (references.Count == 0)
                return InsufficientSigningInput();

            var family = _plan.KeyFamily!.Value;

            var spec = new SignatureSpec(
                family,
                family == SignatureKeyFamily.Rsa ? Options.SignatureAlgorithm.GetDescription() : SymmetricSignatureMethod(),
                Options.DigestAlgorithm.GetDescription(),
                Options.Canonicalization,
                references);

            if (_plan.EmitsEndorsingSignature)
                spec.Id = _ids.Next("sig");

            if (family == SignatureKeyFamily.Rsa)
            {
                spec.RsaKey = _rsaPrivateKey;
                spec.KeyInfoClause = _primaryKeyInfoClause ?? SecurityTokenReferenceClause.ToBinarySecurityToken(_binarySecurityTokenId);
            }
            else
            {
                _hookOutcome = null;
                ProvidePrimarySignatureKey(spec);

                var provided = HookOutcome(SymmetricNotAvailableCode());
                if (provided.IsSuccess.IsFalse())
                    return provided;
            }

            var emitted = SignatureEmitter.Emit(_document, _security, spec);

            if (spec.HmacKey.IsNotNull())
                Array.Clear(spec.HmacKey, 0, spec.HmacKey.Length);

            if (emitted.IsSuccess.IsFalse())
                return emitted.ToBase();

            _primarySignature = emitted.Response;

            return Result.Success();
        }

        /// <summary>
        ///     Emits the endorsing signature, an RSA signature with the caller's certificate whose one
        ///     reference is the primary signature, addressed by the plain <c>Id</c> stamped on it.
        /// 
        /// </summary>
        /// <returns>
        ///     Success once emitted, or a failed result when no primary signature was stamped, otherwise
        ///     the emitter's failure.
        /// </returns>
        private IResult EmitEndorsingSignature()
        {
            if (_primarySignature.IsNull() || _primarySignature!.Id.IsMissing())
                return InsufficientSigningInput();

            var spec = new SignatureSpec(
                SignatureKeyFamily.Rsa,
                Options.SignatureAlgorithm.GetDescription(),
                Options.DigestAlgorithm.GetDescription(),
                Options.Canonicalization,
                new[] { _primarySignature.Id })
            {
                RsaKey = _rsaPrivateKey,
                KeyInfoClause = SecurityTokenReferenceClause.ToBinarySecurityToken(_binarySecurityTokenId)
            };

            var emitted = SignatureEmitter.Emit(_document, _security, spec);

            return emitted.IsSuccess.IsFalse() ? emitted.ToBase() : Result.Success();
        }

        /// <summary>
        ///     Resolves the HMAC signature method of the symmetric mode in use.
        /// </summary>
        /// <returns>
        ///     The signature method URI.
        /// </returns>
        private string SymmetricSignatureMethod()
            => (Options.SymmetricBinding?.SignatureAlgorithm ?? SoapSymmetricSignatureAlgorithmType.HmacSha256).GetDescription();

        /// <summary>
        ///     Resolves the "not available" code of the symmetric mode in use.
        /// </summary>
        /// <returns>
        ///     The message code.
        /// </returns>
        private MessageCodes SymmetricNotAvailableCode()
            => _plan.Mode == SoapSecurityModeType.SecureConversation ? MessageCodes.V_SEC_060 : MessageCodes.V_SEC_034;

        /// <summary>
        ///     Reads the outcome the last hook reported, refusing under the "not available" code when a
        ///     hook with no implementation reported nothing.
        /// </summary>
        /// <param name="notAvailable">The code to refuse under when the hook has no implementation.</param>
        /// <returns>
        ///     The hook's outcome, or a failure carrying <paramref name="notAvailable" />.
        /// </returns>
        private IResult HookOutcome(MessageCodes notAvailable)
            => _hookOutcome ?? Result.Failure(notAvailable.GetDescription(), Messages.GetValidationMessage(notAvailable));

        /// <summary>
        ///     Locates the SOAP Body directly under the envelope.
        /// </summary>
        /// <returns>
        ///     The Body element, or null when the document is not an envelope carrying one.
        /// </returns>
        private XmlElement LocateBody()
        {
            var namespaceManager = new XmlNamespaceManager(_document.NameTable);
            namespaceManager.AddNamespace("s", _envelopeNamespace!);

            return _document.SelectSingleNode("/s:Envelope/s:Body", namespaceManager) as XmlElement;
        }

        /// <summary>
        ///     Locates the SOAP Header, creating and inserting one as the Envelope's first child when
        ///     the envelope was built without one.
        /// </summary>
        /// <returns>
        ///     The Header element.
        /// </returns>
        private XmlElement EnsureHeader()
        {
            var namespaceManager = new XmlNamespaceManager(_document.NameTable);
            namespaceManager.AddNamespace("s", _envelopeNamespace!);

            var header = (XmlElement)_document.SelectSingleNode("/s:Envelope/s:Header", namespaceManager);
            if (header.IsNotNull())
                return header;

            header = _document.CreateElement(_document.DocumentElement!.Prefix, WsSecurityNames.HeaderLocalName, _envelopeNamespace);
            _document.DocumentElement.PrependChild(header);

            return header;
        }

        /// <summary>
        ///     Finds the Security header the caller already supplied for this build's actor, reusing it
        ///     as it stands and declaring the utility namespace on it if it was not already.
        /// </summary>
        /// <returns>
        ///     The existing Security header of the actor, or null when the caller supplied none.
        /// </returns>
        private XmlElement FindSecurityHeader()
        {
            var actorAttributeName = ActorAttributeName();

            foreach (XmlNode child in _header.ChildNodes)
            {
                if (child is not XmlElement candidate)
                    continue;

                if (string.Equals(candidate.LocalName, WsSecurityNames.SecurityLocalName, StringComparison.Ordinal).IsFalse()
                    || string.Equals(candidate.NamespaceURI, WsSecurityNames.WsseNamespace, StringComparison.Ordinal).IsFalse())
                    continue;

                var actor = candidate.HasAttribute(actorAttributeName, _envelopeNamespace)
                    ? candidate.GetAttribute(actorAttributeName, _envelopeNamespace)
                    : null;

                if (string.Equals(actor.IfNullThenEmpty(), Options.SecurityActor.IfNullThenEmpty(), StringComparison.Ordinal).IsFalse())
                    continue;

                if (candidate.HasAttribute("xmlns:" + WsSecurityNames.WsuPrefix).IsFalse())
                    candidate.SetAttribute("xmlns:" + WsSecurityNames.WsuPrefix, WsSecurityNames.WsuNamespace);

                return candidate;
            }

            return null;
        }

        /// <summary>
        ///     Creates the Security header of the actor and appends it to the SOAP Header, setting the
        ///     utility namespace, then <c>mustUnderstand</c>, then the actor or role.
        /// </summary>
        /// <returns>
        ///     The attached Security header.
        /// </returns>
        private XmlElement CreateSecurityHeader()
        {
            var security = _document.CreateElement(WsSecurityNames.WssePrefix, WsSecurityNames.SecurityLocalName, WsSecurityNames.WsseNamespace);
            security.SetAttribute("xmlns:" + WsSecurityNames.WsuPrefix, WsSecurityNames.WsuNamespace);

            if (Options.MustUnderstand)
                security.SetAttribute(WsSecurityNames.MustUnderstandLocalName, _envelopeNamespace, "1");

            if (Options.SecurityActor.IsPresent())
                security.SetAttribute(ActorAttributeName(), _envelopeNamespace, Options.SecurityActor);

            _header.AppendChild(security);

            return security;
        }

        /// <summary>
        ///     Resolves the name of the attribute the protocol in use targets a header with.
        /// </summary>
        /// <returns>
        ///     <c>role</c> for SOAP 1.2, <c>actor</c> for SOAP 1.1.
        /// </returns>
        private string ActorAttributeName()
            => string.Equals(_envelopeNamespace, WsSecurityNames.Soap12EnvelopeNamespace, StringComparison.Ordinal)
                ? WsSecurityNames.Soap12RoleLocalName
                : WsSecurityNames.Soap11ActorLocalName;

        /// <summary>
        ///     Resolves each caller-supplied id to exactly one element by its <c>wsu:Id</c> and records it
        ///     for the primary signature.
        /// </summary>
        /// <param name="additionalIds">The caller-supplied ids, or null.</param>
        /// <param name="body">The SOAP Body of the envelope.</param>
        /// <returns>
        ///     Success once every id is recorded, or a failed result.
        /// </returns>
        private IResult ResolveAdditionalIds(IEnumerable<string> additionalIds, XmlElement body)
        {
            if (additionalIds.IsNullOrEmptyEnumerable())
                return Result.Success();

            foreach (var id in additionalIds.NotNull())
            {
                if (id.IsMissing())
                    return InsufficientSigningInput();

                var matches = WsuIdLookup.FindById(_document, id);
                if (matches.Count != 1)
                    return InsufficientSigningInput();

                if (ReferenceEquals(matches[0], body) && _plan.EmitsSignature && Options.SignBody)
                    return BodyNamedAsAdditionalId(id);

                if (IsUniquelyAddressable(matches[0], id).IsFalse())
                    return ShadowedAdditionalId(id);

                if (EnclosesSignatureAnchor(matches[0], _header))
                    return SelfEnclosingSignature(id);

                _additionalIds.Add(id);
            }

            return Result.Success();
        }

        /// <summary>
        ///     Determines whether a candidate element is an ancestor-or-self of the point the signature
        ///     is inserted at.
        /// </summary>
        /// <param name="candidate">The element an additional id resolved to.</param>
        /// <param name="signatureAnchor">The element the signature is inserted into.</param>
        /// <returns>
        ///     True when signing the candidate would enclose the signature.
        /// </returns>
        private static bool EnclosesSignatureAnchor(XmlNode candidate, XmlElement signatureAnchor)
        {
            for (var node = (XmlNode)signatureAnchor; node.IsNotNull(); node = node!.ParentNode)
            {
                if (ReferenceEquals(node, candidate))
                    return true;
            }

            return false;
        }

        /// <summary>
        ///     Appends a text-only child element.
        /// </summary>
        /// <param name="parent">The parent element.</param>
        /// <param name="prefix">The child element's prefix.</param>
        /// <param name="localName">The child element's local name.</param>
        /// <param name="namespaceUri">The child element's namespace.</param>
        /// <param name="value">The child element's text.</param>
        /// <returns>
        ///     The appended child.
        /// </returns>
        private XmlElement AppendTextChild(XmlElement parent, string prefix, string localName, string namespaceUri, string value)
        {
            var child = _document.CreateElement(prefix, localName, namespaceUri);
            child.InnerText = value;
            parent.AppendChild(child);

            return child;
        }

        /// <summary>
        ///     The failure raised when the envelope carries no Body, an additional signed element id is
        ///     blank or does not resolve to exactly one element, or the resulting reference set covers
        ///     nothing.
        /// </summary>
        /// <returns>
        ///     The failed IResult.
        /// </returns>
        private static IResult InsufficientSigningInput()
            => Result.Failure(MessageCodes.V_SEC_001.GetDescription(), Messages.GetValidationMessage(MessageCodes.V_SEC_001));

        /// <summary>
        ///     The failure raised when an additional signed element id names an element that would
        ///     contain the signature itself.
        /// </summary>
        /// <param name="id">The offending id.</param>
        /// <returns>
        ///     The failed IResult naming the id.
        /// </returns>
        private static IResult SelfEnclosingSignature(string id)
            => Result.Failure(
                MessageCodes.V_SEC_014.GetDescription(),
                Messages.GetValidationMessage(MessageCodes.V_SEC_014).TryFormatWith(id));

        /// <summary>
        ///     The failure raised when an additional signed element id is also carried by another
        ///     element in an unqualified id attribute, which a signature reference resolves first.
        /// </summary>
        /// <param name="id">The offending id.</param>
        /// <returns>
        ///     The failed IResult naming the id.
        /// </returns>
        private static IResult ShadowedAdditionalId(string id)
            => Result.Failure(
                MessageCodes.V_SEC_016.GetDescription(),
                Messages.GetValidationMessage(MessageCodes.V_SEC_016).TryFormatWith(id));

        /// <summary>
        ///     The failure raised when an additional signed element id names the Body, whose
        ///     <c>wsu:Id</c> the build replaces before signing it.
        /// </summary>
        /// <param name="id">The offending id.</param>
        /// <returns>
        ///     The failed IResult naming the id.
        /// </returns>
        private static IResult BodyNamedAsAdditionalId(string id)
            => Result.Failure(
                MessageCodes.V_SEC_017.GetDescription(),
                Messages.GetValidationMessage(MessageCodes.V_SEC_017).TryFormatWith(id));

        /// <summary>
        ///     Emits the <c>xenc:EncryptedKey</c> of the symmetric binding. Implemented by the symmetric
        ///     partial; the implementation records its outcome in <see cref="_hookOutcome" />.
        /// </summary>
        partial void EmitEncryptedKey();

        /// <summary>
        ///     Emits the <c>SecurityContextToken</c> of the secure conversation. Implemented by the
        ///     secure conversation partial; the implementation records its outcome in
        ///     <see cref="_hookOutcome" />.
        /// </summary>
        partial void EmitSecurityContextToken();

        /// <summary>
        ///     Emits the <c>DerivedKeyToken</c> elements of a symmetric mode. Implemented by the
        ///     symmetric partial; the implementation records its outcome in <see cref="_hookOutcome" />.
        /// </summary>
        partial void EmitDerivedKeyTokens();

        /// <summary>
        ///     Provides the HMAC key and <c>KeyInfo</c> clause of the primary signature on a symmetric
        ///     mode, setting <see cref="SignatureSpec.HmacKey" /> and
        ///     <see cref="SignatureSpec.KeyInfoClause" /> and recording its outcome in
        ///     <see cref="_hookOutcome" />. The core zeroes the key once the signature is computed.
        /// </summary>
        /// <param name="spec">The spec of the primary signature.</param>
        partial void ProvidePrimarySignatureKey(SignatureSpec spec);

        /// <summary>
        ///     Emits the <c>xenc:ReferenceList</c>. Implemented by the encryption partial; the
        ///     implementation records its outcome in <see cref="_hookOutcome" />.
        /// </summary>
        partial void EmitReferenceList();

        /// <summary>
        ///     Emits the username token wrapped in <c>xenc:EncryptedData</c>. Implemented by the
        ///     encryption partial; the implementation records its outcome in <see cref="_hookOutcome" />.
        /// </summary>
        partial void EmitEncryptedUsernameToken();

        /// <summary>
        ///     Emits the caller's <c>saml:Assertion</c>. Implemented by the SAML partial; the
        ///     implementation records its outcome in <see cref="_hookOutcome" />.
        /// </summary>
        partial void EmitSamlAssertion();

        /// <summary>
        ///     Runs after every signature is emitted, for the encryption partial to encrypt the Body
        ///     and the signature once they exist. The implementation records its outcome in
        ///     <see cref="_hookOutcome" />; leaving it unset means nothing to do.
        /// </summary>
        partial void AfterSignatures();

        /// <summary>
        ///     Runs once the header is complete, for the symmetric partial to hand out the SHA-1 of the
        ///     encrypted key and the secret the response check derives from, ownership included. Any
        ///     other mode leaves both null.
        /// </summary>
        /// <param name="encryptedKeySha1">The SHA-1 of the encrypted key's cipher value, or null.</param>
        /// <param name="sessionSecret">The shared secret, or null.</param>
        partial void CollectSymmetricMaterial(ref byte[] encryptedKeySha1, ref byte[] sessionSecret);
    }
}
