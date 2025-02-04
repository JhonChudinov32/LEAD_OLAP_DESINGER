namespace LEAD_OLAP_DESINGER.Models
{
    public class ReporterObject
    {
        public int Object_id { get; set; } // Уникальный идентификатор объекта
        public string ObjectName { get; set; } // Название объекта
        public int ReporterDimension_id { get; set; } // Идентификатор измерения (или -1, если отсутствует)
        public int ReporterMeasure_id { get; set; } // Идентификатор меры (или -1, если отсутствует)
        public int ReporterDetail_id { get; set; } // Идентификатор детали (или -1, если отсутствует)
        public int ReporterClass_id { get; set; } // Идентификатор связанного класса
        public bool IsNumeric { get; set; } // Признак, является ли объект числовым
        public string ObjectType { get; set; } // Тип объекта (например, "Измерение", "Мера", "Деталь")
        public string ClassName { get; set; } // Название класса
        public string ObjectDescription { get; set; }
        public int  ItemIndex { get; set; }
    }
}
