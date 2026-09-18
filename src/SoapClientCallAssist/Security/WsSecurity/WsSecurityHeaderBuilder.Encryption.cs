// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 22:40
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-12 02:30
//  ***********************************************************************
//  <copyright file="WsSecurityHeaderBuilder.Encryption.cs" company="RzR SOFT & TECH">
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
using SoapClientCallAssist.Enums;
using SoapClientCallAssist.Helpers;
using System;
using System.Collections.Generic;
using System.Xml;

#endregion

namespace SoapClientCallAssist.Security.WsSecurity
{
    /// <summary>
    ///     The encryption concern of the header builder. After signing it encrypts the username
    ///     token, the Body's content and optionally the primary signature in place, announced
    ///     beforehand in a reference list.
    /// </summary>
    internal sealed partial class WsSecurityHeaderBuilder
    {
        /// <summary>
        ///     The plain <c>Id</c> reserved for the username token's <c>EncryptedData</c>, or null.
        /// </summary>
        private string _usernameTokenEncryptedDataId;

        /// <summary>
        ///     The plain <c>Id</c> reserved for the Body content's <c>EncryptedData</c>, or null.
        /// </summary>
        private string _bodyEncryptedDataId;

        /// <summary>
        ///     The plain <c>Id</c> reserved for the signature's <c>EncryptedData</c>, or null.
        /// </summary>
        private string _signatureEncryptedDataId;

        /// <summary>
        ///     The plaintext username token awaiting encryption, or null.
        /// </summary>
        private XmlElement _usernameTokenToEncrypt;

        /// <summary>
        ///     Emits the <c>xenc:ReferenceList</c> naming every <c>EncryptedData</c> the build will
        ///     produce, reserving their ids, or nothing when there is nothing to encrypt. Encrypting
        ///     without an encryption token fails.
        /// </summary>
        partial void EmitReferenceList()
        {
            if (_symmetricKeySource.IsNull() || _symmetricKeySource!.EncryptionTokenId.IsNull())
            {
                _hookOutcome = EncryptsAnything() ? SymmetricRefusal(MessageCodesType.V_SEC_050) : Result.Success();

                return;
            }

            var ids = new List<string>();

            if (_plan.EncryptUsernameToken)
            {
                _usernameTokenEncryptedDataId = _ids.Next("ed");
                ids.Add(_usernameTokenEncryptedDataId);
            }

            if (Options.Encryption.IsNotNull() && Options.Encryption!.EncryptBody)
            {
                _bodyEncryptedDataId = _ids.Next("ed");
                ids.Add(_bodyEncryptedDataId);
            }

            if (Options.Encryption.IsNotNull() && Options.Encryption!.EncryptSignature)
            {
                _signatureEncryptedDataId = _ids.Next("ed");
                ids.Add(_signatureEncryptedDataId);
            }

            if (ids.Count == 0)
            {
                _hookOutcome = Result.Success();

                return;
            }

            var referenceList = _document.CreateElement(XmlEncryptionNames.Prefix, 
                XmlEncryptionNames.ReferenceListLocalName, XmlEncryptionNames.Namespace);

            foreach (var id in ids)
            {
                var reference = _document.CreateElement(XmlEncryptionNames.Prefix, 
                    XmlEncryptionNames.DataReferenceLocalName, XmlEncryptionNames.Namespace);
                reference.SetAttribute(WsSecurityNames.UriAttributeName, "#" + id);
                referenceList.AppendChild(reference);
            }

            _security.AppendChild(referenceList);

            _hookOutcome = Result.Success();
        }

        /// <summary>
        ///     Emits the username token in clear where its <c>EncryptedData</c> will go and records it
        ///     for the primary signature, whether or not it asked to be signed;
        ///     <see cref="AfterSignatures" /> encrypts it.
        /// </summary>
        partial void EmitEncryptedUsernameToken()
        {
            if (_usernameTokenEncryptedDataId.IsNull())
            {
                _hookOutcome = SymmetricRefusal(MessageCodesType.V_SEC_050);

                return;
            }

            EmitUsernameToken();

            _usernameTokenToEncrypt = _security.LastChild as XmlElement;

            var tokenId = _usernameTokenToEncrypt?.GetAttribute(WsSecurityNames.IdLocalName, WsSecurityNames.WsuNamespace);
            if (tokenId.IsMissing())
            {
                _hookOutcome = InsufficientSigningInput();

                return;
            }

            if (_referenceIds.Contains(tokenId).IsFalse())
                _referenceIds.Add(tokenId);

            _hookOutcome = Result.Success();
        }

        /// <summary>
        ///     Encrypts the username token, the Body's content and, when asked, the primary signature
        ///     in place under the encryption derived key once every signature exists, then zeroes the
        ///     key.
        /// </summary>
        partial void AfterSignatures()
        {
            if (_usernameTokenToEncrypt.IsNull() && _bodyEncryptedDataId.IsNull() && _signatureEncryptedDataId.IsNull())
                return;

            var key = _symmetricKeySource?.DeriveEncryptionKey();
            if (key.IsNull())
            {
                _hookOutcome = SymmetricRefusal(MessageCodesType.V_SEC_050);

                return;
            }

            try
            {
                var algorithm = (Options.Encryption?.DataAlgorithm ?? SoapDataEncryptionAlgorithmType.Aes256Cbc).GetDescription();
                var keyInfoClause = SecurityTokenReferenceClause.Reference(
                    "#" + _symmetricKeySource!.EncryptionTokenId, WsSecureConversationNames.DerivedKeyTokenValueType(_symmetricKeySource.Version));

                if (_usernameTokenToEncrypt.IsNotNull())
                {
                    var encryptedToken = WsSecurityEncryptor.EncryptElementInPlace(
                        _document, _usernameTokenToEncrypt, key, algorithm, keyInfoClause, _usernameTokenEncryptedDataId);
                    if (encryptedToken.IsSuccess.IsFalse())
                    {
                        _hookOutcome = encryptedToken.ToBase();

                        return;
                    }

                    _usernameTokenToEncrypt = null;
                }

                if (_bodyEncryptedDataId.IsNotNull())
                {
                    var encryptedBody = WsSecurityEncryptor.EncryptContentInPlace(
                        _document, LocateBody(), key, algorithm, keyInfoClause, _bodyEncryptedDataId);
                    if (encryptedBody.IsSuccess.IsFalse())
                    {
                        _hookOutcome = encryptedBody.ToBase();

                        return;
                    }
                }

                if (_signatureEncryptedDataId.IsNotNull())
                {
                    if (_primarySignature.IsNull())
                    {
                        _hookOutcome = InsufficientSigningInput();

                        return;
                    }

                    var encryptedSignature = WsSecurityEncryptor.EncryptElementInPlace(
                        _document, _primarySignature!.Element, key, algorithm, keyInfoClause, _signatureEncryptedDataId);
                    if (encryptedSignature.IsSuccess.IsFalse())
                    {
                        _hookOutcome = encryptedSignature.ToBase();

                        return;
                    }
                }

                _hookOutcome = Result.Success();
            }
            finally
            {
                Array.Clear(key, 0, key.Length);
            }
        }
    }
}
