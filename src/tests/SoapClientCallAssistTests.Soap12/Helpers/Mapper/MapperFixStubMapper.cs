
#nullable disable

using RzR.ResultMessage;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Dto.Map;
using System.Collections.Generic;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Helpers.Mapper;

internal sealed class MapperFixStubMapper : ISoapModelMapper
{
    public IResult<IEnumerable<XElement>> ToBodies(SoapOperationRequest request)
        => Result<IEnumerable<XElement>>.Success(new List<XElement>());

    public IResult<T> FromResponse<T>(
        string soapResponse, XNamespace protocolNamespace, string soapXmlBodyTag = null)
        => Result<T>.Success(default);
}
