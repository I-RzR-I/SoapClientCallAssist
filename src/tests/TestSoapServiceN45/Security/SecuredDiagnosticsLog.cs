using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace TestSoapServiceN45.Security
{
    public static class SecuredDiagnosticsLog
    {
        public const string RelativeDirectory = "SoapClientCallAssist";

        public const string FileName = "ServiceSecured.diagnostics.log";

        private static readonly object Gate = new object();

        public static string Path
        {
            get
            {
                return System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), RelativeDirectory, FileName);
            }
        }

        public static void Write(string source, string message)
        {
            try
            {
                var path = Path;
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));

                var entry = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) + " [" + source + "] " + message + Environment.NewLine;

                lock (Gate)
                    File.AppendAllText(path, entry, Encoding.UTF8);
            }
            catch (Exception)
            {
            }
        }
    }
}
