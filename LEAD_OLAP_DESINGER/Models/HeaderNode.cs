using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LEAD_OLAP_DESINGER.Models
{
    public class HeaderNode
    {
        public ObservableCollection<object> Children { get; set; } = new();
    }

}
