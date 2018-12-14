using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ClassifiedPrint.Uploader.Models
{
    public class ClassifiedPrint
    {
        public ClassifiedPrint()
        {
            ContentBK2 = string.Empty;
        }

        [XmlElement("id")]
        public int Id { get; set; }
        [XmlElement("contractno")]
        public string ContractNo { get; set; }
        [XmlElement("bdate")]
        public DateTime BDate { get; set; }
        [XmlElement("edate")]
        public DateTime EDate { get; set; }
        [XmlElement("content")]
        public string Content { get; set; }
        [XmlElement("phone")]
        public string Phone { get; set; }
        [XmlElement("created")]
        public DateTime Created { get; set; }

        [XmlIgnore]
        public string ContentBK2 { get; set; }
    }

    [XmlRoot("MBP")]
    public class MBP
    {
        public MBP() { Items = new List<ClassifiedPrint>(); }
        [XmlElement("A")]
        public List<ClassifiedPrint> Items { get; set; }
    }


    public class ParserContentResult
    {
        public int TotalRecord { get; set; }
        public int Parsed { get; set; }
    }

}
