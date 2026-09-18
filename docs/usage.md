After package installation for correct using and working you must know a few things:

-> Register client DI; <br/>
-> Available methods;  <br/>
-> Current and possible way to complete request;  <br/>
-> Invoke order;  <br/>
-> Data mapper for request/response models. <br/>
-> What a send result carries when the service answers with a non-2xx status. <br/>
-> WS-Security: message signing, username and SAML tokens, WS-Addressing, the symmetric binding with encryption, secure conversation, and checking the response against the request. <br/>
-> HTTP transport: attaching a client certificate, a proxy or a custom handler, and transport authentication (mTLS, Negotiate/NTLM). <br/>
-> The test environment of this repository. <br/>

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
    
    public Foo(Func<SoapProtocolType, ISoapClientEndpoint> clientFactory)
    {
        _clientFactory = clientFactory;
    }
    
    public ISoapClientEndpoint GetClient()
    {
        return _clientFactory(SoapProtocolType.SOAP_1_1);
    }
}
```

Available SOAP protocols (enum: `SoapProtocolType`):
 - SOAP 1.1 -> SOAP_1_1;
 - SOAP 1.2 -> SOAP_1_2.

Invoking the factory with any other `SoapProtocolType` value throws `NotImplementedException` naming the value; it previously surfaced as a `FormatException`.
 
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
				<b>bool buildGetRequestAsSlashUrl</b> -> Indicates that if the HTTP method is GET, then the current request will be generated as normal SOAP or as URL with slash between params.<br/>
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
	    Check response body (string value) for fault code. A fault is only recognised as a direct child of the `Body` that sits directly under the `Envelope`, in this client's protocol namespace. A `Fault` planted anywhere else, such as in the `Header`, is not a fault.
		<br/>
				<br/>
	    Fails with `ER-BEC-FLT` when the Body carries a fault (the fault text is the message), with `ER-BEC-GRB-01` when the document is not a SOAP envelope at all (a proxy's HTML error page, for example, which was previously reported as "no fault"), and with `ER-XML-DEPTH` when the document nests too deeply. An envelope in the <i>other</i> protocol's namespace is not one of this client's responses and reports success, deliberately.
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
	    Gets response body XmlNode: the first <i>element</i> child of the SOAP `Body`, never a comment, a processing instruction or whitespace. Fails with `ER-BEC-GRB-01` when the document is not a SOAP envelope or resolves no single `Body`, with `ER-BEC-GRB-02` when the `Body` holds no element child, with `ER-XML-DEPTH` when the document nests too deeply, and with `ER-BEC-GRB-03` for anything that is not well-formed XML.
	    </td>
			<td>
				<b>string soapResponse</b> -> The SOAP response body readed as string value. <br/>
				<b>string soapNamespace</b> -> The SOAP envelope namespace to demand, or null to accept either protocol. <br/>
				<b>string soapXmlBodyTag</b> -> An optional, possibly prefixed, Body tag such as `s:Body`. Its local part must be `Body`; a tag naming any other element makes the call fail with `ER-BEC-GRB-01`. <br/>
			</td>
		</tr>
		<tr>
			<td>
				<b>GetXNodeResponseBody</b>
			</td>
			<td>
	    Gets response body XNode. Same element, same rules and same failure codes as `GetXmlNodeResponseBody`, re-parsed into an `XDocument`.
	    </td>
			<td>
				<b>string soapResponse</b> -> The SOAP response body readed as string value. <br/>
				<b>string soapNamespace</b> -> The SOAP envelope namespace to demand, or null to accept either protocol. <br/>
				<b>string soapXmlBodyTag</b> -> Same as for `GetXmlNodeResponseBody`. <br/>
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

A few things about the result objects and the response side that are easy to get wrong:

`IResult` failures carry a code in `Messages[0].Key` (`ER-BEC-FLT`, `V_BEC_VR_002`, `V-SEC-003`, ...) that a caller can branch on instead of string-matching the text, but not every entry point preserves it. The `BuildRequest(HttpMethod, BuildSoapRequestDto)` overload, `SendRequest` and `SendRequestAsync` propagate the code and its details. The long parameter list `BuildRequest` overload still flattens a failure into a bare message, so `Messages[0].Key` is `null` on that path; a test in this repository pins that as `DEFECT-VALIDATION-CODE-LOST` until it is fixed. If you need the code, build through the DTO overload.

<a name="send-contract"></a>
`SendRequest` and `SendRequestAsync` succeed only on a 2xx status. Any other status is a failed result whose code names the status class, and whose `Response` still carries the `HttpResponseMessage`, so nothing the service sent is lost; previously a 401, 403 or 502 page was reported as a successful send and then classified as "not a SOAP envelope" by the readers. The classification never reads the body, and no message ever quotes an error page.

<table>
	<thead>
		<tr>
			<th>Code</th>
			<th>Status</th>
			<th>What the message carries</th>
		</tr>
	</thead>
	<tbody>
		<tr>
			<td><b>ER-BEC-HTTP-401</b></td>
			<td>401</td>
			<td>The `WWW-Authenticate` scheme and, when present, its realm (truncated to 128 characters), so the caller learns what to cache credentials under. Nothing else of the challenge, and never the body.</td>
		</tr>
		<tr>
			<td><b>ER-BEC-HTTP-403</b></td>
			<td>403</td>
			<td>The identity or client certificate presented is not authorized for the endpoint.</td>
		</tr>
		<tr>
			<td><b>ER-BEC-HTTP-FLT</b></td>
			<td>500 or 400 with a SOAP media type (`application/soap+xml` or `text/xml`)</td>
			<td>A SOAP fault. Read the body from `.Response` and pass it to `CheckBodyForFaultCode`, which reports `ER-BEC-FLT` with the fault text.</td>
		</tr>
		<tr>
			<td><b>ER-BEC-HTTP-4XX</b></td>
			<td>any other 4xx</td>
			<td>The status code and reason phrase.</td>
		</tr>
		<tr>
			<td><b>ER-BEC-HTTP-5XX</b></td>
			<td>any other 5xx</td>
			<td>The status code and reason phrase; without a SOAP media type this is an error page, not a fault.</td>
		</tr>
		<tr>
			<td><b>ER-BEC-HTTP-3XX</b></td>
			<td>3xx</td>
			<td>A redirect the handler did not follow. With the default `AllowAutoRedirect = true` the handler follows redirects itself and this code is not seen; see the transport section for why an authenticated handler should turn that off.</td>
		</tr>
		<tr>
			<td><b>ER-BEC-HTTP-STS</b></td>
			<td>anything else</td>
			<td>The status code and reason phrase, or no response at all.</td>
		</tr>
	</tbody>
</table>

A transport failure that produces no response at all (a refused connection, a timeout) is still reported under the send error codes (`ER-BEC-BSRM-SR`, `ER-BEC-BSRM-SRA`) with a `null` `Response`.

```csharp
var sent = await client.SendRequestAsync(built.Response);

if (sent.IsSuccess == false)
{
    var code = sent.Messages.FirstOrDefault()?.Key;

    using (var response = sent.Response)
    {
        if (code == "ER-BEC-HTTP-FLT" && response != null)
        {
            var faultBody = await response.Content.ReadAsStringAsync();
            var fault = client.CheckBodyForFaultCode(faultBody);   // fails with ER-BEC-FLT and the fault text
        }
    }

    return;
}
```

Because the failed result carries the response, a caller who ignores `.Response` on failure holds an undisposed `HttpResponseMessage` until it is collected; dispose it as above, or read what you need from it and then dispose it.

Every envelope is written compact, on the signed and the unsigned path alike: no synthesised indentation is ever inserted, so signing never changes the layout of the bytes a service receives and no whitespace you did not put there is digested. This is not a cosmetic choice; a WCF service rejects a signed envelope whose `Body` is written indented.

`GetXmlNodeResponseBody` and `GetXNodeResponseBody` hand back the first <i>element</i> child of the `Body`. Comments and processing instructions are dropped at the reader, at every depth, and whitespace is skipped. This matters on a WS-Security-signed response: a signature is computed over a canonical form that carries neither, so a comment an intermediary injects in front of the payload is content nobody signed. Previously that comment came back as the "payload", an `XmlComment` with attacker-controlled text, under a passing verification; now the signed element is what you get, and a `Body` with no element child fails with `ER-BEC-GRB-02`. The `soapXmlBodyTag` override exists only for prefix tolerance (`s:Body`, `soap:Body`); its local part must be `Body`, since honouring any other name would let a caller point the reader at the unsigned `Header`.

`CheckBodyForFaultCode` fails, rather than reporting "no fault", when the document is not a SOAP envelope at all, and only looks for a `Fault` directly under the `Body` that sits directly under the `Envelope`. An envelope in the other protocol's namespace is deliberately reported as success: its faults are not this client's faults to classify.

Every response parse is hardened the same way: DTD processing is prohibited, there is no external resolver, entity expansion is capped, and element nesting is bounded at 64 levels, beyond which the call fails with `ER-XML-DEPTH` instead of running for minutes (a response nested 200,000 deep previously did not return within three minutes; it now fails in about a second). The 8 MB document size cap applies to the model mapper and to the signature verifier only; the raw client paths accept a response of any size, on purpose, so that a consumer who already receives large responses is not regressed.

Building the body by hand with nested `XElement` calls works fine for a small payload, but once a real operation takes a model with a dozen fields it gets easy to typo an element name or forget one. `ISoapModelMapper` is a second way to build the same kind of body: you decorate a plain C# class once, and the mapper produces the `XElement` tree from it, then binds the response straight back into a model. It is registered by the same `services.RegisterSoapClientsEndpoint()` call shown above, so there is nothing extra to wire up, just inject `ISoapModelMapper` where you need it.

Decorating a model uses two attributes:

-> `[SoapContract(Name, Namespace)]` on the class, both optional. `Name` defaults to the CLR type name, `Namespace` defaults to whatever namespace the call site resolves to (the request namespace, or the namespace of the member that carries this type). <br/>
-> `[SoapMember(Name, Namespace, Order, Path, ItemName)]` on each property you want on the wire. A member needs a public getter and a public setter; if you decorate a property that has no setter, the mapper fails outright rather than silently dropping it. Worth knowing that this is reported as `V-MAP-002 Unsupported member type`, which points at the type rather than at the missing setter. <br/>

<table>
	<thead>
		<tr>
			<th>Attribute</th>
			<th>Property</th>
			<th>Description</th>
		</tr>
	</thead>
	<tbody>
		<tr>
			<td rowspan="2"><b>SoapContract</b></td>
			<td><b>Name</b></td>
			<td>The wire element name of the type. Defaults to the CLR type name.</td>
		</tr>
		<tr>
			<td><b>Namespace</b></td>
			<td>The XML namespace for the type and, by default, for its members. Defaults to the namespace resolved at the call site.</td>
		</tr>
		<tr>
			<td rowspan="5"><b>SoapMember</b></td>
			<td><b>Name</b></td>
			<td>The wire name of the element. This is the point of the attribute: the service can call something `IsValidResult` while the C# property stays `Result`. Defaults to the property name.</td>
		</tr>
		<tr>
			<td><b>Namespace</b></td>
			<td>The namespace of this member. Defaults to the namespace resolved for the declaring type.</td>
		</tr>
		<tr>
			<td><b>Order</b></td>
			<td>Emission order, lower values first. Default is `-1` (unspecified). When every member is left unspecified they keep their declaration order, but note `-1` sorts <i>before</i> an explicit `Order = 0`, so mixing set and unset ordering in one class pushes the unset members to the front. If the service expects an `xsd:sequence`, set `Order` on every member or on none of them.</td>
		</tr>
		<tr>
			<td><b>Path</b></td>
			<td>A slash separated chain of local names, not an XPath expression, used to locate this member when binding a response, ex: `ReceivedProduct/Detail`.</td>
		</tr>
		<tr>
			<td><b>ItemName</b></td>
			<td>The element name emitted per item of a collection member, ex: `int` so a `List&lt;int&gt;` becomes `&lt;ids&gt;&lt;int&gt;1&lt;/int&gt;&lt;/ids&gt;`. Defaults to the item type's contract name.</td>
		</tr>
	</tbody>
</table>

Building a request follows the same shape as the raw examples above, except the body comes from `ToBodies` instead of an `XElement` tree you write yourself:

```csharp
var client = _clientFactory(SoapProtocolType.SOAP_1_1);
var ns = XNamespace.Get("http://SoapClientCallAssist.local/");

var request = new SoapOperationRequest("AddRecordWithDetail", ns)
    .AddParameter("product", product);
var map = _mapper.ToBodies(request);   // IResult<IEnumerable<XElement>>

var soapRequest = client.BuildRequest(
    HttpMethod.Post,
    new BuildSoapRequestDto(new HttpClientDto(_baseUri),
        new SoapEnvelopeDto(map.Response))
);
```

`SoapOperationRequest` carries the operation name, the namespace the operation and its parameter elements are emitted in, and an ordered list of arguments built through `AddParameter`. The parameters are a sequence rather than a single object because a real operation usually takes more than one, a model plus a handful of scalars, for example.

Given this model:

```csharp
[SoapContract(Name = "product", Namespace = "http://SoapClientCallAssist.local/")]
public class SoapMemberProduct
{
    [SoapMember] 
	public int? Id { get; set; }

    [SoapMember]
	 public string Code { get; set; }

    [SoapMember] 
	public string Name { get; set; }

    [SoapMember] 
	public bool IsActive { get; set; }

    [SoapMember]
	 public SoapMemberProductDetail Detail { get; set; }
}
```

`ToBodies` produces exactly this, verified against a real ASMX endpoint:

```xml
<AddRecordWithDetail xmlns="http://SoapClientCallAssist.local/"><product><Id>7</Id><Code>C1</Code><Name>N1</Name><IsActive>true</IsActive><Detail><PartnerId>1</PartnerId><ManufacturerId>2</ManufacturerId><SupplierId>3</SupplierId></Detail></product></AddRecordWithDetail>
```

Notice the namespace: every element that lands in a non-empty namespace is declared as a default `xmlns`, never with a prefix. This is what lets a document mix namespaces cleanly, a member that overrides `Namespace` on its own just declares its own default namespace at the point it is needed, instead of the whole document juggling prefixes.

A collection parameter follows the same rule. `.AddParameter("associatedLocationIds", new List<int> { 11, 22 })` emits `<associatedLocationIds><int>11</int><int>22</int></associatedLocationIds>`, the `int` tag per item coming from `ItemName`, or from the item type's contract name when `ItemName` is not set.

Reading the response back is the mirror operation:

```csharp
var response = await soapCall.Response.Content.ReadAsStringAsync();
var bound = _mapper.FromResponse<SoapMemberAddRecordWithDetailResponse>(response, ns);   // IResult<T>
```

The full signature is `IResult<T> FromResponse<T>(string soapResponse, XNamespace protocolNamespace, string soapXmlBodyTag = null)`. Despite the parameter name, pass the same service namespace you used to build the request, exactly as the examples above do. The mapper uses it to resolve the namespace of the operation element when the response does not carry one itself. It can also pick between several `Body` candidates, but only when the value you pass is a real SOAP envelope namespace; with a service namespace that branch never matches and the mapper simply requires the response to contain exactly one `Body`, which is the normal case anyway.

The mapper applies the same envelope discipline as the raw client. The document element must be an `Envelope` in a SOAP protocol namespace, the `Body` must be its single direct child of that name in the envelope's own namespace (`V-MAP-009` otherwise), and a `Fault` is only recognised as a direct child of that `Body`, reported as `ER-MAP-FLT`. A `soap:Fault` planted in the unsigned `Header` no longer makes `FromResponse` report a fault, and a `Body` nested inside the `Header` is no longer bound. The optional `soapXmlBodyTag` follows the raw client's rule too: its local part must be `Body`. The first element of the `Body` must also match the contract's element name (the `SoapContract.Name`, or the CLR type name, compared case-insensitively); a validly signed response to a <i>different</i> operation previously bound silently onto the wrong model, and now fails with `V-MAP-012`. Responses larger than 8 MB are refused with `ER-MAP-RSP` before parsing.

Two things worth knowing before you rely on this for a real integration:

`FromResponse<T>` needs a decorated class, even to read back a single scalar. `FromResponse<int>(response, ns)` fails with `Type 'Int32' declares no mapped members.`, because the mapper always looks for a mapped type rather than a bare value. The fix is a small response contract, which is also a good place to see why `Name` matters on `SoapMember`: the service returns `<IsValidResult>`, but nothing forces the property to be called that:

```csharp
[SoapContract(Name = "IsValidResponse", Namespace = "http://SoapClientCallAssist.local/")]
public class SoapMemberIsValidResponse
{
    [SoapMember(Name = "IsValidResult")]
    public int Result { get; set; }
}
```

The other one is a real limitation, not a style choice: build mapper requests with `HttpMethod.Post`, not `Get`. The mapper always emits the operation element into a namespace, an unqualified operation fails mapper validation outright, and a mapper-built GET writes that namespace-qualified name straight into the URL path. That produces something like `.../ServiceAsmx.asmx/{http://SoapClientCallAssist.local/}IsValid?id=s1`, which the server rejects with `400 Bad Request`. This reproduces identically under SOAP 1.1 and SOAP 1.2, so for now treat every mapper-built call as POST only; `BuildRequest` with a hand-built body and `HttpMethod.Get` is unaffected.

Both SOAP 1.1 and SOAP 1.2 work with the mapper the same way, through whichever `ISoapClientEndpoint` you got from the client factory; nothing about `ToBodies` or `FromResponse` changes with the protocol.

<a name="ws-security"></a>
Alongside the raw envelope builder and the model mapper, the library can sign an outgoing message with WS-Security and verify the signature on the reply. Both are driven by one object, `SoapSecurityDto`, assigned to the `Security` property of `BuildSoapRequestDto`. Note that only the `BuildRequest(HttpMethod, BuildSoapRequestDto)` overload carries security; the long parameter list overload has no security parameter, so signing is reachable only through the DTO overload.

```csharp
public async Task CallSigned(X509Certificate2 signingCertificate)
{
    var client = _clientFactory(SoapProtocolType.SOAP_1_2);
    var ns = XNamespace.Get("http://SoapClientCallAssist.local/");

    var security = new SoapSecurityDto
    {
        SigningCertificate = signingCertificate
    };

    var built = client.BuildRequest(
        HttpMethod.Post,
        new BuildSoapRequestDto
        {
            Client = new HttpClientDto(_baseUri),
            Envelope = new SoapEnvelopeDto(
                new List<XElement>
                {
                    new XElement(ns.GetName("IsValid"), new XElement(ns.GetName("id"), "s1"))
                },
                action: "http://SoapClientCallAssist.local/IServiceSvc/IsValid"),
            Security = security
        });

    if (built.IsSuccess == false)
    {
        // built.Messages carries the failure code (V-SEC-002, V-SEC-003, ...) and its details.
        return;
    }

    var sent = await client.SendRequestAsync(built.Response);
}
```

That is the whole opt-in. `SoapSecurityDto.Enabled` defaults to `true`, so an options object that names a signing certificate signs the message; there is no separate switch to turn on. Set `Enabled = false` when you want to keep the same options object around but send this particular request unsigned.

What gets signed by default is the SOAP `Body` and a `wsu:Timestamp` that the library adds to the `wsse:Security` header. The signing certificate must expose an accessible RSA private key, otherwise the build fails with `V-SEC-002 A signing certificate with an accessible RSA private key is required when message security is enabled.` The signing certificate itself travels in the header as a `wsse:BinarySecurityToken`, referenced from the signature `KeyInfo`.

Signing is `POST` only. A `GET` request with signing enabled fails validation with `V-SEC-003 Signing a SOAP message is only supported for HTTP POST requests.`, the same restriction the mapper has, and for a related reason: there is no body on the wire to sign.

-> `SoapSecurityDto` options and their defaults: <br/>

<table>
	<thead>
		<tr>
			<th>Property</th>
			<th>Default</th>
			<th>Description</th>
		</tr>
	</thead>
	<tbody>
		<tr>
			<td><b>Enabled</b></td>
			<td>`true`</td>
			<td>Whether the request is signed at all. Defaults to on, so supplying a signing certificate is enough.</td>
		</tr>
		<tr>
			<td><b>SigningCertificate</b></td>
			<td>`null`</td>
			<td>The `X509Certificate2` the request is signed with. Required when signing, together with an accessible RSA private key.</td>
		</tr>
		<tr>
			<td><b>SignBody</b></td>
			<td>`true`</td>
			<td>Whether the SOAP `Body` is covered by the signature.</td>
		</tr>
		<tr>
			<td><b>IncludeTimestamp</b></td>
			<td>`true`</td>
			<td>Whether a `wsu:Timestamp` is added to the Security header.</td>
		</tr>
		<tr>
			<td><b>SignTimestamp</b></td>
			<td>`true`</td>
			<td>Whether that timestamp is covered by the signature. No effect when `IncludeTimestamp` is false.</td>
		</tr>
		<tr>
			<td><b>TimestampTimeToLive</b></td>
			<td>5 minutes</td>
			<td>The validity window written into the timestamp, from the moment it is created.</td>
		</tr>
		<tr>
			<td><b>MustUnderstand</b></td>
			<td>`true`</td>
			<td>Whether the Security header carries a `mustUnderstand` attribute.</td>
		</tr>
		<tr>
			<td><b>SecurityActor</b></td>
			<td>`null`</td>
			<td>The SOAP 1.1 actor or SOAP 1.2 role the Security header targets. The attribute matching the protocol in use is emitted.</td>
		</tr>
		<tr>
			<td><b>DigestAlgorithm</b></td>
			<td>`SoapDigestAlgorithmType.Sha256`</td>
			<td>The digest algorithm for every signed reference. Also `Sha512` and `Sha1`.</td>
		</tr>
		<tr>
			<td><b>SignatureAlgorithm</b></td>
			<td>`SoapSignatureAlgorithmType.RsaSha256`</td>
			<td>The signature algorithm. Also `RsaSha512` and `RsaSha1`.</td>
		</tr>
		<tr>
			<td><b>Canonicalization</b></td>
			<td>`SoapCanonicalizationType.ExclusiveC14N`</td>
			<td>The canonicalization method for the signature and for every reference transform. The only other value, `InclusiveC14N`, is not interoperable, see below.</td>
		</tr>
		<tr>
			<td><b>AdditionalSignedElementIds</b></td>
			<td>`null`</td>
			<td>`wsu:Id` values of elements already present among the request headers or body, to cover alongside the Body and timestamp. Every id must resolve to exactly one element, or signing fails. An id resolving to an element that contains the Security header fails with `V-SEC-014`, because the digest would be taken over the subtree the signature is then inserted into.</td>
		</tr>
		<tr>
			<td><b>Signer</b></td>
			<td>`null`</td>
			<td>An `ISoapMessageSigner` to use instead of the built-in `WsSecurityMessageSigner`. Its `Sign` returns the exact wire string that must be sent; a re-serialization of it is a different document and no longer matches the signature.</td>
		</tr>
		<tr>
			<td><b>ExpectedResponseCertificate</b></td>
			<td>`null`</td>
			<td>The certificate the response signature is expected to have been produced with. Read only when you call the verification method yourself, see below.</td>
		</tr>
		<tr>
			<td><b>ResponseVerificationPolicy</b></td>
			<td>`null`</td>
			<td>A `SoapVerificationPolicyDto`, or null for the strict defaults. Read only when you call the verification method yourself.</td>
		</tr>
		<tr>
			<td><b>ResponseVerifier</b></td>
			<td>`null`</td>
			<td>An `ISoapMessageVerifier` to use instead of the built-in `WsSecurityMessageVerifier`, or the one the client was constructed with. Read only when you call the verification method yourself; when it is set, the null certificate check is whatever this verifier enforces. Refused with `V-SEC-035` on a symmetric binding or a secure conversation.</td>
		</tr>
		<tr>
			<td><b>UsernameToken</b></td>
			<td>`null`</td>
			<td>A `SoapUsernameTokenDto` to carry, see <a href="#username-token">UsernameToken</a>. On its own it makes a token-only header with no signature.</td>
		</tr>
		<tr>
			<td><b>Addressing</b></td>
			<td>`null`</td>
			<td>A `SoapAddressingDto`, see <a href="#ws-addressing">WS-Addressing</a>. Required by `SymmetricBinding` and `SecureConversation`.</td>
		</tr>
		<tr>
			<td><b>SymmetricBinding</b></td>
			<td>`null`</td>
			<td>A `SoapSymmetricBindingDto`. Setting it selects the symmetric mode, see <a href="#symmetric-binding">Symmetric binding</a>; cannot be combined with `SecureConversation` (`V-SEC-090`).</td>
		</tr>
		<tr>
			<td><b>Encryption</b></td>
			<td>`null`</td>
			<td>A `SoapEncryptionDto` naming the parts to encrypt. Requires the symmetric mode or a secure conversation (`V-SEC-051` otherwise).</td>
		</tr>
		<tr>
			<td><b>SamlToken</b></td>
			<td>`null`</td>
			<td>A `SoapSamlTokenDto` carrying an assertion issued elsewhere, see <a href="#saml">SAML</a>.</td>
		</tr>
		<tr>
			<td><b>SecureConversation</b></td>
			<td>`null`</td>
			<td>A `SoapSecureConversationDto` holding an established `Session`. Setting it selects the secure conversation mode, see <a href="#secure-conversation">Secure conversation</a>.</td>
		</tr>
		<tr>
			<td><b>ResponseSecurity</b></td>
			<td>`null`</td>
			<td>A `SoapResponseSecurityDto` saying what the response may and must carry when it is checked against the sent request, see <a href="#response-security">Response security</a>. Null applies the per-mode defaults.</td>
		</tr>
	</tbody>
</table>

The option groups added below the classic signing options each select or refine one WS-Security mode; how they combine, and what is refused, is described under <a href="#ws-security-modes">Security header modes</a>.

Verifying the response is a separate, explicit call. Two overloads are available, declared on `BaseEndpointClient`, which both `Soap11Client` and `Soap12Client` derive from:

-> `IResult<SoapSignatureVerificationResult> VerifyResponseSignature(string soapResponseBody, X509Certificate2 expectedCertificate, SoapVerificationPolicyDto policy = null)`; <br/>
-> `IResult<SoapSignatureVerificationResult> VerifyResponseSignature(string soapResponseBody, SoapSecurityDto security)`. <br/>

Neither is declared on `ISoapClientEndpoint`, so a caller holding the interface returned by the client factory has to reach the concrete client, as in the example below. `BaseEndpointClient` lives in the `SoapClientCallAssist.Client` namespace, which nothing else in this document needs, so add `using SoapClientCallAssist.Client;` before the downcast compiles. Both take the response body as a string, like `CheckBodyForFaultCode` and `GetXmlNodeResponseBody` do, so sending a request never consumes the response stream on your behalf. Both are now shims over the standalone `ISoapResponseSecurity` service described under <a href="#response-security">Response security</a>, which is also the route that needs no downcast, and the only route for a response to a symmetric binding or a secure conversation.

```csharp
using SoapClientCallAssist.Client;

...

var sent = await client.SendRequestAsync(built.Response);
var wire = await sent.Response.Content.ReadAsStringAsync();

var verification = ((BaseEndpointClient)client).VerifyResponseSignature(wire, security);

if (verification.IsSuccess == false)
{
    // verification.Messages carries the failure code and its details:
    //   V-SEC-004 .. V-SEC-012 and V-SEC-015 for a refused or invalid signature,
    //   V-SEC-013 for a null SoapSecurityDto,
    //   V-SEC-032 for options that select a symmetric binding or a secure conversation,
    //   ER-SEC-KEY, ER-SEC-DOM, ER-SEC-C14N, ER-SEC-VER and ER-XML-DEPTH for a verifier error,
    //   ER-BEC-VRS when the verifier threw or returned nothing at all.
    return;
}

var coverage = verification.Response;   // SoapSignatureVerificationResult
```

The second overload reads `ExpectedResponseCertificate`, `ResponseVerificationPolicy` and `ResponseVerifier` off the same `SoapSecurityDto` that configured the request, so the reply side is configured in the object you already have. The first overload takes the certificate and the policy directly. Verification trusts only the certificate you pass, never one embedded in the response, which is the whole point: a signature checked against a key the message itself supplied proves nothing. The failure codes are split by who is at fault: `V-SEC-013` fires only when the `SoapSecurityDto` itself is null; a null certificate, whether passed directly to the first overload or left as a null `ExpectedResponseCertificate` on the second, fails with `V-SEC-004` from the verifier. Be aware that the null check is the verifier's, not the client's: with a custom `ResponseVerifier` the null certificate is passed straight through and nothing is enforced unless your verifier enforces it.

The second overload also refuses, with `V-SEC-015`, options whose `ExpectedResponseCertificate` carries the same public key as `SigningCertificate`. A response "signed" with the client's own key is indistinguishable from an intermediary replaying the client's own signed request back at it, so a distinct service certificate is required and the check runs before anything is verified.

Which verifier runs is resolved in this order: `SoapSecurityDto.ResponseVerifier` when set, else the `ISoapMessageVerifier` the client was constructed with, else the built-in `WsSecurityMessageVerifier`. `RegisterSoapClientsEndpoint()` registers the built-in verifier with `TryAddSingleton`, and both `Soap11Client` and `Soap12Client` expose a constructor taking `(IHttpClientFactory, ISoapMessageVerifier)`, so registering your own implementation before that call makes it the one every client uses; a DI-registered verifier was previously ignored. `ISoapClientEndpoint` itself is unchanged.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddSingleton<ISoapMessageVerifier, MyAuditingVerifier>();
    services.RegisterSoapClientsEndpoint();
}
```

The built-in verifier accepts one signature shape only: a single `ds:Signature` that is a direct child of the single `wsse:Security` header, itself the only such child of the single `Header` directly under the `Envelope`. None, more than one, or a signature anywhere else fails with `V-SEC-006`. The `BodySigned` verdict is credited on exactly the element `GetXmlNodeResponseBody` returns the payload from, through one shared locator, so what was verified and what you then consume cannot be two different elements.

**Response verification is opt-in, and skipping it is silent.** `ExpectedResponseCertificate`, `ResponseVerificationPolicy` and `ResponseVerifier` are inert on their own. Filling them in and never calling `VerifyResponseSignature` means the response is not verified, and nothing warns you: the call succeeds, the body reads normally, and an unsigned or tampered reply is indistinguishable from a good one. Signing the request and reading the answer is not a round trip; the verification call is what makes it one.

On success, `SoapSignatureVerificationResult` describes what the signature actually covered, rather than only that it was valid: `BodySigned`, `TimestampSigned`, `Created` and `Expires` (the signed timestamp instants, normalized to UTC), and `SignedElementIds` with `SignedElementLocalNames` in the same order.

`SoapVerificationPolicyDto` names the conditions a signature must meet. Null applies these same defaults, so the shortest call is the strictest one and every relaxation has to be spelled out:

<table>
	<thead>
		<tr>
			<th>Property</th>
			<th>Default</th>
			<th>Description</th>
		</tr>
	</thead>
	<tbody>
		<tr>
			<td><b>RequireBodySigned</b></td>
			<td>`true`</td>
			<td>Verification fails unless the `Body` actually present at `/Envelope/Body` is the very element a signature reference covers. Reported as `V-SEC-009`.</td>
		</tr>
		<tr>
			<td><b>RequireValidTimestamp</b></td>
			<td>`true`</td>
			<td>Verification fails unless the message carries a `wsu:Timestamp` covered by the signature, readable as an unambiguous UTC ISO-8601 `wsu:Created` and `wsu:Expires` pair, whose window contains the current instant within `ClockSkew`. Reported as `V-SEC-010`, `V-SEC-011` and `V-SEC-012`.</td>
		</tr>
		<tr>
			<td><b>ClockSkew</b></td>
			<td>5 minutes</td>
			<td>Tolerance applied to both ends of the timestamp window, to absorb the clock difference between this host and the signing service.</td>
		</tr>
		<tr>
			<td><b>AllowSha1Algorithms</b></td>
			<td>`false`</td>
			<td>Whether `RSA-SHA1` as a signature method, or SHA-1 as a reference digest, is accepted. Off by default, so a SHA-1 signed response is rejected with `V-SEC-008` until you turn it on. On a symmetric binding the same switch also gates `HmacSha1` as the request's signature algorithm (`V-SEC-044` without it).</td>
		</tr>
		<tr>
			<td><b>DiagnosticDetail</b></td>
			<td>`false`</td>
			<td>Whether a decryption failure names the stage it failed in. Every failure after a decryption key has been derived is reported under the one code `ER-SEC-DEC` with no cause, so that an observer cannot tell a padding failure from a signature failure; with this on, the message text also names the stage (key derivation, cipher text, plaintext size, plaintext encoding, plaintext parse, plaintext content or signature), never the exception or any of the material. For diagnosing an integration only, not for production: the stage name is exactly the oracle the uniform code exists to withhold.</td>
		</tr>
	</tbody>
</table>

```csharp
var policy = new SoapVerificationPolicyDto
{
    RequireValidTimestamp = false,           // for a service that sends no timestamp at all
    ClockSkew = TimeSpan.FromMinutes(2)
};

var verification = ((BaseEndpointClient)client).VerifyResponseSignature(wire, expectedCertificate, policy);
```

**Leave `Canonicalization` at `ExclusiveC14N`.** `SoapCanonicalizationType.InclusiveC14N` produces signatures that independent verifiers reject. This is not a caution about edge cases, it is a measured result: a cross-stack suite in this repository signs the same envelope both ways and hands it to a Java JSR-105 verifier, which accepts every exclusive C14N case and rejects both inclusive ones. Two causes are known. `SignedXml` canonicalizes `SignedInfo` with namespaces propagated from the document element rather than from the real position of `SignedInfo` inside `wsse:Security`, so an independent verifier reconstructs different `SignedInfo` bytes and the signature value does not match; and setting a namespaced attribute through `SetAttribute(localName, namespaceUri, value)`, which is how the `mustUnderstand` and actor/role attributes are written, introduces a phantom default namespace declaration that inclusive canonicalization then carries into the signed bytes. That second cause is not the whole story on its own: the interop case that clears `MustUnderstand` is rejected too. `InclusiveC14N` stays in the enum for services that demand it and for the tests that pin this behaviour, but it will not interoperate. Exclusive C14N is the default and is the correct choice.

<a name="ws-security-modes"></a>
One `SoapSecurityDto` resolves to exactly one WS-Security mode, decided by which option groups are set, and a combination the library cannot build is refused by code before anything is signed. Nothing is ever downgraded: a request that asks for a mode it cannot have is not sent with a weaker header, it is not sent at all. The mode also decides the order of the children of the single `wsse:Security` header, which is the order a WCF host accepts:

<table>
	<thead>
		<tr>
			<th>Mode</th>
			<th>Selected by</th>
			<th>Security header children, in order</th>
		</tr>
	</thead>
	<tbody>
		<tr>
			<td><b>Asymmetric X.509</b></td>
			<td>`SigningCertificate` set, neither `SymmetricBinding` nor `SecureConversation`.</td>
			<td>`BinarySecurityToken`, `Timestamp`, `UsernameToken`, `saml:Assertion`, `Signature`. This is the classic signing described above; its wire is unchanged.</td>
		</tr>
		<tr>
			<td><b>Symmetric (encrypted key)</b></td>
			<td>`SymmetricBinding` set.</td>
			<td>`Timestamp`, `EncryptedKey`, the `DerivedKeyToken`s, `ReferenceList`, the encrypted `UsernameToken`, `BinarySecurityToken` and the endorsing `Signature` only with `EndorseWithSigningCertificate`, with the primary `Signature` before the endorsing one.</td>
		</tr>
		<tr>
			<td><b>Secure conversation</b></td>
			<td>`SecureConversation` set.</td>
			<td>`Timestamp`, `SecurityContextToken`, the `DerivedKeyToken`s, `ReferenceList`, the encrypted `UsernameToken`, `Signature`.</td>
		</tr>
		<tr>
			<td><b>Token-only</b></td>
			<td>`UsernameToken` or `SamlToken` set, nothing else.</td>
			<td>`Timestamp`, `UsernameToken`, `saml:Assertion`. No signature at all.</td>
		</tr>
	</tbody>
</table>

The WS-Addressing headers, when enabled, are SOAP headers next to `wsse:Security`, not inside it, and are covered by the signature in every mode that signs. The refusals a combination can meet, each a distinct code in `Messages[0].Key`:

-> `V-SEC-002` when `Enabled` is true but nothing selects a mode (no signing certificate, no token, no binding), or when the mode needs an RSA private key the signing certificate does not expose; <br/>
-> `V-SEC-003` for any `GET` request with security enabled; <br/>
-> `V-SEC-090` when `SymmetricBinding` and `SecureConversation` are both set; <br/>
-> `V-SEC-033` when a symmetric binding or a secure conversation has no `Addressing`; <br/>
-> `V-SEC-035` for a custom `Signer` or `ResponseVerifier`, and `V-SEC-036` for an `ExpectedResponseCertificate`, on a symmetric binding or a secure conversation, whose response is checked with derived keys rather than a certificate; <br/>
-> `V-SEC-051` for `Encryption` on the asymmetric or token-only mode, which carry no encryption key; <br/>
-> `V-SEC-088` for a SAML assertion on a symmetric binding or a secure conversation; <br/>
-> `V-SEC-091` for `UsernameToken.SignToken` on a token-only header, which has no signature to sign it with. <br/>

Only the built-in signer goes through this planning. A custom `ISoapMessageSigner` set on `Signer` is invoked as before, with whatever it makes of the options, except that it is refused outright (`V-SEC-035`) on the symmetric binding and the secure conversation, since the response to such a request can only be checked against keys the library derived itself.

<a name="username-token"></a>
`SoapSecurityDto.UsernameToken` (`SoapUsernameTokenDto`) carries a WS-Security Username Token Profile 1.0 token. On its own it produces a token-only header with no signature; next to a signing certificate it sits between the timestamp and the signature; on a symmetric binding or a secure conversation it is always signed and then encrypted in place, and asking for it in clear there (`Encryption.EncryptUsernameToken = false`) is refused with `V-SEC-038` rather than honoured.

<table>
	<thead>
		<tr>
			<th>Property</th>
			<th>Default</th>
			<th>Description</th>
		</tr>
	</thead>
	<tbody>
		<tr>
			<td><b>Username</b></td>
			<td>`null`</td>
			<td>Required; a token naming nobody is refused with `V-SEC-092`.</td>
		</tr>
		<tr>
			<td><b>Password</b></td>
			<td>`null`</td>
			<td>Required when `PasswordType` is `Digest` (`V-SEC-092` otherwise); a text token may omit it, in which case no `wsse:Password` element is written. The value is never copied into any failure message.</td>
		</tr>
		<tr>
			<td><b>PasswordType</b></td>
			<td>`SoapPasswordType.Text`</td>
			<td>`Text` sends the password as is. `Digest` sends `Base64(SHA-1(nonce + created + password))`, computed over the raw nonce bytes, the UTF-8 bytes of the `wsu:Created` text and the UTF-8 bytes of the password, exactly as the profile specifies.</td>
		</tr>
		<tr>
			<td><b>IncludeNonce</b></td>
			<td>`true`</td>
			<td>Whether a fresh 16-byte random `wsse:Nonce` is included. A digest token always carries one, whatever this says, because the digest is computed over it.</td>
		</tr>
		<tr>
			<td><b>IncludeCreated</b></td>
			<td>`true`</td>
			<td>Whether a UTC `wsu:Created` instant is included. A digest token always carries one.</td>
		</tr>
		<tr>
			<td><b>SignToken</b></td>
			<td>`false`</td>
			<td>Whether the token is covered by the message signature. Needs a signature to exist: on a token-only header it is refused with `V-SEC-091`.</td>
		</tr>
		<tr>
			<td><b>AllowTextPasswordOverInsecureTransport</b></td>
			<td>`false`</td>
			<td>A text password to an endpoint that is not `https` is the password itself to whoever reads the wire, so the build refuses it with `V-SEC-094` unless you opt in here, knowing the transport. Only the asymmetric and token-only modes are concerned, where the token travels in clear; a digest password, a null password and the symmetric family (which encrypts the token) are unaffected. The endpoint-less `ISoapMessageSigner.Sign(XElement, SoapSecurityDto)` path cannot know the transport and does not check.</td>
		</tr>
	</tbody>
</table>

A username or password carrying a character XML cannot represent is refused with `V-SEC-093`, without repeating the value.

```csharp
var security = new SoapSecurityDto
{
    UsernameToken = new SoapUsernameTokenDto
    {
        Username = "alice",
        Password = "s3cret",
        PasswordType = SoapPasswordType.Digest
    }
};
```

Two service-side observations worth having before you pick a password type. A WCF `UserNameOverTransport` endpoint accepts `Text` only: a `Digest` token is refused with `wsse:InvalidSecurity` before any validator runs, so WCF is a text-over-`https` case, while the ASMX host in this repository verifies the digest end to end. And `MustUnderstand` defaults to `true` on the Security header, which on ASMX means the service has to claim the header: a method that does not (no `SoapExtension` marking it understood) faults `soap:MustUnderstand` before the method runs.

<a name="ws-addressing"></a>
`SoapSecurityDto.Addressing` (`SoapAddressingDto`) adds `wsa:Action`, `wsa:MessageID`, `wsa:ReplyTo` and `wsa:To` as SOAP headers. Each is stamped with a `wsu:Id` and covered by the message signature in every mode that signs, and a symmetric binding or a secure conversation requires them (`V-SEC-033`), because a WCF `wsHttpBinding` rejects a symmetric-bound message that does not carry them and because the signed `wsa:MessageID` is what the reply is later bound to.

<table>
	<thead>
		<tr>
			<th>Property</th>
			<th>Default</th>
			<th>Description</th>
		</tr>
	</thead>
	<tbody>
		<tr>
			<td><b>To</b></td>
			<td>`null`</td>
			<td>The absolute destination written into `wsa:To`, or null to use the endpoint the request is built for. With neither available the build fails with `V-SEC-022`.</td>
		</tr>
		<tr>
			<td><b>Action</b></td>
			<td>`null`</td>
			<td>The `wsa:Action`. The request's own action (the `action` argument of `BuildRequest`, which becomes the `SOAPAction` header) and this value are one source: when only one is supplied it is used for both, when both are supplied they must be equal, and two different values are refused with `V-SEC-020`. With neither, `V-SEC-021`, since `wsa:Action` is mandatory.</td>
		</tr>
		<tr>
			<td><b>IncludeMessageId</b></td>
			<td>`true`</td>
			<td>Whether a fresh `wsa:MessageID` (`urn:uuid:...`) is generated. When present, a response checked against the sent request must carry a signed `wsa:RelatesTo` equal to it (`V-SEC-023` otherwise).</td>
		</tr>
		<tr>
			<td><b>IncludeReplyTo</b></td>
			<td>`true`</td>
			<td>Whether a `wsa:ReplyTo` naming the anonymous address is included.</td>
		</tr>
		<tr>
			<td><b>Version</b></td>
			<td>`SoapAddressingVersionType.WsAddressing10`</td>
			<td>The addressing namespace the headers are written in. Also `WsAddressingAugust2004`.</td>
		</tr>
	</tbody>
</table>

<a name="symmetric-binding"></a>
`SoapSecurityDto.SymmetricBinding` (`SoapSymmetricBindingDto`) selects the WS-Security 1.1 symmetric binding a WCF `wsHttpBinding` in Message security mode speaks: a fresh secret is wrapped for the service certificate in an `xenc:EncryptedKey`, a signature key and an encryption key are derived from it through `DerivedKeyToken`s (P_SHA1), the message is signed with HMAC under the signature key, and the response is expected to be signed, and where applicable encrypted, with keys derived from the same secret. The response therefore cannot be checked against a certificate; it is checked against the request that was sent, see <a href="#response-security">Response security</a>.

<table>
	<thead>
		<tr>
			<th>Property</th>
			<th>Default</th>
			<th>Description</th>
		</tr>
	</thead>
	<tbody>
		<tr>
			<td><b>ServiceCertificate</b></td>
			<td>`null`</td>
			<td>The service's certificate the secret is wrapped for; only its public key is used. Required, with an RSA public key (`V-SEC-039`), and pinned by you: the library never discovers it from the service. It must not share its public key with `SigningCertificate` (`V-SEC-037`), otherwise the client could answer itself.</td>
		</tr>
		<tr>
			<td><b>ServiceKeyIdentifier</b></td>
			<td>`SoapServiceKeyIdentifierType.ThumbprintSha1`</td>
			<td>How the service certificate is referenced inside the `EncryptedKey`. Only the SHA-1 thumbprint reference is emitted in this version; `SubjectKeyIdentifier`, `IssuerSerial` and `BinarySecurityToken` are refused with `V-SEC-045`.</td>
		</tr>
		<tr>
			<td><b>Version</b></td>
			<td>`SoapSecureConversationVersionType.February2005`</td>
			<td>The WS-SecureConversation namespace the derived key tokens are written in. `February2005` is what `wsHttpBinding` speaks, `December2005` what `ws2007HttpBinding` speaks.</td>
		</tr>
		<tr>
			<td><b>SignatureAlgorithm</b></td>
			<td>`SoapSymmetricSignatureAlgorithmType.HmacSha256`</td>
			<td>The HMAC the primary signature is computed with. `HmacSha1` is only accepted when `ResponseVerificationPolicy.AllowSha1Algorithms` is on (`V-SEC-044`), so that the request is never signed with an algorithm its own response check would reject.</td>
		</tr>
		<tr>
			<td><b>SignatureKeyLength</b></td>
			<td>`24`</td>
			<td>Bytes derived for the signature key; WCF derives 24 for every `Basic*` suite. Accepted range 16 to 64 (`V-SEC-043`).</td>
		</tr>
		<tr>
			<td><b>EncryptionKeyLength</b></td>
			<td>`32`</td>
			<td>Bytes derived for the encryption key; 16, 24 or 32 (`V-SEC-043`), and it must match `Encryption.DataAlgorithm` (`V-SEC-056`): 32 for AES-256, 24 for AES-192, 16 for AES-128.</td>
		</tr>
		<tr>
			<td><b>EndorseWithSigningCertificate</b></td>
			<td>`false`</td>
			<td>Whether the primary signature is endorsed by a second, RSA signature computed with `SigningCertificate`, whose single reference is the primary signature. This is the shape a WCF `MutualCertificate` (client credential `Certificate`) binding expects; the certificate then travels as a `BinarySecurityToken` and is the identity the service sees. Without it `SigningCertificate` is not used at all in this mode.</td>
		</tr>
		<tr>
			<td><b>KeyDerivationLabel</b></td>
			<td>`null`</td>
			<td>The label the keys are derived with, or null for the specification's default label.</td>
		</tr>
	</tbody>
</table>

Two request shapes are proven against a real WCF host in this repository's `SoapClientCallAssistTests.Wcf` suite, for both `Version` values and both SOAP protocols:

```csharp
// Client credential "Certificate": the secret is wrapped for the service, the client certificate endorses.
var certificateSecurity = new SoapSecurityDto
{
    SigningCertificate = clientCertificate,
    SymmetricBinding = new SoapSymmetricBindingDto
    {
        ServiceCertificate = serviceCertificate,   // public key only, pinned by you
        Version = SoapSecureConversationVersionType.February2005,
        EndorseWithSigningCertificate = true
    },
    Addressing = new SoapAddressingDto()
};

// Client credential "UserName": the token is signed and encrypted in place; no client certificate.
var userNameSecurity = new SoapSecurityDto
{
    UsernameToken = new SoapUsernameTokenDto { Username = "alice", Password = "s3cret" },
    SymmetricBinding = new SoapSymmetricBindingDto
    {
        ServiceCertificate = serviceCertificate,
        Version = SoapSecureConversationVersionType.February2005
    },
    Addressing = new SoapAddressingDto(),
    ResponseSecurity = new SoapResponseSecurityDto { RequireSignatureConfirmation = false }
};
```

The `RequireSignatureConfirmation = false` on the username shape is not optional: a WCF `UserNameForCertificate` reply carries no `wsse11:SignatureConfirmation`, while a `MutualCertificate` reply carries one per request signature, and the symmetric default is to require it. The service side has to be configured to match what the library emits, because there is no negotiation:

-> `negotiateServiceCredential="false"`: the `wsHttpBinding` default of `true` runs a WS-Trust SPNEGO/TLS negotiation the library does not implement; the service certificate is pinned on the client instead; <br/>
-> `establishSecurityContext="false"` for one encrypted key per request, or `true` for a session, which is the secure conversation mode below; <br/>
-> `algorithmSuite="Basic256Sha256"` for the SHA-256 defaults; the WCF default `Basic256` is a SHA-1 suite and needs `AllowSha1Algorithms` plus `HmacSha1` and `Sha1` on the client; <br/>
-> WS-Security 1.1 (`messageVersion` with `WSSecurity11`), which is what both `wsHttpBinding` and `ws2007HttpBinding` use in Message mode. <br/>

What the contract's protection level asks for decides whether you need encryption at all. A contract at `ProtectionLevel.Sign` accepts a signed, unencrypted `Body` and answers in kind, so the shapes above are complete. The default protection level, `EncryptAndSign`, expects the `Body` encrypted and answers with an encrypted `Body`, so you add `Encryption.EncryptBody = true`, which in turn requires `ResponseSecurity.AllowDecryption = true` (`V-SEC-052` otherwise) and a response read through `DecryptAndVerify` rather than `Verify`:

```csharp
var encryptedBodySecurity = new SoapSecurityDto
{
    SigningCertificate = clientCertificate,
    SymmetricBinding = new SoapSymmetricBindingDto
    {
        ServiceCertificate = serviceCertificate,
        EndorseWithSigningCertificate = true
    },
    Addressing = new SoapAddressingDto(),
    Encryption = new SoapEncryptionDto { EncryptBody = true, EncryptSignature = true },
    ResponseSecurity = new SoapResponseSecurityDto { AllowDecryption = true }
};
```

Not supported, and refused or simply not offered rather than approximated: the `wsHttpBinding` default of `negotiateServiceCredential="true"`, the WS-Security 1.0 asymmetric binding of `wsHttpBinding`, Windows (Kerberos/NTLM) message credentials, session renewal, and any service key reference other than the SHA-1 thumbprint. A SAML assertion on the symmetric binding is refused with `V-SEC-088`.

`SoapSecurityDto.Encryption` (`SoapEncryptionDto`) says which parts of a symmetric-bound request are encrypted and with what. It requires the symmetric binding or a secure conversation (`V-SEC-051`), since the asymmetric binding carries no encryption key. The order is always sign-then-encrypt, which is what WCF requires: a fresh IV per encrypted element, and a fresh secret per build.

<table>
	<thead>
		<tr>
			<th>Property</th>
			<th>Default</th>
			<th>Description</th>
		</tr>
	</thead>
	<tbody>
		<tr>
			<td><b>EncryptUsernameToken</b></td>
			<td>`true`</td>
			<td>Whether the `UsernameToken` is encrypted. On the symmetric family it always is, and `false` is refused with `V-SEC-038`, because the password would otherwise travel in clear inside a message that claims to be protected.</td>
		</tr>
		<tr>
			<td><b>EncryptBody</b></td>
			<td>`false`</td>
			<td>Whether the SOAP `Body` is encrypted (`xenc:EncryptedData` of type `#Content`). A service that receives an encrypted `Body` answers with one, so this requires `ResponseSecurity.AllowDecryption` (`V-SEC-052`), and a `ServiceCertificate` fit to encrypt for: an RSA key of at least 2048 bits, valid now within the clock skew, permitting key encipherment when a `KeyUsage` extension is present (`V-SEC-057`).</td>
		</tr>
		<tr>
			<td><b>EncryptSignature</b></td>
			<td>`false`</td>
			<td>Whether the primary signature is encrypted after it is computed (WCF's `SignBeforeEncryptAndEncryptSignature`).</td>
		</tr>
		<tr>
			<td><b>DataAlgorithm</b></td>
			<td>`SoapDataEncryptionAlgorithmType.Aes256Cbc`</td>
			<td>The block cipher. Also `Aes192Cbc` and `Aes128Cbc`; `SymmetricBinding.EncryptionKeyLength` must match it.</td>
		</tr>
		<tr>
			<td><b>KeyWrap</b></td>
			<td>`SoapKeyWrapAlgorithmType.RsaOaepMgf1pSha1`</td>
			<td>The RSA key transport the secret is wrapped with. RSA-OAEP with MGF1/SHA-1 is key transport, not a signature or digest, and is not gated by `AllowSha1Algorithms`. The only other value, `Rsa15` (PKCS#1 v1.5), is open to padding-oracle attacks and is refused with `V-SEC-055`.</td>
		</tr>
	</tbody>
</table>

<a name="response-security"></a>
Checking a response is done through `ISoapResponseSecurity`, a standalone service registered by `RegisterSoapClientsEndpoint()` with `TryAddSingleton` (implementation `WsSecurityResponseSecurity`, in `SoapClientCallAssist.Security`), so a caller who holds an `ISoapClientEndpoint` never has to downcast. Its constructor taking an `ISoapMessageVerifier` is the one the container picks once you register a verifier, so a registered verifier is honoured here too. Nothing is automatic: a response is checked only when one of these methods is called.

-> `IResult<SoapSignatureVerificationResult> Verify(HttpRequestMessage sentRequest, string soapResponse)`, the request-bound check; <br/>
-> `IResult<SoapSignatureVerificationResult> Verify(string soapResponse, X509Certificate2 expectedCertificate, SoapVerificationPolicyDto policy)`; <br/>
-> `IResult<SoapSignatureVerificationResult> Verify(string soapResponse, SoapSecurityDto security)`; <br/>
-> `IResult<string> DecryptAndVerify(HttpRequestMessage sentRequest, string soapResponse)`, with `Decrypt(HttpRequestMessage, string)` as an alias of the same operation. <br/>

The two overloads that take a certificate or a `SoapSecurityDto` are the ones `VerifyResponseSignature` on the clients forward to. They establish that the response was signed by the expected certificate under the policy, and nothing about which request it answers; they are for the asymmetric binding only, and options that select a symmetric binding or a secure conversation are refused with `V-SEC-032`, naming the overload to use instead.

The request-bound overloads are different in kind. When a request is built with the built-in signer, the build mints a one-time snapshot of everything the check needs (the mode, the policy, the expected certificate's public part, every nonce generated, the signature value, the `wsa:MessageID`, and on the symmetric family the derived-key material) and stores it on the `HttpRequestMessage` under `HttpRequestMessage.Properties[SoapClientEndpointExtensions.RequestSecurityStateKey]`. `Verify(sentRequest, response)` reads that snapshot, decides from it whether to check against a certificate or against the request's own derived keys (the response's `KeyInfo` never gets a say), and then binds the response to the request: a signed `wsa:RelatesTo` equal to the request's `wsa:MessageID` (`V-SEC-023`), a signed WS-Security 1.1 `SignatureConfirmation` equal to the request's signature value where required (`V-SEC-040`), and refusals of a response that reflects one of the request's own nonces (`V-SEC-041`) or carries the request's own signature value (`V-SEC-042`).

```csharp
public async Task CallSymmetric(ISoapClientEndpoint client, ISoapResponseSecurity responseSecurity, SoapSecurityDto security)
{
    var built = client.BuildRequest(
        HttpMethod.Post,
        new BuildSoapRequestDto
        {
            Client = new HttpClientDto(_baseUri),
            Envelope = new SoapEnvelopeDto(new List<XElement> { new XElement(_ns.GetName("WhoAmI")) }, action: _action),
            Security = security
        });

    if (built.IsSuccess == false)
    {
        return;
    }

    var sent = await client.SendRequestAsync(built.Response);
    if (sent.IsSuccess == false)
    {
        sent.Response?.Dispose();
        return;
    }

    var wire = await sent.Response.Content.ReadAsStringAsync();

    // Tier A (ProtectionLevel.Sign): the response is in clear, verify it against the request that was sent.
    var verified = responseSecurity.Verify(built.Response, wire);

    // Tier B (EncryptAndSign): decrypt and verify in one operation; the plaintext is handed out only if the signature verifies.
    var plaintext = responseSecurity.DecryptAndVerify(built.Response, wire);   // IResult<string>
}
```

Things about the snapshot you have to know:

-> It is single-use. The first `Verify(sentRequest, ...)` or `DecryptAndVerify` consumes it, whether it succeeds or fails, and zeroes its byte arrays when done; a second call on the same request is refused with `V-SEC-031`. A request built without message security, with a custom signer, or by anything other than `BuildRequest`, is refused with `V-SEC-030`. That is what makes a decrypt-before-verify oracle structurally impossible: nothing can use the key twice. <br/>
-> It is not cleared by disposing the request. `HttpRequestMessage.Dispose()` leaves `Properties` alone, so the material of a request that is never checked lives until the request is collected. Check every secured request, or at least do not keep unchecked ones around. <br/>
-> It is visible to every handler the request passes through (a retry policy that clones the request copies its properties too), but it exposes nothing: no getters, and a `ToString()` that returns a constant. A retry that rebuilds the request mints fresh material by construction. <br/>
-> On a secure conversation, a session disposed between the build and the check makes the response key underivable, refused with `V-SEC-073`. <br/>

`DecryptAndVerify` is one atomic operation: the encrypted parts are located structurally from the single `xenc:ReferenceList`, decrypted under the request's own derived keys (never through `EncryptedXml`, never with a key the response supplies), the plaintext is re-inserted, the signature over the plaintext `Body` is verified, and the plaintext envelope is returned only when that verification succeeded. Every check that can be decided from the structure alone runs first, under its own code, before any key is derived: a response carrying its own `EncryptedKey`, a `RetrievalMethod`, a `CarriedKeyName`, a `CipherReference` or a certificate reference, or encrypted parts in any shape other than the one the library decrypts (the `Body`'s only content, or Security header elements that decrypt to the `Signature` or a `SignatureConfirmation`), is refused with `V-SEC-059`; a `DerivedKeyToken` keyed to an `EncryptedKey` other than the request's own with `V-SEC-047`; cipher text that could not fit under the plaintext cap with `V-SEC-054`. The library has no RSA-decrypt path at all: it never unwraps a key wrapped for its own certificate, so a response keyed by anything but the request's own secret cannot be read. Once a key has been derived, every failure, in the derived key, the padding, the plaintext, its size, its parse, its content or the signature over it, is reported under the one code `ER-SEC-DEC` with no exception attached and no plaintext, and every stage still runs to its end, so the outcome tells an observer nothing about where it failed. Treat `ER-SEC-DEC` as terminal for that exchange: never retry it automatically, and never surface it to the remote party. `SoapVerificationPolicyDto.DiagnosticDetail` names the stage for diagnosis only.

`Verify(sentRequest, ...)` never decrypts. A response that carries encrypted content is refused with `V-SEC-058` when checked through `Verify`, or when the request was built without `AllowDecryption`, and the cipher text is not surfaced; `DecryptAndVerify` on a request built on the asymmetric binding is refused with `V-SEC-053`.

`SoapSecurityDto.ResponseSecurity` (`SoapResponseSecurityDto`) says what the response may and must carry when it is checked against the sent request. It is read at build time and snapshotted, so changing it after the build changes nothing.

<table>
	<thead>
		<tr>
			<th>Property</th>
			<th>Default</th>
			<th>Description</th>
		</tr>
	</thead>
	<tbody>
		<tr>
			<td><b>AllowDecryption</b></td>
			<td>`false`</td>
			<td>Whether an encrypted response may be decrypted. Off, so a response is never decrypted unless you asked for it (an encrypted one is then refused with `V-SEC-058`); required whenever `Encryption.EncryptBody` is set. Only accepted on the symmetric family, the modes that carry a decryption key (`V-SEC-053`).</td>
		</tr>
		<tr>
			<td><b>MaxPlaintextBytes</b></td>
			<td>`null` (4 MiB)</td>
			<td>The largest decrypted plaintext accepted. A response whose cipher text could not fit under it is refused before anything is decrypted, one whose plaintext exceeds it before that plaintext is parsed. A value not greater than zero is refused when the request is built (`V-SEC-054`).</td>
		</tr>
		<tr>
			<td><b>RequireSignatureConfirmation</b></td>
			<td>`null`</td>
			<td>A `bool?`. Whether the response must carry a signed `wsse11:SignatureConfirmation` echoing the request's signature value. Null takes the mode's default: required on the symmetric binding and the secure conversation, where the confirmation is what binds the reply to the request, not required on the asymmetric binding, where a service may not implement WS-Security 1.1 at all.</td>
		</tr>
		<tr>
			<td><b>AllowUnboundResponse</b></td>
			<td>`false`</td>
			<td>The request-bound `Verify` refuses with `V-SEC-024` a check it cannot bind: a request that carries no `wsa:MessageID` and requires no `SignatureConfirmation` leaves nothing to tie the reply to, so any response the service signed inside the timestamp window would pass as its answer. Set this to accept such a response deliberately, knowing the service; it has no effect on the certificate and options overloads, which are not bound to a request in the first place.</td>
		</tr>
	</tbody>
</table>

<a name="saml"></a>
`SoapSecurityDto.SamlToken` (`SoapSamlTokenDto`) carries a SAML 1.1 or SAML 2.0 assertion that an issuer produced elsewhere. The library carries it only: it never validates the issuer's signature over the assertion, because it holds no issuer key. It refuses an assertion no receiver could accept or that the envelope could not reference unambiguously, and on a holder-of-key confirmation it proves possession of the key the assertion names by signing the message with `SigningCertificate`.

<table>
	<thead>
		<tr>
			<th>Property</th>
			<th>Default</th>
			<th>Description</th>
		</tr>
	</thead>
	<tbody>
		<tr>
			<td><b>Assertion</b></td>
			<td>`null`</td>
			<td>The `saml:Assertion` element itself, as an <b>`XmlElement`</b>, required (`V-SEC-081`). Pass the issuer's element directly, loaded through an `XmlDocument` with `PreserveWhitespace = true`: an issued assertion carries its own enveloped signature whose digest covered the assertion's whitespace, and `XElement.Parse` drops that whitespace, so an assertion that went through LINQ to XML no longer verifies at the service. It is imported node for node, whitespace included. An envelope, a Security header or anything else wrapping the assertion is refused with `V-SEC-084`; an assertion without a non-blank `ID` (SAML 2.0) or `AssertionID` (SAML 1.1) with `V-SEC-085`; an identifier that also names another element of the built envelope with `V-SEC-086`.</td>
		</tr>
		<tr>
			<td><b>Confirmation</b></td>
			<td>`SoapSamlConfirmationType.Bearer`</td>
			<td>`Bearer` carries the assertion and signs nothing on its account. `HolderOfKey` requires `SigningCertificate` (`V-SEC-082`): the primary signature's `KeyInfo` then points at the assertion through a `SecurityTokenReference` with a SAML key identifier, and no `BinarySecurityToken` is emitted, since the assertion names the key.</td>
		</tr>
		<tr>
			<td><b>SignAssertion</b></td>
			<td>`true`</td>
			<td>Whether a holder-of-key assertion is covered by the message signature, by a reference to its own identifier. Most receivers accept or expect that. A WCF service on the issued-token-over-transport binding cannot resolve a reference to the assertion and rejects the message: for WCF set this to `false`, and `SignBody = false` too, so that the signature covers the timestamp alone. No effect on a bearer confirmation.</td>
		</tr>
		<tr>
			<td><b>AllowBearerOverInsecureTransport</b></td>
			<td>`false`</td>
			<td>A bearer assertion authenticates whoever presents it, so one read off an unprotected wire can be replayed by anyone; a bearer assertion to an endpoint that is not `https` is refused with `V-SEC-083` unless you opt in here. No effect on holder-of-key, and none on the endpoint-less `ISoapMessageSigner.Sign` path.</td>
		</tr>
		<tr>
			<td><b>ClockSkew</b></td>
			<td>5 minutes</td>
			<td>The tolerance applied when the assertion's validity is checked before it is carried: an assertion whose `NotOnOrAfter` (in its conditions, or on SAML 2.0 in any subject confirmation) lies at or before now less this skew is refused with `V-SEC-087`, because every receiver would reject it. A `NotBefore` in the future is never refused; the receiver applies its own tolerance to that end. A negative value counts as none.</td>
		</tr>
	</tbody>
</table>

```csharp
var document = new XmlDocument { PreserveWhitespace = true };
document.LoadXml(issuedAssertionXml);

var security = new SoapSecurityDto
{
    SigningCertificate = holderOfKeyCertificate,
    SignBody = false,                       // WCF issued-token-over-transport: the signature covers the timestamp alone
    SamlToken = new SoapSamlTokenDto
    {
        Assertion = document.DocumentElement,
        Confirmation = SoapSamlConfirmationType.HolderOfKey,
        SignAssertion = false               // WCF cannot resolve a reference to the assertion
    }
};
```

A SAML assertion on the symmetric binding or in a secure conversation is refused with `V-SEC-088`; carry it with a signing certificate or on its own. Observed WCF sub-codes for the negatives, from the real WCF host in this repository's test suite: an issuer the service does not know is `wsse:InvalidSecurityToken`; a wrong audience, an expired assertion or an unsigned one is `wsse:FailedAuthentication`; a tampered assertion or a holder-of-key message signed with a key other than the one the assertion names is `wsse:InvalidSecurity`.

<a name="secure-conversation"></a>
WS-SecureConversation lets many application messages be keyed by one established session instead of a fresh encrypted key per request, which is what a WCF binding with `establishSecurityContext="true"` (the `wsHttpBinding` default) expects. The session is issued and cancelled through `SoapSecureConversationClient` (`SoapClientCallAssist.Security`), a public, stateless class registered by `RegisterSoapClientsEndpoint()` with `TryAddSingleton`; its parameterless constructor uses the library's shared fallback factory, the other one takes `(IHttpClientFactory, ISoapResponseSecurity)`.

-> `Task<IResult<SoapSecureConversationSession>> IssueAsync(Uri endpoint, SoapSecurityDto bootstrap, CancellationToken cancellationToken = default, SoapProtocolType protocol = SoapProtocolType.SOAP_1_2)`; <br/>
-> `Task<IResult> CancelAsync(Uri endpoint, SoapSecureConversationSession session, CancellationToken cancellationToken = default, SoapProtocolType protocol = SoapProtocolType.SOAP_1_2)`. <br/>

`IssueAsync` builds a `wst:RequestSecurityToken` (issue, symmetric key, 256-bit key size, 32 bytes of client entropy, P_SHA1 computed key), sends it under the bootstrap's symmetric binding with the body and the signature encrypted and the reply decryptable, decrypts and verifies the reply atomically through `ISoapResponseSecurity.DecryptAndVerify` (which also binds it by `wsa:RelatesTo`), validates the `RequestSecurityTokenResponse`, and computes the session secret as `P_SHA1(clientEntropy, serverEntropy)`. The `bootstrap` is the symmetric `SoapSecurityDto` you would use for a one-shot call, and it must carry a `SymmetricBinding` (`V-SEC-033` otherwise; `V-SEC-022` for an endpoint that is not absolute). The client copies `SigningCertificate`, `UsernameToken`, `SymmetricBinding`, `MustUnderstand` and `ResponseVerificationPolicy` from it and sets body encryption, reply decryption and the WS-Addressing headers with the WS-Trust `.../RST/SCT` action itself (keeping only your `Addressing.Version`), so you do not set `Encryption`, `ResponseSecurity` or `Addressing` on the bootstrap, and the bootstrap itself is never mutated. The certificate bootstrap is proven against a real WCF host in this repository for both `Version` values (SOAP 1.1 with `February2005`, SOAP 1.2 with `December2005`); the username bootstrap goes through the same exchange, with the token signed and encrypted, but has no WCF acceptance test of its own here. WCF answers only the secure-conversation dialect of the issue action (`.../RST/SCT`), not the generic `.../RST/Issue`.

```csharp
public async Task CallUnderASession(ISoapClientEndpoint client, 
	SoapSecureConversationClient conversation, ISoapResponseSecurity responseSecurity)
{
    var bootstrap = new SoapSecurityDto
    {
        SigningCertificate = clientCertificate,
        SymmetricBinding = new SoapSymmetricBindingDto
        {
            ServiceCertificate = serviceCertificate,
            Version = SoapSecureConversationVersionType.February2005,
            EndorseWithSigningCertificate = true
        }
    };

    var issued = await conversation.IssueAsync(_baseUri, bootstrap);
    if (issued.IsSuccess == false)
    {
        return;   // V-SEC-066 .. V-SEC-070 and V-SEC-072 for a reply the client refused, ER-SEC-DEC for one it could not decrypt
    }

    using (var session = issued.Response)
    {
        var security = new SoapSecurityDto
        {
            SecureConversation = new SoapSecureConversationDto { Session = session },
            Addressing = new SoapAddressingDto(),
            Encryption = new SoapEncryptionDto { EncryptBody = true, EncryptSignature = true },
            ResponseSecurity = new SoapResponseSecurityDto { AllowDecryption = true, RequireSignatureConfirmation = false }
        };

        var built = client.BuildRequest(
            HttpMethod.Post,
            new BuildSoapRequestDto
            {
                Client = new HttpClientDto(_baseUri),
                Envelope = new SoapEnvelopeDto(new List<XElement> { new XElement(_ns.GetName("WhoAmI")) }, action: _action),
                Security = security
            });

        var sent = await client.SendRequestAsync(built.Response);
        var plaintext = responseSecurity.DecryptAndVerify(built.Response, await sent.Response.Content.ReadAsStringAsync());

        await conversation.CancelAsync(_baseUri, session);   // terminal: the session is disposed whatever the service says
    }
}
```

`SoapSecureConversationSession` (`IDisposable`) is what the caller owns: `ContextIdentifier` (what the `SecurityContextToken` names), `ExpiresUtc`, `Version`, `IsExpired` and `IsDisposed`. It is immutable after issue, the secret is copied in, never exposed (`ToString()` returns a constant) and zeroed on `Dispose`, and it is safe to share between threads for as many requests as you like: pass it per request through `SoapSecurityDto.SecureConversation.Session`. The refusals around it: no session `V-SEC-061`; an expired session at build time `V-SEC-062`, applied one minute before `ExpiresUtc` so that a message never arrives with a context the service has already dropped; a disposed session, or one holding no secret, `V-SEC-063`; disposed between the build and the response check `V-SEC-073`. There is no renewal in this version: when a session expires, issue a new one.

`CancelAsync` keys a `wst:RequestSecurityToken` of type `Cancel`, whose `CancelTarget` references the session's context token, by the session itself, and is terminal on any outcome: the session is disposed and its secret zeroed before the call returns, even when the endpoint is invalid, the session was already expired (`V-SEC-063`) or the service refused the cancel, so a cancelled context can never key another request. The result reports whether the service accepted the cancel; the cancel reply is not decrypted or verified.

The `RequestSecurityTokenResponse` is validated before a session is handed out, each refusal by code: not a single RSTR for a security context token in the requested WS-Trust version `V-SEC-072`; a `RequestedProofToken` that carries a `BinarySecret` the service chose alone, or any computed-key algorithm other than P_SHA1, `V-SEC-066`, because the session key must be computed from both entropies; service entropy missing, shorter than 32 bytes or all zero `V-SEC-067`; a `KeySize` other than the 256 bits requested `V-SEC-068`; no `SecurityContextToken` with a single non-empty `Identifier` `V-SEC-069`; no readable `Lifetime/Expires` `V-SEC-070`.

<a name="http-transport"></a>
Every request from both clients goes out through one named `HttpClient`, registered by `RegisterSoapClientsEndpoint()` under `SoapClientEndpointExtensions.SoapHttpClientName`. That name is how you attach a client certificate, a proxy, a delegating handler or a retry policy to the library's sends, which is the usual companion to message signing; nothing on the clients themselves takes a handler.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddHttpClient(SoapClientEndpointExtensions.SoapHttpClientName)
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler
            {
                Proxy = new WebProxy("http://proxy.local:8080"),
                UseProxy = true
            };
            handler.ClientCertificates.Add(_clientCertificate);

            return handler;
        });

    services.RegisterSoapClientsEndpoint();
}
```

`RegisterSoapClientsEndpoint()` no longer replaces the primary handler configured on that name; it augments whatever `HttpClientHandler` the named client ends up with by turning on `Deflate | GZip` response decompression. Previously the library assigned a handler of its own, which silently discarded the client certificate and proxy configured before it: mTLS failed loudly, and certificate pinning failed open. Two caveats follow from `IHttpClientFactory` applying its configuration in registration order:

-> A `ConfigurePrimaryHttpMessageHandler(...)` call made <i>after</i> `RegisterSoapClientsEndpoint()` still replaces the handler the library has already augmented, and you then have to enable decompression on it yourself (`AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip`). Configure the name first, as above, and the augmentation lands on your handler. <br/>
-> A consumer who deliberately set `AutomaticDecompression = DecompressionMethods.None` gets `Deflate | GZip` OR'd back on, because the augmentation cannot tell an untouched default from a considered choice. Only an `HttpClientHandler` that supports automatic decompression is touched; any other handler type is left exactly as you configured it. <br/>

<a name="transport-authentication"></a>
Transport authentication has no API of its own in the library. Authenticating the HTTP connection, as opposed to the message, is the handler's job, so it is configured exactly as above: `services.AddHttpClient(SoapClientEndpointExtensions.SoapHttpClientName).ConfigurePrimaryHttpMessageHandler(...)` before `RegisterSoapClientsEndpoint()`. What the library adds is the send contract: a 401 is reported as `ER-BEC-HTTP-401` naming the `WWW-Authenticate` scheme and realm, a 403 as `ER-BEC-HTTP-403`, and the response is carried in the result. Two shapes are exercised against a real IIS Express host in this repository's legacy test suite.

Mutual TLS, a client certificate on the handshake:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // A persisted key is required on Windows: the handshake proves possession through the platform's key store.
    var clientCertificate = new X509Certificate2(pfxPath, pfxPassword, X509KeyStorageFlags.UserKeySet);

    services.AddHttpClient(SoapClientEndpointExtensions.SoapHttpClientName)
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback = ServerCertificateIsPinned,
                AllowAutoRedirect = false
            };
            handler.ClientCertificates.Add(clientCertificate);

            return handler;
        });

    services.RegisterSoapClientsEndpoint();
}

private static bool ServerCertificateIsPinned(HttpRequestMessage request, 
	X509Certificate2 certificate, X509Chain chain, SslPolicyErrors errors)
{
    if (certificate == null)
    {
        return false;
    }

    using (var sha256 = SHA256.Create())
    {
        var presented = BitConverter.ToString(sha256.ComputeHash(certificate.RawData)).Replace("-", string.Empty);

        return string.Equals(presented, PinnedServerCertificateSha256, StringComparison.OrdinalIgnoreCase);
    }
}
```

Pin the server by a SHA-256 over `RawData`, as above, or leave the platform chain validation in place; never install a callback that returns `true` for everything, which turns mTLS into a connection to whoever answers. On Windows a certificate loaded with an ephemeral key cannot be used for a TLS client handshake; load the PFX with `UserKeySet` (or `MachineKeySet`), which persists the key, and remember that the persisted key outlives the process.

Windows authentication, Negotiate/NTLM with the process identity:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    var exactOrigin = new Uri("https://soap.example.local/");

    services.AddHttpClient(SoapClientEndpointExtensions.SoapHttpClientName)
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            Credentials = new CredentialCache { { exactOrigin, "Negotiate", CredentialCache.DefaultNetworkCredentials } },
            UseDefaultCredentials = false,
            AllowAutoRedirect = false
        });

    services.RegisterSoapClientsEndpoint();
}
```

Hand the credentials over through a `CredentialCache` keyed on the exact base URI of the service, never through `UseDefaultCredentials = true`: a handler with default credentials answers any host's challenge with the process identity, including a host a redirect took the request to, which is also why `AllowAutoRedirect` is off. Credentials cached for one origin are sent to that origin only; a challenge from anywhere else is left unanswered and reported as `ER-BEC-HTTP-401`.

**Kerberos is not provided.** The machine model this library is developed and tested on has no domain, so what is supported and exercised is NTLM through the Negotiate scheme with the process identity; nothing here obtains or validates a Kerberos ticket, and nothing asserts one. One named client also means one process-wide identity: every request the library sends goes out under the credentials configured on that handler, not per caller. And the library does not refuse Windows authentication over plain `http:`; a test in this repository pins that the NTLM exchange is carried over `http` when that is the origin the credentials were cached for, so the transport is your decision.

<a name="package-security-note"></a>
One note on the `System.Security.Cryptography.Xml` dependency, which does the XML signature work. The library references version 10.0.11 of the NuGet package, above every published advisory for it. That protects consumers on .NET Core and .NET 5 and later, who load the package's assembly. A consumer on .NET Framework is different: there the netstandard2.0 build of `System.Security.Cryptography.Xml` type-forwards to the in-box `System.Security.dll`, so the package version provides no protection on that platform and the Windows patch level does. Keep the host patched.

<a name="test-environment"></a>
The repository carries three MSTest suites and a console sample. Each suite is marked `[assembly: DoNotParallelize]` because it owns a single shared, statically addressed service instance for the whole run:

-> `src/tests/SoapClientCallAssistTests.Soap12` (net8.0), the largest suite: mapper, WS-Security wire goldens, planner refusals, symmetric key derivation known-answer vectors, response decryption, and cross-stack Java interop. It hosts `src/tests/SoapTestService` in-process, so it needs no external server for most tests. <br/>
-> `src/tests/SoapClientCallAssistTests.Wcf` (net48), which self-hosts a real WCF `ServiceHost` at `http://localhost:80/Temporary_Listen_Addresses/<guid>/`, the URL prefix Windows reserves for non-elevated listeners, so it runs without elevation. This is where the symmetric binding (both credentials, both versions, Sign and EncryptAndSign contracts), secure conversation, and the SAML bearer and holder-of-key shapes are proven against WCF itself. <br/>
-> `src/tests/SoapClientCallAssistTests` (net5.0), the legacy suite, which hosts `src/tests/TestSoapServiceN45` (a .NET Framework 4.5 ASMX/WCF web project) in IIS Express at `http://localhost:44338`, with an `https` site and Windows-authenticated and client-certificate endpoints next to it. Its secured endpoints, `ServiceSecured.svc` and `ServiceSecuredAsmx.asmx`, trust the checked-in test PFX files under `SoapClientCallAssistTests/TestData`. <br/>
-> `src/tests/ConsoleSoapCallTest`, a runnable console sample, not an automated test. <br/>

`TestSoapServiceN45` is a classic `packages.config` web project. Under `dotnet build` its build target is a no-op that prints a notice, so `dotnet build src/RzR.Shared.Services.sln` succeeds without it; build it with Visual Studio or `MSBuild.exe` before running the legacy suite, whose fixture hosts that project's `bin\`.

Environment variables the suites read:

<table>
	<thead>
		<tr>
			<th>Variable</th>
			<th>Effect</th>
		</tr>
	</thead>
	<tbody>
		<tr>
			<td><b>SOAPCLIENTCALLASSIST_ALLOW_ENVIRONMENT_SKIP</b></td>
			<td>`1`, `true` or `yes` downgrades an environment gap to a skipped test instead of a failure: port 80 not listenable for the WCF host, IIS Express not installed for the legacy suite, no JDK for the Java interop rows. Unset, those gaps fail the affected tests, on purpose.</td>
		</tr>
		<tr>
			<td><b>SOAPCLIENTCALLASSIST_TESTS_IIS_CLIENT_CERT</b></td>
			<td>Enables the elevated IIS mTLS path of the legacy suite, which needs a client certificate whose issuer the operator imported into `LocalMachine\Root`; `SOAPCLIENTCALLASSIST_TESTS_IIS_CLIENT_CERT_PFX` names the PFX carrying that certificate and its private key, `SOAPCLIENTCALLASSIST_TESTS_IIS_CLIENT_CERT_PFX_PASSWORD` its password.</td>
		</tr>
		<tr>
			<td><b>SOAPCLIENTCALLASSIST_TESTS_JAVA_HOME</b></td>
			<td>The JDK the Java cross-stack verifier runs on, tried before `JAVA_HOME` and `PATH`. The harness prefers the first JDK of major version 11 or newer, because validating an HMAC `ds:Signature` through JSR-105 with a `SecretKeySpec` is only relied upon from JDK 11; with only an older JDK the RSA rows still run, while the symmetric (HMAC) rows fail, or skip under the environment-skip variable, rather than trust a verdict from that runtime.</td>
		</tr>
	</tbody>
</table>

An environment note rather than a requirement: the Java rows start a JVM per verification, and on a host whose commit charge is near its limit that JVM can fail to reserve its default heap; on such a machine `JAVA_TOOL_OPTIONS='-Xmx96m'` (or similar) lets the rows run.
