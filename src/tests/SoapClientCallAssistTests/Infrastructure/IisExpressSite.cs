namespace SoapClientCallAssistTests.Infrastructure
{
    public class IisExpressSite
    {
        public IisExpressSite(int id, string name, string physicalPath)
        {
            Id = id;
            Name = name;
            PhysicalPath = physicalPath;
        }

        public int Id { get; }

        public string Name { get; }

        public string PhysicalPath { get; }

        public override string ToString()
        {
            var description = "id=" + Id + " name='" + Name + "'";

            return PhysicalPath == null ? description : description + " path='" + PhysicalPath + "'";
        }
    }
}
