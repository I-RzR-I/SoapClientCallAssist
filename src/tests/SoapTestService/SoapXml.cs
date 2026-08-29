using System.Xml;

namespace SoapTestService;

public static class SoapXml
{
    private static readonly XmlReaderSettings Settings = new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        MaxCharactersFromEntities = 1024,
        MaxCharactersInDocument = 1000000,
        IgnoreProcessingInstructions = true,
        ConformanceLevel = ConformanceLevel.Document,
        CloseInput = true
    };

    public static XmlReader CreateReader(Stream stream)
        => XmlReader.Create(stream, Settings);

    public static XmlReader CreateReader(TextReader reader)
        => XmlReader.Create(reader, Settings);
}
