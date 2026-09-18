#region U S I N G

using System;

#endregion

namespace SoapClientCallAssistTests.Infrastructure
{
    public class TestServiceUnavailableException : Exception
    {
        public TestServiceUnavailableException(string message) : base(message)
        {
        }
    }
}
