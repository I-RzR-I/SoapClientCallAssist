// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2024-09-15 16:59
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-12 09:30
//  ***********************************************************************
//  <copyright file="DefaultResultMessageHelper.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using SoapClientCallAssist.Enums;
using System.Collections.Generic;

#endregion

namespace SoapClientCallAssist.Helpers
{
    /// <summary>
    ///     Resolves a <see cref="MessageCodesType" /> to its human-readable message text.
    /// </summary>
    internal static class DefaultResultMessageHelper
    {
        /// <summary>
        ///     The error message text of each error code.
        /// </summary>
        internal static readonly Dictionary<MessageCodesType, string> ErrorMessages
            = new()
            {
                { MessageCodesType.ER_DI_RSCE_001, "No implementation for endpoint type: {0}" },
                { MessageCodesType.ER_S11_BSR, "An error occurred while trying to build a SOAP 1.1 request." },
                { MessageCodesType.ER_S11_BSRA, "An error occurred while trying to build a SOAP 1.1 request async." },
                { MessageCodesType.ER_S12_BSR, "An error occurred while trying to build a SOAP 1.2 request." },
                { MessageCodesType.ER_S12_BSRA, "An error occurred while trying to build a SOAP 1.2 request async." },
                { MessageCodesType.ER_S11_SR, "An error occurred while trying to execute/send a SOAP 1.1 request." },
                { MessageCodesType.ER_S11_SRA, "An error occurred while trying to execute/send a SOAP 1.1 request async." },
                { MessageCodesType.ER_S12_SR, "An error occurred while trying to execute/send a SOAP 1.2 request." },
                { MessageCodesType.ER_S12_SRA, "An error occurred while trying to execute/send a SOAP 1.2 request async." },
                { MessageCodesType.ER_BEC_BSRM, "An error occurred while trying to validate and build SOAP request message." },
                { MessageCodesType.ER_BEC_BSRM_SR, "An error occurred while trying to send SOAP request message." },
                { MessageCodesType.ER_BEC_BSRM_SRA, "An error occurred while trying to send SOAP request message async." },
                { MessageCodesType.ER_BEC_VR, "An error occurred while trying to validate input parameters for SOAP request." },
                { MessageCodesType.ER_BEC_GRB_01, "No or more than one SOAP Body in response." },
                { MessageCodesType.ER_BEC_GRB_02, "No child in SOAP Body." },
                { MessageCodesType.ER_BEC_GRB_03, "Invalid SOAP Message in response." },
                { MessageCodesType.ER_BEC_CBFFC, "An error occurred while trying to check response for fault code." },
                { MessageCodesType.ER_BEC_FLT, "The SOAP response carries a SOAP fault; the service answered with a fault rather than a result." },
                { MessageCodesType.ER_BEC_VRS, "An error occurred while trying to verify the WS-Security signature on the SOAP response." },
                { MessageCodesType.ER_BEC_HTTP_3XX, "The service answered the SOAP request with a redirect (HTTP {0} {1}) that was not followed, so the request was not processed; the response is carried in the result." },
                { MessageCodesType.ER_BEC_HTTP_401, "The service refused the SOAP request as unauthenticated (HTTP 401); it challenges with {0}. The response is carried in the result; its body is withheld from this message." },
                { MessageCodesType.ER_BEC_HTTP_403, "The service refused the SOAP request as forbidden (HTTP 403); the identity or client certificate presented is not authorized for the endpoint. The response is carried in the result; its body is withheld from this message." },
                { MessageCodesType.ER_BEC_HTTP_4XX, "The service rejected the SOAP request with HTTP {0} {1}. The response is carried in the result; its body is withheld from this message." },
                { MessageCodesType.ER_BEC_HTTP_FLT, "The service answered the SOAP request with a SOAP fault over HTTP {0}; the fault envelope is carried in the result's response, read its body and pass it to CheckBodyForFaultCode." },
                { MessageCodesType.ER_BEC_HTTP_5XX, "The service failed while processing the SOAP request with HTTP {0} {1}; the response does not carry a SOAP media type, so it is an error page rather than a SOAP fault. The response is carried in the result; its body is withheld from this message." },
                { MessageCodesType.ER_BEC_HTTP_STS, "The service answered the SOAP request with an unexpected HTTP status {0} {1}. The response is carried in the result; its body is withheld from this message." },
                { MessageCodesType.ER_MAP_EMT, "An error occurred while emitting XML for member '{0}' on type '{1}'." },
                { MessageCodesType.ER_MAP_BND, "An error occurred while binding response element '{0}' to '{1}.{2}'." },
                { MessageCodesType.ER_MAP_MTD, "An error occurred while reading SOAP mapping metadata for type '{0}'." },
                { MessageCodesType.ER_MAP_RSP, "The SOAP response could not be read into '{0}' ({1})." },
                { MessageCodesType.ER_MAP_FLT, "The SOAP response carries a SOAP fault; nothing was bound to '{0}'." },
                { MessageCodesType.ER_SEC_SGN, "An error occurred while trying to sign the SOAP message with WS-Security." },
                { MessageCodesType.ER_SEC_C14N, "An error occurred while canonicalizing or computing the WS-Security signature." },
                { MessageCodesType.ER_SEC_KEY, "An error occurred while resolving the RSA key from the supplied certificate." },
                { MessageCodesType.ER_SEC_DOM, "An error occurred while parsing the SOAP message for WS-Security processing." },
                { MessageCodesType.ER_SEC_VER, "The WS-Security signature could not be verified against the expected certificate." },
                { MessageCodesType.ER_XML_DEPTH, "The XML document nesting exceeds the supported depth of {0} elements." },

                #region ERROR CODES - WS-SECURITY DECRYPTION

                { MessageCodesType.ER_SEC_DEC, "An error occurred while decrypting the WS-Security protected SOAP response." },

                #endregion
            };

        /// <summary>
        ///     The validation message text of each validation code.
        /// </summary>
        internal static readonly Dictionary<MessageCodesType, string> ValidationMessages
            = new()
            {
                { MessageCodesType.V_BEC_VR_001, "SOAP request object can not be null." },
                { MessageCodesType.V_BEC_VR_002, "The supplied HTTP method ({0}) is not allowed." },
                { MessageCodesType.V_BEC_VR_003, "SOAP Uri is mandatory." },
                { MessageCodesType.V_BEC_VR_004, "SOAP protocol version is mandatory." },
                { MessageCodesType.V_BEC_VR_005, "SOAP namespace is mandatory." },
                { MessageCodesType.V_BEC_VR_006, "SOAP media type is mandatory." },
                { MessageCodesType.V_BEC_VR_007, "SOAP body encoding is mandatory." },
                { MessageCodesType.V_BEC_HDR_001, "The supplied HTTP header '{0}' could not be applied to the request; its name is accepted neither as a request header nor as a content header, or its value list is null." },
                { MessageCodesType.V_BEC_TMO_001, "The supplied HTTP client timeout must be greater than zero and no longer than {0}, or exactly the infinite timeout sentinel (-1 millisecond) to send without a deadline; a zero or any other negative timeout is refused here rather than failing every later send." },
                { MessageCodesType.V_MAP_001, "Type '{0}' declares no mapped members." },
                { MessageCodesType.V_MAP_002, "Unsupported member type '{0}' for member '{1}'." },
                { MessageCodesType.V_MAP_003, "Duplicate wire name '{0}' declared more than once on type '{1}'." },
                { MessageCodesType.V_MAP_004, "Member '{0}' resolves to an element with no namespace; every mapped element must be namespace-qualified." },
                { MessageCodesType.V_MAP_005, "Element '{0}' is nil but member '{1}' is a non-nullable value type." },
                { MessageCodesType.V_MAP_006, "Type '{0}' mixes [SoapMember] and [DataMember]; use one convention per type." },
                { MessageCodesType.V_MAP_007, "Type graph for '{0}' exceeds the maximum supported nesting depth of {1}." },
                { MessageCodesType.V_MAP_008, "Type '{0}' cannot be instantiated; a public parameterless constructor is required." },
                { MessageCodesType.V_MAP_009, "No SOAP Body element was found in the response, or more than one was present." },
                { MessageCodesType.V_MAP_010, "The SOAP Body of the response holds no element to bind '{0}' from." },
                { MessageCodesType.V_MAP_011, "Element '{0}' carries content, but no item of collection member '{1}.{2}' could be read from it; the response collection shape could not be interpreted." },
                { MessageCodesType.V_MAP_012, "The first element of the SOAP Body is '{0}', but type '{1}' is mapped to element '{2}'; the response does not belong to the operation being bound." },
                { MessageCodesType.V_SEC_001, "A SOAP envelope, and a valid, unambiguous set of elements to sign (body, timestamp, or additional element ids that resolve to exactly one element), are required." },
                { MessageCodesType.V_SEC_002, "A signing certificate with an accessible RSA private key is required when message security is enabled." },
                { MessageCodesType.V_SEC_003, "Signing a SOAP message is only supported for HTTP POST requests." },
                { MessageCodesType.V_SEC_004, "An expected certificate with an accessible RSA public key is required to verify a WS-Security signature." },
                { MessageCodesType.V_SEC_005, "The SOAP response to verify is empty or exceeds the supported document size of {0} characters." },
                { MessageCodesType.V_SEC_006, "The SOAP response must carry exactly one WS-Security signature as a child of its single 'wsse:Security' header; none, or more than one, was found there." },
                { MessageCodesType.V_SEC_007, "A signature reference must target a plain same-document fragment ('#id'); external, empty, multi-fragment ('##id') and XPointer ('#xpointer(...)') references are not allowed." },
                { MessageCodesType.V_SEC_008, "The SOAP response signature uses a disallowed or unrecognized algorithm, transform, or key reference." },
                { MessageCodesType.V_SEC_009, "The SOAP Body is not covered by the signature; the signature is valid over other content, so the body cannot be trusted." },
                { MessageCodesType.V_SEC_010, "The SOAP response carries no unambiguous 'wsu:Timestamp' covered by the signature, so its freshness cannot be established." },
                { MessageCodesType.V_SEC_011, "The signed 'wsu:Timestamp' does not carry a 'wsu:Created' and 'wsu:Expires' pair readable as an unambiguous UTC ISO-8601 instant." },
                { MessageCodesType.V_SEC_012, "The signed 'wsu:Timestamp' is expired, is not yet valid, or carries an inverted validity window, allowing for the configured clock skew." },
                { MessageCodesType.V_SEC_013, "WS-Security options carrying the certificate the response is expected to be signed with are required to verify a SOAP response signature." },
                { MessageCodesType.V_SEC_014, "The additional signed element id '{0}' resolves to an element that contains the WS-Security header the signature is placed in; the digest would be taken before the signature is inserted into the very subtree it covers, so no receiver could verify the message." },
                { MessageCodesType.V_SEC_015, "The expected response certificate carries the same public key as the signing certificate; a response signed with the client's own key cannot be told apart from a replay of the client's own request, so a distinct service certificate is required." },
                { MessageCodesType.V_SEC_016, "The additional signed element id '{0}' is also carried by another element of the envelope in an unqualified 'Id', 'id', 'ID' or 'AssertionID' attribute, which a signature reference resolves before the 'wsu:Id'; the digest would cover that other element in place of the one named, so the request is refused rather than signed over the wrong element." },
                { MessageCodesType.V_SEC_017, "The additional signed element id '{0}' names the SOAP Body, whose 'wsu:Id' the build replaces with a fresh one before the Body is signed; the Body is covered through 'SignBody', so remove it from 'AdditionalSignedElementIds'." },

                #region VALIDATION CODES - WS-ADDRESSING (V-SEC-020..029)

                { MessageCodesType.V_SEC_020, "The request action and the WS-Addressing action are one value; both were supplied and they differ, so the message would carry a transport action that contradicts its addressing action. Supply one, or the same value for both." },
                { MessageCodesType.V_SEC_021, "WS-Addressing is enabled but no action is available; 'wsa:Action' is mandatory, so supply the request action or 'Addressing.Action'." },
                { MessageCodesType.V_SEC_022, "WS-Addressing is enabled but no absolute destination is available for 'wsa:To'; supply 'Addressing.To' or build the request for an absolute endpoint." },
                { MessageCodesType.V_SEC_023, "The SOAP response does not carry a signed 'wsa:RelatesTo' equal to the 'wsa:MessageID' of the request it is claimed to answer, so it cannot be bound to that request." },
                { MessageCodesType.V_SEC_024, "The SOAP response cannot be bound to the request it is checked against: the request carried no 'wsa:MessageID' and required no 'SignatureConfirmation', so any response the service signed inside the timestamp window would be accepted as its answer. Enable 'Addressing' or 'ResponseSecurity.RequireSignatureConfirmation', or set 'ResponseSecurity.AllowUnboundResponse' to accept an unbound response deliberately." },

                #endregion

                #region VALIDATION CODES - SYMMETRIC BINDING (V-SEC-030..049)

                { MessageCodesType.V_SEC_030, "The sent request carries no WS-Security key material; a response can only be checked against a request that was built by this library with message security enabled and its built-in signer." },
                { MessageCodesType.V_SEC_031, "The WS-Security key material of the sent request has already been consumed by an earlier verification or decryption; every request is checked at most once, so rebuild and resend the request." },
                { MessageCodesType.V_SEC_032, "A response to a symmetric-bound or secure-conversation request cannot be verified from the response and a certificate alone; the derived keys live with the sent request, so use ISoapResponseSecurity.Verify with the HttpRequestMessage that was sent." },
                { MessageCodesType.V_SEC_033, "A symmetric binding or a secure conversation requires WS-Addressing; supply 'Addressing', because the service binds the reply to the request through the signed addressing headers." },
                { MessageCodesType.V_SEC_034, "The symmetric binding is not available in this version; the options are valid, but no key exchange can be emitted yet." },
                { MessageCodesType.V_SEC_035, "A custom 'Signer' or 'ResponseVerifier' cannot be used with a symmetric binding or a secure conversation; ISoapMessageVerifier verifies against a certificate and cannot receive a derived key." },
                { MessageCodesType.V_SEC_036, "'ExpectedResponseCertificate' is ambiguous on a symmetric binding or a secure conversation, where the response is signed with a derived key rather than a certificate; remove it." },
                { MessageCodesType.V_SEC_037, "The symmetric binding's service certificate carries the same public key as the signing certificate; a secret wrapped for the client's own key would let the client answer itself, so a distinct service certificate is required." },
                { MessageCodesType.V_SEC_038, "A username token on a symmetric binding or a secure conversation is always encrypted; 'Encryption.EncryptUsernameToken' cannot be false there." },
                { MessageCodesType.V_SEC_039, "A symmetric binding requires 'SymmetricBinding.ServiceCertificate' with an RSA public key to wrap the secret for." },
                { MessageCodesType.V_SEC_040, "The SOAP response does not carry a signed WS-Security 1.1 'SignatureConfirmation' equal to the signature value of the sent request, so it cannot be bound to that request." },
                { MessageCodesType.V_SEC_041, "The SOAP response reflects a nonce the request itself generated; a reply that reuses the request's own nonce is a replay or a reflection, not an answer." },
                { MessageCodesType.V_SEC_042, "The SOAP response carries the very signature value of the sent request; a reply signed with the request's own signature is a reflection of the request, not an answer." },
                { MessageCodesType.V_SEC_043, "The symmetric binding's derived key lengths are out of range; the signature key must be between 16 and 64 bytes and the encryption key must be 16, 24 or 32 bytes." },
                { MessageCodesType.V_SEC_044, "The symmetric binding signs with HMAC-SHA1 only when 'ResponseVerificationPolicy.AllowSha1Algorithms' opts in; 'SymmetricBinding.SignatureAlgorithm' selects HMAC-SHA1 without that opt-in, so the request is refused rather than signed with an algorithm its own response check would reject." },
                { MessageCodesType.V_SEC_045, "'SymmetricBinding.ServiceKeyIdentifier' selects a service key reference this version does not emit; only the SHA-1 thumbprint reference is available." },
                { MessageCodesType.V_SEC_046, "The SOAP response does not carry a single readable 'DerivedKeyToken' that the response signature's KeyInfo references, keyed to an 'EncryptedKey' by its SHA-1 and describing a P_SHA1 derivation with a nonce and a key length this library accepts." },
                { MessageCodesType.V_SEC_047, "The SOAP response's 'DerivedKeyToken' is keyed to an 'EncryptedKey' other than the one the request carried; a reply derived from a different secret cannot be the answer to this request." },

                #endregion

                #region VALIDATION CODES - ENCRYPTION AND DECRYPTION (V-SEC-050..059)

                { MessageCodesType.V_SEC_050, "Message encryption and response decryption are not available in this version; the options are valid, but nothing can be encrypted or decrypted yet." },
                { MessageCodesType.V_SEC_051, "'Encryption' requires a symmetric binding or a secure conversation; the asymmetric X.509 binding carries no encryption key." },
                { MessageCodesType.V_SEC_052, "'Encryption.EncryptBody' requires 'ResponseSecurity.AllowDecryption'; a service that receives an encrypted Body answers with one, and a response nobody agreed to decrypt would be unreadable." },
                { MessageCodesType.V_SEC_053, "Decryption, and 'ResponseSecurity.AllowDecryption', require a request built on a symmetric binding or a secure conversation; the asymmetric X.509 binding carries no decryption key, and the client never unwraps a key wrapped for its own certificate." },
                { MessageCodesType.V_SEC_054, "The plaintext limit 'ResponseSecurity.MaxPlaintextBytes' is {0} bytes: it must be greater than zero, and the encrypted parts of a response may carry no more cipher text than it could hold, so such a response is refused before anything is decrypted." },
                { MessageCodesType.V_SEC_055, "The symmetric binding wraps its secret with RSA-OAEP only; 'Encryption.KeyWrap' cannot select RSA PKCS#1 v1.5, which is open to padding-oracle attacks." },
                { MessageCodesType.V_SEC_056, "'SymmetricBinding.EncryptionKeyLength' does not match 'Encryption.DataAlgorithm'; AES-128, AES-192 and AES-256 need a 16, 24 and 32 byte key respectively." },
                { MessageCodesType.V_SEC_057, "'Encryption.EncryptBody' requires a 'SymmetricBinding.ServiceCertificate' fit to encrypt for: an RSA key of at least 2048 bits, a validity window that contains the current instant within the clock skew, a KeyUsage extension that permits key encipherment when one is present, and a public key distinct from the signing certificate's. The request is refused rather than sent with a Body encrypted for a key that cannot be trusted." },
                { MessageCodesType.V_SEC_058, "The SOAP response carries encrypted content and is refused undecrypted: the request was built without 'ResponseSecurity.AllowDecryption', or the response was checked through Verify rather than Decrypt, which is the only operation that decrypts. The cipher text is not surfaced." },
                { MessageCodesType.V_SEC_059, "The encrypted parts of the SOAP response are not in the one shape this library decrypts, so nothing was decrypted: every 'EncryptedData' must be named by the single 'ReferenceList' and resolve unambiguously, sit as the only content of the Body or in the Security header as an element that decrypts to the 'Signature' or a 'SignatureConfirmation', name the request's own data encryption algorithm, and be keyed through a 'DerivedKeyToken' to the request's own 'EncryptedKey'; any other 'EncryptedKey', 'RetrievalMethod', 'CarriedKeyName', 'CipherReference' or certificate reference is refused before any key is touched." },

                #endregion

                #region VALIDATION CODES - SECURE CONVERSATION (V-SEC-060..079)

                { MessageCodesType.V_SEC_060, "Secure conversation is not available in this version; the options are valid, but no security context token can be emitted yet." },
                { MessageCodesType.V_SEC_061, "A secure conversation requires 'SecureConversation.Session', the session issued by the service." },
                { MessageCodesType.V_SEC_062, "The secure conversation session has expired; renew it with the issuer before building the request." },
                { MessageCodesType.V_SEC_063, "The secure conversation session has been disposed or holds no secret, so no key can be derived from it." },
                { MessageCodesType.V_SEC_064, "The 'RequestSecurityTokenResponse' was not signed by the expected service key under the bootstrap's symmetric binding, so the issued session cannot be trusted." },
                { MessageCodesType.V_SEC_065, "The 'RequestSecurityTokenResponse' does not carry a signed 'wsa:RelatesTo' equal to the issue request's MessageID, so it cannot be bound to the request that asked for the session." },
                { MessageCodesType.V_SEC_066, "The 'RequestedProofToken' does not name a P_SHA1 'ComputedKey'; a proof token that carries a 'BinarySecret' the service chose alone, or any other computed-key algorithm, is refused because the session key must be computed from both entropies." },
                { MessageCodesType.V_SEC_067, "The service 'Entropy/BinarySecret' is missing, shorter than 32 bytes or all zero, so no session key can be computed from it." },
                { MessageCodesType.V_SEC_068, "The 'RequestSecurityTokenResponse' echoes a 'KeySize' other than the 256 bits requested, so the issued key does not match the request." },
                { MessageCodesType.V_SEC_069, "The 'RequestedSecurityToken' does not carry a 'SecurityContextToken' with a single non-empty 'Identifier', so there is no context to key later requests by." },
                { MessageCodesType.V_SEC_070, "The 'RequestSecurityTokenResponse' does not carry a readable 'Lifetime/Expires', so the session has no known expiry and is refused." },
                { MessageCodesType.V_SEC_071, "The 'RequestSecurityTokenResponse' timestamp is missing, expired or too far skewed to be fresh, so the issued session is refused." },
                { MessageCodesType.V_SEC_072, "The issue response is not a single 'RequestSecurityTokenResponse' for a security context token in the requested WS-Trust version, so nothing was issued." },
                { MessageCodesType.V_SEC_073, "The secure conversation session was disposed before its response could be verified, so the response key can no longer be derived; the session and the response no longer belong together." },
                { MessageCodesType.V_SEC_074, "The response's 'DerivedKeyToken' does not reference this session's 'SecurityContextToken', so a reply keyed by another context cannot be the answer to this request." },
                { MessageCodesType.V_SEC_075, "The 'DerivedKeyToken' describes a derivation this library does not accept: its 'Length' exceeds 64, both 'Offset' and 'Generation' are present, or a 'Properties' element is present." },

                #endregion

                #region VALIDATION CODES - SAML (V-SEC-080..089)

                { MessageCodesType.V_SEC_080, "SAML tokens are not available in this version; the options are valid, but no assertion can be carried yet." },
                { MessageCodesType.V_SEC_081, "'SamlToken' requires 'SamlToken.Assertion', the issued assertion element." },
                { MessageCodesType.V_SEC_082, "A holder-of-key SAML confirmation requires 'SigningCertificate', because the message must be signed with the key the assertion names." },
                { MessageCodesType.V_SEC_083, "A bearer SAML assertion is only carried to an 'https' endpoint, because whoever reads it off the wire can replay it; set 'SamlToken.AllowBearerOverInsecureTransport' to carry it over an insecure transport deliberately." },
                { MessageCodesType.V_SEC_084, "'SamlToken.Assertion' must be the 'saml:Assertion' element itself, in the SAML 1.1 or the SAML 2.0 assertion namespace; an envelope, a Security header or any other element wrapping the assertion is refused." },
                { MessageCodesType.V_SEC_085, "The SAML assertion carries no identifier: a SAML 2.0 assertion needs a non-blank 'ID' and a SAML 1.1 assertion a non-blank 'AssertionID'." },
                { MessageCodesType.V_SEC_086, "The SAML assertion's identifier also names another element of the built envelope, so a reference to the assertion would be ambiguous; the assertion is not carried." },
                { MessageCodesType.V_SEC_087, "The SAML assertion has already lapsed ('NotOnOrAfter' is at or before now, allowing for 'SamlToken.ClockSkew'), or its validity instant cannot be read; every receiver would reject it, so it is not carried." },
                { MessageCodesType.V_SEC_088, "A SAML assertion cannot be carried on the symmetric binding or in a secure conversation in this version; carry it with a signing certificate, or on its own." },

                #endregion

                #region VALIDATION CODES - SECURITY PLANNER (V-SEC-090..099)

                { MessageCodesType.V_SEC_090, "'SymmetricBinding' and 'SecureConversation' select two different modes and cannot both be set; choose one." },
                { MessageCodesType.V_SEC_091, "'UsernameToken.SignToken' requires a signature, and a header carrying only tokens has none; supply a signing certificate or a symmetric binding, or do not ask for the token to be signed." },
                { MessageCodesType.V_SEC_092, "A username token requires 'UsernameToken.Username', and a digest token also requires 'UsernameToken.Password'." },
                { MessageCodesType.V_SEC_093, "The username token carries a character XML cannot represent; the value is not repeated here." },
                { MessageCodesType.V_SEC_094, "A username token carrying a text password is only sent to an 'https' endpoint when the token is not encrypted, because whoever reads it off the wire holds the password; use a digest password, a symmetric binding that encrypts the token, or set 'UsernameToken.AllowTextPasswordOverInsecureTransport' to carry it over an insecure transport deliberately." },

                #endregion
            };

        /// <summary>
        ///     Reads the error message of a code, without throwing when the code has no text.
        /// </summary>
        /// <param name="code">The message code.</param>
        /// <returns>
        ///     The message text, or a placeholder naming the unmapped code.
        /// </returns>
        internal static string GetErrorMessage(MessageCodesType code)
            => ErrorMessages.TryGetValue(code, out var message) ? message : UnmappedMessage(code);

        /// <summary>
        ///     Gets the validation message of the supplied code without throwing on an unmapped code.
        /// </summary>
        /// <param name="code">The message code.</param>
        /// <returns>
        ///     The message text, or a placeholder naming the unmapped code.
        /// </returns>
        internal static string GetValidationMessage(MessageCodesType code)
            => ValidationMessages.TryGetValue(code, out var message) ? message : UnmappedMessage(code);

        /// <summary>
        ///     Builds the placeholder returned for a code that has no message text.
        /// </summary>
        /// <param name="code">The unmapped message code.</param>
        /// <returns>
        ///     The placeholder message.
        /// </returns>
        private static string UnmappedMessage(MessageCodesType code)
            => $"No message is defined for code '{code}'.";
    }
}