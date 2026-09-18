using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Helpers.Mapper;

internal static class MapperEmitNs
{
    internal const string Operation = "http://SoapClientCallAssist.local/";

    internal const string Product = "http://schemas.datacontract.org/2004/07/SoapTestService";

    internal const string Explicit = "http://contracts.example/explicit-data-contract";

    internal const string DataContractDefaultPrefix = "http://schemas.datacontract.org/2004/07/";

    internal static readonly XNamespace OperationNamespace = Operation;

    internal static readonly XNamespace ProductNamespace = Product;

    internal static readonly XNamespace ExplicitNamespace = Explicit;
}
