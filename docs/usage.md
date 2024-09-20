After package installation for correct using and working you must know a few things:

-> Register client DI; <br/>
-> Available methods;  <br/>
-> Current and possible way to complete request;  <br/>
-> Invoke order. <br/>

After intallation in the `Startup.cs` class add the following blocks:
```csharp
public void ConfigureServices(IServiceCollection services)
{
           ...
           //   To register SOAP client
           services.RegisterSoapClientsEndpoint();
           
           ...
}
```

Once the SOAP client is registered you can inject it where is required or where you want to call some SOAP service.

```csharp
public class Foo
{
    private readonly Func<SoapProtocolType, ISoapClientEndpoint> _clientFactory;
    
    public void Foo(Func<SoapProtocolType, ISoapClientEndpoint> clientFactory)
    {
        _clientFactory = clientFactory;
    }
    
    public IResult<IEnumerable<Document>> GetDocument()
    {
        var client = _clientFactory(SoapProtocolType.SOAP_1_1);
    }
}
```

Available SOAP protocols (enum: `SoapProtocolType`):
 - SOAP 1.1 -> SOAP_1_1;
 - SOAP 1.2 -> SOAP_1_2.
 
 Available methods to be called are defined in interface `ISoapClientEndpoint`:
 
 - `IResult<HttpRequestMessage> BuildRequest(...)`;
 - `IResult<HttpResponseMessage> SendRequest(...)`;
 - `Task<IResult<HttpResponseMessage>> SendRequestAsync(...)`;
 - `IResult SetClientTimeout(...)`;
 - `IResult CheckBodyForFaultCode(...)`;
 - `IResult<XmlNode> GetXmlNodeResponseBody(...)`;
 - `IResult<XNode> GetXNodeResponseBody(...)`.
 
 The invoke flow is divided in a few methods:
 
 - Build the HTTP request with SOAP params;
 - Set the timeout if is needed;
 - Send the HTTP request (sync or async);
 - Check the HTTP response for fault codes or exceptions;
 - Get the HTTP response in `XNode` or `XMLNode` format OR simply read response content as string `...Content.ReadAsStringAsync()`;
 

<table>
	<thead>
		<tr>
			<th>Name</th>
			<th>Description</th>
			<th>Available parameters</th>
		</tr>
	</thead>
	<tbody>
		<tr>
			<td>
				<b>BuildRequest</b>
			</td>
			<td>
		First and the main method is where is builed the HTTP request in dependence of input params.
		<br/>
				<br/>
		Result is type of `IResult&lt;HttpRequestMessage&gt;` and response is used for send request methods.
		</td>
			<td>
				<b>HttpMethod method</b> -> the HTTP method used to send the request (`GET` or `POST`). <br/>
				<b>Uri endpoint</b> -> Service URL where will be sending the request.<br/>
				<b>IEnumerable&lt;XElement&gt; bodies</b> -> An array of `XElement` which represents the SOAP body content.<br/>
				<b>IEnumerable&lt;XElement&gt; headers</b> -> An array of `XElement` which represents the SOAP header content.<br/>
				<b>Encoding bodyEncoding</b> -> Represent which algorithm will be used to encode the content, default UTF8.<br/>
				<b>string action</b> -> The current method action URL.<br/>
				<b> IEnumerable&lt;XAttribute&gt; ownSoapEnvelopeAttributes</b> -> SOAP Envelope additional defined attributes.<br/>
				<b>Dictionary&lt;string, IEnumerable&lt;string&gt;&gt; httpClientHeaders</b> -> HTTP client custom header variables.<br/>
				<b>bool buildGetRequestAsRest</b> -> Indicates that if the HTTP method is GET, then the current request will be generated as normal SOAP or as REST.<br/>
			</td>
		</tr>
		<tr>
			<td>
				<b>SendRequest</b>
			</td>
			<td>
	    Method used to send the HTTP request.
		<br/>
				<br/>
		Result is type of `IResult&lt;HttpResponseMessage&gt;` and response is used for send request methods.
	    </td>
			<td>
				<b>HttpRequestMessage request</b> -> The HTTP request parameter. <br/>
			</td>
		</tr>
		<tr>
			<td>
				<b>SendRequestAsync</b>
			</td>
			<td>
	    Method used to send the HTTP request async.
		<br/>
				<br/>
		Result is type of `IResult&lt;HttpResponseMessage&gt;` and response is used for send request methods.
	    </td>
			<td>
				<b>HttpRequestMessage request</b> -> The HTTP request parameter. <br/>
				<b>CancellationToken cancellationToken</b> -> A token that allows processing to be cancelled. <br/>
			</td>
		</tr>
		<tr>
			<td>
				<b>SetClientTimeout</b>
			</td>
			<td>Sets client timeout.</td>
			<td>
				<b>TimeSpan clientTimeout</b> -> The client timeout as TimeSpan. <br/>
			</td>
		</tr>
		<tr>
			<td>
				<b>CheckBodyForFaultCode</b>
			</td>
			<td> 
	    Check response body (string value) for fault code.
	    </td>
			<td>
				<b>string soapResponse</b> -> The SOAP response body readed as string value. <br/>
			</td>
		</tr>
		<tr>
			<td>
				<b>GetXmlNodeResponseBody</b>
			</td>
			<td>
	    Gets response body XmlNode.
	    </td>
			<td>
				<b>string soapResponse</b> -> The SOAP response body readed as string value. <br/>
				<b>string soapNamespace</b> -> TThe SOAP namespace. <br/>
				<b>string soapXmlBodyTag</b> -> The SOAP XML body tag. <br/>
			</td>
		</tr>
		<tr>
			<td>
				<b>GetXNodeResponseBody</b>
			</td>
			<td>
	    Gets response body XNode.
	    </td>
			<td>
				<b>string soapResponse</b> -> The SOAP response body readed as string value. <br/>
				<b>string soapNamespace</b> -> TThe SOAP namespace. <br/>
				<b>string soapXmlBodyTag</b> -> The SOAP XML body tag. <br/>
			</td>
		</tr>
	</tbody>
</table>

Example:

```csharp
public void AddNewProduct()
{
    var client = _clientFactory(SoapProtocolType.SOAP_1_1);
    var ns = XNamespace.Get("http://SoapClientCallAssist.local/");

    var soapRequest = client.BuildRequest(
        HttpMethod.Post,
        _baseUri,
        bodies: new List<XElement>()
        {
            new XElement(
                ns.GetName("AddRecordWithDetail"),
                new XElement(ns.GetName("product"),
                    new XElement(ns.GetName("Id"), "1"),
                    new XElement(ns.GetName("Code"), "Code-001"),
                    new XElement(ns.GetName("Name"), "Name-001"),
                    new XElement(ns.GetName("IsActive"), "true"),
                    new XElement(ns.GetName("Detail"),
                        new XElement(ns.GetName("ManufacturerId"), "1"),
                        new XElement(ns.GetName("SupplierId"), "2"),
                        new XElement(ns.GetName("PartnerId"), "3")
                        )
                    )
            )
        });
}
```
<br />

```csharp
public void AddNewProduct()
{
    var client = _clientFactory(SoapProtocolType.SOAP_1_1);
    var ns = XNamespace.Get("http://SoapClientCallAssist.local/");
    var nsObject = XNamespace.Get("http://schemas.datacontract.org/2004/07/TestSoapServiceN45.Dto");
    var action = "http://SoapClientCallAssist.local/IServiceSvc/AddRecordWithDetail";

    var soapRequest = client.BuildRequest(
        HttpMethod.Post,
        _baseUri,
        bodies: new List<XElement>()
        {
            new XElement(
                ns.GetName("AddRecordWithDetail"),
                new XElement(ns.GetName("product"),
                    new XElement(nsObject.GetName("Code"), "Code-001"),
                    new XElement(nsObject.GetName("Detail"),
                        new XElement(nsObject.GetName("ManufacturerId"), 1),
                        new XElement(nsObject.GetName("PartnerId"), 3),
                        new XElement(nsObject.GetName("SupplierId"), 2)
                    ),
                    new XElement(nsObject.GetName("Id"), 1),
                    new XElement(nsObject.GetName("IsActive"), true),
                    new XElement(nsObject.GetName("Name"), "Name-001")
                )
            )
        },
        ownSoapEnvelopeAttributes: new List<XAttribute>()
        {
            new XAttribute(XNamespace.Xmlns + "tes", nsObject),
            new XAttribute(XNamespace.Xmlns + "externalNs", ns)
        },
        action: action);
}
```



