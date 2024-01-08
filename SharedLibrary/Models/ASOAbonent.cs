using System.Xml.Serialization;

namespace SharedLibrary.Models
{
    public class ASOAbonent
    {
        public string? Name { get; set; }
        public string? Comment { get; set; }
        public int Prior { get; set; }
        public string? Dep { get; set; }
        public string? Loc { get; set; }
        public string? Pos { get; set; }
        public int Role { get; set; }
        public int Stat { get; set; }        
        public string? Passw { get; set; }

        public string? Phone { get; set; }
        public int Confirm { get; set; }
        public int GlobType { get; set; }
        public int UserType { get; set; }
        public string? Addr { get; set; }        
        public string? BTime { get; set; }
        public string? ETime { get; set; }
        public int DayType { get; set; }
        public string? WeekDay { get; set; } = "0000000";
        
        public int BaseType { get; set; }
        public int ConnType { get; set; }
        [XmlArrayItem(ElementName ="Label")]
        public List<Labels>? Labels { get; set; }
    }

    public class Labels
    {
        [XmlAttribute]
        public string? Key { get; set; }
        [XmlText]
        public string? Value { get; set; }
    }
}
