using SoapClientCallAssistTests.Wcf.Service.Common;
using System.ServiceModel;

namespace SoapClientCallAssistTests.Wcf.Service.TierB;

[ServiceContract(Namespace = TierBProbeContract.Namespace)]
public interface ITierBProbeService
{

    [OperationContract]
    string Echo(string value);

    [OperationContract]
    WcfCallerIdentity WhoAmI();
}
