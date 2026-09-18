using System.ServiceModel;
using TestSoapServiceN45.Dto;

namespace TestSoapServiceN45
{
    [ServiceContract(Namespace = "http://SoapClientCallAssist.local/")]
    public interface IServiceSecured
    {
        [OperationContract]
        string Echo(string value);

        [OperationContract]
        CallerIdentity WhoAmI();
    }
}
