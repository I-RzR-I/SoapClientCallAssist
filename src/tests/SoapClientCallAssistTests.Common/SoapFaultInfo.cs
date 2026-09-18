namespace SoapClientCallAssistTests.Common;

public sealed class SoapFaultInfo
{
    public SoapFaultInfo(string namespaceName, string localName, string reason)
    {
        NamespaceName = namespaceName;
        LocalName = localName;
        Reason = reason;
    }

    public string NamespaceName { get; }

    public string LocalName { get; }

    public string Reason { get; }

    public override string ToString() => $"{{{NamespaceName}}}{LocalName}: {Reason}";
}