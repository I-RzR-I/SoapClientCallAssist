// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-03 00:09
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-03 19:33
//  ***********************************************************************
//  <copyright file="SoapCanonicalizationType.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using System.ComponentModel;
using System.Security.Cryptography.Xml;

// ReSharper disable InconsistentNaming

#endregion

namespace SoapClientCallAssist.Enums
{
    /// <summary>
    ///     The XML canonicalization methods supported when signing or verifying a WS-Security
    ///     message.
    /// </summary>
    public enum SoapCanonicalizationType
    {
        /// <summary>
        ///     Exclusive XML canonicalization (<c>xml-exc-c14n#</c>). The default canonicalization
        ///     method.
        /// </summary>
        [Description(SignedXml.XmlDsigExcC14NTransformUrl)]
        ExclusiveC14N = 0,

        /// <summary>
        ///     Inclusive XML canonicalization (<c>REC-xml-c14n-20010315</c>).
        /// </summary>
        [Description(SignedXml.XmlDsigC14NTransformUrl)] 
        InclusiveC14N = 1
    }
}