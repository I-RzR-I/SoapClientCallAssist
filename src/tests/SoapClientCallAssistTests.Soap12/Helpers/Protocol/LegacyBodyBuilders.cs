using System.Collections.Generic;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Helpers.Protocol;

internal static class LegacyBodyBuilders
{
    internal static readonly XNamespace Service = SoapAssert.ServiceNs;

    internal static readonly XNamespace DataContract = "http://schemas.datacontract.org/2004/07/TestSoapServiceN45.Dto";

    internal static XElement IsValidQualifiedViaConstructor(string id = "s1", string idV2 = "s12")
        => new XElement(
            Service + "IsValid",
            new XElement(Service + "id", id),
            new XElement(Service + "idV2", idV2));

    internal static XElement IsValidQualifiedViaAddCalls(string id = "s1", string idV2 = "s12")
    {
        var root = new XElement(Service + "IsValid");
        root.Add(new XElement(Service + "id", id));
        root.Add(new XElement(Service + "idV2", idV2));

        return root;
    }

    internal static XElement IsValidQualifiedFromSharedParamsList(string id = "s1", string idV2 = "s12")
    {
        var parameters = new List<XElement>
        {
            new XElement(Service + "id", id),
            new XElement(Service + "idV2", idV2)
        };

        return new XElement(Service + "IsValid", parameters);
    }

    internal static XElement IsValidUnqualifiedIdQualifiedIdV2(string id = "s1", string idV2 = "s12")
        => new XElement(
            Service + "IsValid",
            new XElement("id", id),
            new XElement(Service + "idV2", idV2));

    internal static XElement IsValidBothUnqualified(string id = "s1", string idV2 = "s12")
        => new XElement(
            Service + "IsValid",
            new XElement("id", id),
            new XElement("idV2", idV2));

    internal static XElement IsValidNoNamespaceRoot(string id = "s1", string idV2 = "s12")
        => new XElement(
            "IsValid",
            new XElement("id", id),
            new XElement("idV2", idV2));

    internal static XElement IsValidQualifiedChildrenNoNamespaceRoot(string id = "s1", string idV2 = "s12")
        => new XElement(
            "IsValid",
            new XElement(Service + "id", id),
            new XElement(Service + "idV2", idV2));

    internal static XElement AddRecordWithDetailEmpty()
        => new XElement(Service + "AddRecordWithDetail");

    internal static XElement AddRecordWithDetailSingleNamespace(
        string id = "1",
        string code = "Code-001",
        string name = "Name-001",
        string manufacturerId = "1",
        string supplierId = "2",
        string partnerId = "3")
        => new XElement(
            Service + "AddRecordWithDetail",
            new XElement(
                Service + "product",
                new XElement(Service + "Id", id),
                new XElement(Service + "Code", code),
                new XElement(Service + "Name", name),
                new XElement(Service + "IsActive", "true"),
                new XElement(
                    Service + "Detail",
                    new XElement(Service + "ManufacturerId", manufacturerId),
                    new XElement(Service + "SupplierId", supplierId),
                    new XElement(Service + "PartnerId", partnerId))));

    internal static XElement AddRecordWithDetailDualNamespace(
        string id = "1",
        string code = "Code-001",
        string name = "Name-001",
        string manufacturerId = "1",
        string supplierId = "2",
        string partnerId = "3")
        => new XElement(
            Service + "AddRecordWithDetail",
            new XElement(
                Service + "product",
                new XElement(DataContract + "Code", code),
                new XElement(
                    DataContract + "Detail",
                    new XElement(DataContract + "ManufacturerId", manufacturerId),
                    new XElement(DataContract + "PartnerId", partnerId),
                    new XElement(DataContract + "SupplierId", supplierId)),
                new XElement(DataContract + "Id", id),
                new XElement(DataContract + "IsActive", "true"),
                new XElement(DataContract + "Name", name)));

    internal static IReadOnlyList<XAttribute> DualNamespaceEnvelopeAttributes()
        => new List<XAttribute>
        {
            new XAttribute(XNamespace.Xmlns + "tes", DataContract),
            new XAttribute(XNamespace.Xmlns + "externalNs", Service)
        };
}
