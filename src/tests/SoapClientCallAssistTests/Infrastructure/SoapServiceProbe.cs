#region U S I N G

using System;
using System.Net;

#endregion

namespace SoapClientCallAssistTests.Infrastructure
{
    public class SoapServiceProbe
    {
        public SoapServiceProbe(Uri uri, string expectedBodyMarker, HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            if (!uri.IsAbsoluteUri)
                throw new ArgumentException("The probe endpoint must be an absolute URI.", nameof(uri));

            Uri = uri;
            ExpectedBodyMarker = string.IsNullOrEmpty(expectedBodyMarker) ? null : expectedBodyMarker;
            ExpectedStatusCode = expectedStatusCode;
        }

        public Uri Uri { get; }

        public string ExpectedBodyMarker { get; }

        public HttpStatusCode ExpectedStatusCode { get; }

        public bool VerifiesIdentity => ExpectedBodyMarker != null;

        public override string ToString()
        {
            return Uri.ToString();
        }
    }
}
