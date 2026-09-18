// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 21:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-11 21:10
//  ***********************************************************************
//  <copyright file="X509KeyMatch.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using RzR.Extensions.Domain.Primitives;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

#endregion

namespace SoapClientCallAssist.Security
{
    /// <summary>
    ///     Compares certificates by the public key they carry.
    /// </summary>
    internal static class X509KeyMatch
    {
        /// <summary>
        ///     Determines whether two certificates carry the same public key. A certificate that cannot
        ///     expose its key counts as sharing nothing.
        /// </summary>
        /// <param name="first">The first certificate, or null.</param>
        /// <param name="second">The second certificate, or null.</param>
        /// <returns>
        ///     True when both are present and carry the same key algorithm and public key bytes.
        /// </returns>
        internal static bool SharesPublicKey(X509Certificate2 first, X509Certificate2 second)
        {
            if (first.IsNull() || second.IsNull())
                return false;

            try
            {
                return string.Equals(first!.GetKeyAlgorithm(), second!.GetKeyAlgorithm(), StringComparison.Ordinal)
                       && first.GetPublicKey().SequenceEqual(second.GetPublicKey());
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
