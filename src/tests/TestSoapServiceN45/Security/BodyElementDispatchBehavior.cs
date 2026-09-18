using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace TestSoapServiceN45.Security
{
    public sealed class BodyElementDispatchBehavior : IEndpointBehavior
    {
        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            endpointDispatcher.ContractFilter = new MatchAllMessageFilter();
            endpointDispatcher.DispatchRuntime.OperationSelector = new BodyElementOperationSelector();
        }

        public void Validate(ServiceEndpoint endpoint)
        {
        }
    }
}
