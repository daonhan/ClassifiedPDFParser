using System;
using System.Collections.Generic;
using System.Text;

namespace ClassifiedPrint.Core.Models
{
    public class Newspaper
    {
       
        public int ClassifiedId { get; set; }
        public string ContractNo { get; set; }
        public DateTime BeginDate { get; set; }
        public DateTime EndDate { get; set; }        
        public string Content { get; set; }
        public string Phone { get; set; }
        public DateTime Created { get; set; }
        public int Col { get; set; }
        public int Page { get; set; }
    }
}
