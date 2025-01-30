using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace LEAD_OLAP_DESINGER.Models
{

    public class ReporterClass
    {
        public int Class_id { get; set; } // Уникальный идентификатор класса
        public string ClassName { get; set; } // Название класса
        public int GroupIndex { get; set; }
    }
}
