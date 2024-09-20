> **Note** This repository is developed using .netstandard2.0.

| Name     | Details |
|----------|----------|
| SoapClientCallAssist | [![NuGet Version](https://img.shields.io/nuget/v/SoapClientCallAssist.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/SoapClientCallAssist/) [![Nuget Downloads](https://img.shields.io/nuget/dt/SoapClientCallAssist.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/SoapClientCallAssist)|

This repository provides a simple solution to call/invoke `SOAP` (`WCF`, `ASMX`) service using only basic definitions.

Through basic definition is meaning: `Action`, `XML`, `HTTP method` and others.

If you ever worked with `SOAP` services, you know that every service needed to be called must be added as a service reference and with a few configurations can be invoked in dependence on the specifications. 
But whenever the source service is changed/added or deleted you will get an exception if changes affect something you can see (methods definition, result, etc).

A single solution, in this case, is to update the service reference(configuration) from the source `URL` or update the configuration from `WSDL`, but in some cases, it may be tough, impossible, complicated, or too may take much time to obtain WSDL or access to the resource.

So the basic idea is to avoid this dependency on the `WSDL` service definition and build requests with minimum required info. This repository is created to improve your experience in a positive direction and make your work easy with productive time spent.

To understand more efficiently how you can use available functionalities please consult the [using documentation/file](docs/usage.md).

**In case you wish to use it in your project, u can install the package from <a href="https://www.nuget.org/packages/SoapClientCallAssist" target="_blank">nuget.org</a>** or specify what version you want:

> `Install-Package SoapClientCallAssist -Version x.x.x.x`

## Content
1. [USING](docs/usage.md)
1. [CHANGELOG](docs/CHANGELOG.md)
1. [BRANCH-GUIDE](docs/branch-guide.md)