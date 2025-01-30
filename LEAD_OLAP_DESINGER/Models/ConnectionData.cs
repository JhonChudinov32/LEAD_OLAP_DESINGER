using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LEAD_OLAP_DESINGER.Models
{
    public class ConnectionData
    {
        public int Id { get; set; }
        public string nameBD { get; set; }
        public string namePlatform { get; set; }

        public string mainIP { get; set; }
        public string mainPort { get; set; }
        public string mainBD { get; set; }
        public string mainUser { get; set; }
        public string mainPassword { get; set; }
    }
}
