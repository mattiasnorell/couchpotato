using System.Xml.Serialization;

namespace M3uFixer.Models
{
    public class EpgChannel{
        [XmlAttribute(AttributeName="id")]
        public string Id{get;set;}
        
        [XmlElement(ElementName="display-name")]
        public string DisplayName {get;set;}

        [XmlElement(ElementName="url")]
        public string Url{get;set;}
    }
}
