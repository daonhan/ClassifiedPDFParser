using System;
using System.Collections.Generic;
using System.Text;

namespace ClassifiedPrint.Core.Models
{
    public class Command
    {
        public Command(string sql, object parameters)
        {
            Sql = sql;
            Parameters = parameters;
        }

        public string Sql { get; set; }
        public object Parameters { get; set; }
    }
}
