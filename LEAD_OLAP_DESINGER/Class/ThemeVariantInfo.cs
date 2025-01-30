using Avalonia.Styling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LEAD_OLAP_DESINGER.Class
{
    public class ThemeVariantInfo
    {
        public string ThemeVariantName { get; init; }

        public ThemeVariant ThemeVariant { get; init; }

        public ThemeVariantInfo(string name, ThemeVariant themeVariant)
        {
            ThemeVariantName = name;
            ThemeVariant = themeVariant;
        }
    }
}
