using System;
using System.IO;

namespace SoapClientCallAssistTests.Wcf.Helpers;

internal static class CngKeyStoreLitter
{

    internal static string KeyDirectory
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Crypto", "Keys");

    internal static int CountKeyFiles()
        => Directory.Exists(KeyDirectory) ? Directory.GetFiles(KeyDirectory).Length : 0;
}
