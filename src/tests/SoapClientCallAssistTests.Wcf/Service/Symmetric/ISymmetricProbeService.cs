using SoapClientCallAssistTests.Wcf.Service.Common;
using System.Net.Security;
using System.ServiceModel;

namespace SoapClientCallAssistTests.Wcf.Service.Symmetric;

[ServiceContract(Namespace = SymmetricProbeContract.Namespace, ProtectionLevel = ProtectionLevel.Sign)]
public interface ISymmetricProbeService
{

    [OperationContract]
    string Echo(string value);

    [OperationContract]
    WcfCallerIdentity WhoAmI();
}
