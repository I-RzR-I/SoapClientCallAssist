// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-12 00:40
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-12 00:40
//  ***********************************************************************
//  <copyright file="DecryptionStages.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using RzR.Extensions.Domain.Primitives;

#endregion

namespace SoapClientCallAssist.Security
{
    /// <summary>
    ///     Records which stage of a decryption failed first. Stage names are fixed words and reach a
    ///     caller only through the opt-in
    ///     <see cref="Dto.Public.SoapVerificationPolicyDto.DiagnosticDetail" />.
    /// </summary>
    internal sealed class DecryptionStages
    {
        /// <summary>
        ///     The stage that derives the keys and refuses a reflected one.
        /// </summary>
        internal const string KeyDerivation = "key derivation";

        /// <summary>
        ///     The stage that decrypts the cipher text.
        /// </summary>
        internal const string CipherText = "cipher text";

        /// <summary>
        ///     The stage that enforces the plaintext cap.
        /// </summary>
        internal const string PlaintextSize = "plaintext size";

        /// <summary>
        ///     The stage that decodes the plaintext as UTF-8.
        /// </summary>
        internal const string PlaintextEncoding = "plaintext encoding";

        /// <summary>
        ///     The stage that parses the plaintext through the hardened reader.
        /// </summary>
        internal const string PlaintextParse = "plaintext parse";

        /// <summary>
        ///     The stage that gates what the plaintext may carry.
        /// </summary>
        internal const string PlaintextContent = "plaintext content";

        /// <summary>
        ///     The stage that verifies the signature over the decrypted document.
        /// </summary>
        internal const string Signature = "signature";

        /// <summary>
        ///     The name of the first stage that failed, or null while no stage has failed.
        /// </summary>
        internal string Stage { get; private set; }

        /// <summary>
        ///     True once a stage has failed.
        /// </summary>
        internal bool Failed => Stage.IsNotNull();

        /// <summary>
        ///     Notes the outcome of a stage, keeping the first failure and ignoring every later one.
        /// </summary>
        /// <param name="stage">The stage name.</param>
        /// <param name="failed">True when the stage failed.</param>
        internal void Record(string stage, bool failed)
        {
            if (failed && Stage.IsNull())
                Stage = stage;
        }

        /// <summary>
        ///     Renders the sentence appended to the failure message when the caller opted in to the
        ///     diagnostic detail.
        /// </summary>
        /// <param name="stage">The stage name.</param>
        /// <returns>
        ///     The sentence naming the stage.
        /// </returns>
        internal static string Describe(string stage)
            => "Diagnostic detail (not for production): the first stage that failed was " + stage + ".";
    }
}
