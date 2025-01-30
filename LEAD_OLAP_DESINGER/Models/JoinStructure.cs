using Avalonia.Controls;
using LEAD_OLAP_DESINGER.Class;
using System;

namespace LEAD_OLAP_DESINGER.Models
{
    public class JoinStructure
    {
        public LineControl Line;
        public ListBox SourceListBox, TargetListBox;
        public int SourceIndex, TargetIndex;
        public Border SourcePanel, TargetPanel;
        public string SourceTableName, SourceColumnName, TargetTableName, TargetColumnName, Join_id, ConditionStatement;
        public int Record_id;
    }
}
