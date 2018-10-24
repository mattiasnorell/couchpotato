using System.Collections.Generic;
using System.Xml.Serialization;

namespace M3uFixer.Models
{
    [XmlRoot("tv")]
    public class EpgList{
        [XmlAttribute(AttributeName = "generator-info-name")]
        public string GeneratorInfoName{get;set;}
        
        [XmlAttribute(AttributeName = "generator-info-url")]
        public string GeneratorInfoUrl {get;set;}

        [XmlElement(ElementName = "channel")]
        public List<EpgChannel> Channels{get;set;}

        [XmlElement(ElementName = "programme")]
        public List<EpgProgram> Programs{get;set;}
    }
}
