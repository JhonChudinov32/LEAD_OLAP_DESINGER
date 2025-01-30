using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LEAD_OLAP_DESINGER.Models
{
    public class DBParameters
    {
        public string Server = string.Empty;
        public int Port = 0;
        public string Database = string.Empty;
        public string Username = string.Empty;
        public string Password = string.Empty;
        public enum DBMS { PG, MS }
    }
}
