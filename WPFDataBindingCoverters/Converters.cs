using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace WPFDataBindingCoverters
{

    public class PercentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
        object parameter, System.Globalization.CultureInfo culture)
        {
            double dValue = (double)value;
            return dValue.ToString("F0") + "%";
        }
        public object ConvertBack(object value, Type targetType,
        object parameter, System.Globalization.CultureInfo culture)
        {
            double dValue;
            string d = (string)value;
            d.Replace("%", "");
            double.TryParse(d, out dValue);
            return dValue;
            //Mes
        }
    }
    
}
