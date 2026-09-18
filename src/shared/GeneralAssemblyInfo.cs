// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2024-04-19 17:42
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-08-31 20:42
//  ***********************************************************************
//  <copyright file="GeneralAssemblyInfo.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using System.Reflection;
using System.Resources;

#endregion

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: AssemblyCompany("RzR ®")]
[assembly: AssemblyProduct("SoapClientCallAssist")]
[assembly: AssemblyCopyright("Copyright © 2022-2026 RzR All rights reserved.")]
[assembly: AssemblyTrademark("® RzR™")]
[assembly:
    AssemblyDescription(
        "Provides a more easy way to implement and invoke SOAP(1.1, 1.2) service(`WCF`, `ASMX`) endpoints avoiding dependency on the `WSDL` definition and build requests with minimum required info (basic definition: `Action`, `XML`, `HTTP method`, etc).")]

[assembly: AssemblyMetadata("TermsOfService", "")]
[assembly: AssemblyMetadata("ContactUrl", "")]
[assembly: AssemblyMetadata("ContactName", "RzR")]
[assembly: AssemblyMetadata("ContactEmail", "ddpRzR@hotmail.com")]
[assembly: NeutralResourcesLanguage("en-US", UltimateResourceFallbackLocation.MainAssembly)]
[assembly: AssemblyVersion("2.0.0.380")]
[assembly: AssemblyFileVersion("2.0.0.380")]
[assembly: AssemblyInformationalVersion("2.0.0.380")]
