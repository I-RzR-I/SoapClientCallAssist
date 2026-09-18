using SoapClientCallAssistTests.Wcf.Service.Common;
using System.ServiceModel;

namespace SoapClientCallAssistTests.Wcf.Service.Plain;

[ServiceContract(Namespace = WcfProbeContract.Namespace)]
public interface IWcfProbeService
{

    [OperationContract]
    string Echo(string value);

    [OperationContract]
    WcfCallerIdentity WhoAmI();
}
