using System;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Security;

namespace TestSoapServiceN45.Security
{
    public sealed class SecuredServiceHostFactory : ServiceHostFactory
    {
        protected override ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
        {
            var host = new ServiceHost(serviceType, baseAddresses);

            var certificates = host.Credentials.ClientCertificate.Authentication;
            certificates.CertificateValidationMode = X509CertificateValidationMode.Custom;
            certificates.RevocationMode = System.Security.Cryptography.X509Certificates.X509RevocationMode.NoCheck;
            certificates.CustomCertificateValidator = new ThumbprintCertificateValidator();

            var userNames = host.Credentials.UserNameAuthentication;
            userNames.UserNamePasswordValidationMode = UserNamePasswordValidationMode.Custom;
            userNames.CustomUserNamePasswordValidator = new InMemoryUserNamePasswordValidator();

            AddEndpoints(host, MessageVersion.Soap11, SecuredBindings.Soap11Segment, false);
            AddEndpoints(host, MessageVersion.Soap12, SecuredBindings.Soap12Segment, true);

            var debug = host.Description.Behaviors.Find<ServiceDebugBehavior>();
            if (debug == null)
                host.Description.Behaviors.Add(new ServiceDebugBehavior { IncludeExceptionDetailInFaults = true });
            else
                debug.IncludeExceptionDetailInFaults = true;

            SecuredDiagnosticsLog.Write(
                nameof(SecuredServiceHostFactory),
                "Host built with " + host.Description.Endpoints.Count + " endpoints under " + string.Join(", ", Array.ConvertAll(baseAddresses, address => address.ToString())));

            return host;
        }

        private static void AddEndpoints(ServiceHost host, MessageVersion version, string versionSegment, bool dispatchByBody)
        {
            AddEndpoint(host, SecuredBindings.UserNameOverTransport(version), SecuredBindings.UserNameSegment, versionSegment, dispatchByBody);
            AddEndpoint(host, SecuredBindings.CertificateOverTransport(version), SecuredBindings.CertificateSegment, versionSegment, dispatchByBody);
            AddEndpoint(host, SecuredBindings.UserNameWithEndorsingCertificate(version), SecuredBindings.BothSegment, versionSegment, dispatchByBody);
        }

        private static void AddEndpoint(ServiceHost host, Binding binding, string modeSegment, string versionSegment, bool dispatchByBody)
        {
            var endpoint = host.AddServiceEndpoint(typeof(IServiceSecured), binding, modeSegment + "/" + versionSegment);

            if (dispatchByBody)
                endpoint.EndpointBehaviors.Add(new BodyElementDispatchBehavior());
        }
    }
}
