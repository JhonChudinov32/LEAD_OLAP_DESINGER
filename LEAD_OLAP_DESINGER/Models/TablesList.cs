using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LEAD_OLAP_DESINGER.Models
{
    public class TablesList
    {
        public string TableName { get; set; }
        public string TableAlias { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Table_id { get; set; }
        public Border CustomPanel { get; set; }
        public ListBox CustomList { get; set; }


    }
}
