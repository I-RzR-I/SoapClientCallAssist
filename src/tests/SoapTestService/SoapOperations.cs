using System.Xml.Linq;

namespace SoapTestService;

public static class SoapOperations
{
    public const int MaxDelayMilliseconds = 30000;

    public static async Task<SoapResult> InvokeAsync(string operation, SoapArguments arguments,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        switch (operation)
        {
            case "HelloWorld":
                return HelloWorld(operation);

            case "EchoValue":
                return EchoValue(operation, arguments);

            case "AddRecordWithDetail":
                return AddRecordWithDetail(operation, arguments);

            case "GetProduct":
                return GetProduct(operation, arguments);

            case "IsValid":
                return IsValid(operation, arguments);

            case "AddRecordWithDetailWithLocations":
                return AddRecordWithDetailWithLocations(operation, arguments);

            case "ThrowFault":
                return ThrowFault();

            case "ThrowEmptyFault":
                return ThrowEmptyFault();

            case "NotFound500":
                return NotFound500();

            case "SlowOp":
                return await SlowOpAsync(operation, arguments, cancellationToken);

            default:
                return SoapResult.Fault(SoapFaultCode.Receiver, "The requested operation is not supported.");
        }
    }

    private static SoapResult HelloWorld(string operation)
        => Respond(operation, new XElement(SoapNames.Service + "HelloWorldResult", "Hello World"));

    private static SoapResult EchoValue(string operation, SoapArguments arguments)
    {
        var value = arguments.GetString("value", 0) ?? string.Empty;

        if (!arguments.TryGetInt32("count", 1, 0, out var count))
            return InvalidArgument("count");

        return Respond(operation, new XElement(SoapNames.Service + "EchoValueResult", $"{value}:{count}"));
    }

    private static SoapResult AddRecordWithDetail(string operation, SoapArguments arguments)
    {
        var product = arguments.GetElement("product");

        if (product is null)
            return SoapResult.Fault(SoapFaultCode.Sender, "Argument 'product' is required.");

        var detail = SoapElement.Child(product, "Detail");

        return Respond(
            operation,
            new XElement(SoapNames.Service + "AddRecordWithDetailResult", true),
            new XElement(
                SoapNames.Service + "ReceivedProduct",
                new XElement(SoapNames.Service + "Id", SoapElement.Text(product, "Id")),
                new XElement(SoapNames.Service + "Code", SoapElement.Text(product, "Code")),
                new XElement(SoapNames.Service + "Name", SoapElement.Text(product, "Name")),
                new XElement(SoapNames.Service + "IsActive", SoapElement.Text(product, "IsActive")),
                new XElement(
                    SoapNames.Service + "Detail",
                    new XElement(SoapNames.Service + "PartnerId", SoapElement.Text(detail, "PartnerId")),
                    new XElement(SoapNames.Service + "ManufacturerId", SoapElement.Text(detail, "ManufacturerId")),
                    new XElement(SoapNames.Service + "SupplierId", SoapElement.Text(detail, "SupplierId")))));
    }

    private static SoapResult GetProduct(string operation, SoapArguments arguments)
    {
        if (!arguments.TryGetInt32("id", 0, 0, out var id))
            return InvalidArgument("id");

        return Respond(
            operation,
            new XElement(
                SoapNames.Service + "GetProductResult",
                new XElement(SoapNames.Service + "Id", id),
                new XElement(SoapNames.Service + "Code", $"P-{id}"),
                new XElement(SoapNames.Service + "Name", $"Product {id}"),
                new XElement(SoapNames.Service + "IsActive", true),
                new XElement(
                    SoapNames.Service + "Detail",
                    new XElement(SoapNames.Service + "PartnerId", id + 100),
                    new XElement(SoapNames.Service + "ManufacturerId", id + 200),
                    new XElement(SoapNames.Service + "SupplierId", id + 300))));
    }

    private static SoapResult ThrowFault()
        => SoapResult.Fault(SoapFaultCode.Sender, "The operation failed on purpose.");

    private static SoapResult ThrowEmptyFault()
        => SoapResult.Fault(SoapFaultCode.Sender, string.Empty);

    private static SoapResult NotFound500()
        => SoapResult.Fault(SoapFaultCode.Sender, "The requested record was not found.");

    private static async Task<SoapResult> SlowOpAsync(
        string operation,
        SoapArguments arguments,
        CancellationToken cancellationToken)
    {
        if (!arguments.TryGetInt32("delayMs", 0, 0, out var requested))
            return InvalidArgument("delayMs");

        var delay = Math.Clamp(requested, 0, MaxDelayMilliseconds);

        await Task.Delay(delay, cancellationToken);

        return Respond(operation, new XElement(SoapNames.Service + "SlowOpResult", delay));
    }

    private static SoapResult IsValid(string operation, SoapArguments arguments)
    {
        var id = arguments.GetString("id", 0);

        return Respond(
            operation,
            new XElement(SoapNames.Service + "IsValidResult", string.IsNullOrEmpty(id) ? -1 : 1));
    }

    private static SoapResult AddRecordWithDetailWithLocations(string operation, SoapArguments arguments)
    {
        var product = arguments.GetElement("product");

        if (product is null)
            return SoapResult.Fault(SoapFaultCode.Sender, "Argument 'product' is required.");

        var detail = SoapElement.Child(product, "Detail");

        if (detail is null)
            return SoapResult.Fault(SoapFaultCode.Sender, "Argument 'product.Detail' is required.");

        var locations = arguments.GetElement("associatedLocationIds");
        var locationIds = locations is null
            ? Array.Empty<string>()
            : locations.Elements().Select(element => element.Value).ToArray();

        return Respond(
            operation,
            new XElement(SoapNames.Service + "AddRecordWithDetailWithLocationsResult", true),
            new XElement(
                SoapNames.Service + "ReceivedLocationIds",
                locationIds.Select(id => new XElement(SoapNames.Service + "int", id))));
    }

    private static SoapResult Respond(string operation, params object[] content)
        => SoapResult.Payload(new XElement(SoapNames.Service + $"{operation}Response", content));

    private static SoapResult InvalidArgument(string name)
        => SoapResult.Fault(SoapFaultCode.Sender, $"Argument '{name}' must be a 32-bit integer.");
}
