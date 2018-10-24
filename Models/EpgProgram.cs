using System.Xml.Serialization;

namespace Couchpotato.Models
{
    public class EpgProgram{
        [XmlAttribute(AttributeName = "start")]
        public string Start{get;set;}
        
        [XmlAttribute(AttributeName = "stop")]
        public string Stop {get;set;}

        [XmlAttribute(AttributeName = "channel")]
        public string Channel{get;set;}
        public string Lang{get;set;}

        [XmlElement(ElementName="title")]
        public string Title{get;set;}

        [XmlElement(ElementName="desc")]
        public string Desc{get;set;}

        [XmlElement(ElementName="episode-num")]
        public string EpisodeNumber{get;set;}
    }
}
