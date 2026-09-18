using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Web.Script.Services;
using System.Web.Services;
using TestSoapServiceN45.Dto;

namespace TestSoapServiceN45
{
    [ServiceContract(Namespace = "http://SoapClientCallAssist.local/")]
    public interface IServiceSvc
    {
        [OperationContract]
        bool DoWork();

        //[WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped,
        //    RequestFormat = WebMessageFormat.Json)]
        [OperationContract]
        string HelloWorld();

        [OperationContract]
        int IsValid(string id);

        //[WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped,
        //    RequestFormat = WebMessageFormat.Json)]
        [OperationContract]
        bool AddRecordWithDetail(Product product);

        //[WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped,
        //    RequestFormat = WebMessageFormat.Json)]
        [OperationContract]
        bool AddRecordWithDetailWithLocations(Product product, List<int> associatedLocationIds);
    }
}
