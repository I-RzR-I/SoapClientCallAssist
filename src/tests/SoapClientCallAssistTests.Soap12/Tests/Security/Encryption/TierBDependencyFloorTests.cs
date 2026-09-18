#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.Encryption;

[TestClass]
public sealed class TierBDependencyFloorTests
{

    private const string PackageId = "System.Security.Cryptography.Xml";

    private static readonly Version Floor = new(10, 0, 11);

    [TestMethod]
    public void LibraryProject_PinsSystemSecurityCryptographyXmlAtOrAboveTheFloor_Test()
    {
        var project = LocateLibraryProject();
        var reference = XDocument.Load(project).Descendants("PackageReference").SingleOrDefault(element => (string)element.Attribute("Include") == PackageId);

        Assert.IsNotNull(reference, $"{project} | {PackageId}");

        var declared = Version.Parse((string)reference.Attribute("Version"));

        Assert.IsTrue(declared >= Floor, $"{PackageId} | {declared} | {Floor}");
    }

    private static string LocateLibraryProject()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "src", "SoapClientCallAssist", "SoapClientCallAssist.csproj");

            if (File.Exists(candidate))
                return candidate;
        }

        throw new AssertFailedException("library project");
    }
}
