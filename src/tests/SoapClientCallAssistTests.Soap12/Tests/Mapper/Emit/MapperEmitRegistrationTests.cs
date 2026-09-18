using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Mapping;

namespace SoapClientCallAssistTests.Soap12.Tests.Mapper.Emit;

[TestClass]
public class MapperEmitRegistrationTests
{
    [TestMethod]
    public void RegisterSoapClientsEndpoint_ResolvesTheMapperFromTheContainer_Test()
    {
        var services = new ServiceCollection();

        services.RegisterSoapClientsEndpoint();

        using var provider = services.BuildServiceProvider();

        var mapper = provider.GetRequiredService<ISoapModelMapper>();

        Assert.IsNotNull(mapper);
        Assert.IsInstanceOfType<SoapModelMapper>(mapper);
    }

    [TestMethod]
    public void RegisterSoapClientsEndpoint_ResolvesTheSameMapperInstanceEveryTime_Test()
    {
        var services = new ServiceCollection();

        services.RegisterSoapClientsEndpoint();

        using var provider = services.BuildServiceProvider();

        var first = provider.GetRequiredService<ISoapModelMapper>();
        var second = provider.GetRequiredService<ISoapModelMapper>();

        Assert.AreSame(first, second);
    }

    [TestMethod]
    public void RegisterSoapClientsEndpoint_ResolvesTheSameMapperInstanceAcrossScopes_Test()
    {
        var services = new ServiceCollection();

        services.RegisterSoapClientsEndpoint();

        using var provider = services.BuildServiceProvider();

        ISoapModelMapper first;
        ISoapModelMapper second;

        using (var scope = provider.CreateScope())
            first = scope.ServiceProvider.GetRequiredService<ISoapModelMapper>();

        using (var scope = provider.CreateScope())
            second = scope.ServiceProvider.GetRequiredService<ISoapModelMapper>();

        Assert.AreSame(first, second);
    }
}
