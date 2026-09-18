using System.ServiceModel;

namespace SoapClientCallAssistTests.Wcf.Service.Saml;

[ServiceContract(Namespace = SamlProbeContract.Namespace)]
public interface ISamlProbeService
{

    [OperationContract]
    SamlCallerClaims WhoAmI();
}
