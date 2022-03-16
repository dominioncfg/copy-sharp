using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using CopyCore;
namespace CopySharp
{
    [ValueConversion(typeof(double), typeof(string))]
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

    [ValueConversion(typeof(long), typeof(string))]
    public class SpeedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double dValue = Math.Abs((long)value);

            string[] SpeedRateUnit = new string[] { " B/s", " Kb/s", " Mb/s", " Gb/s" };
            int actunit = 0;
            while (actunit < SpeedRateUnit.Length && dValue > 1024)
            {
                dValue /= 1024;
                actunit++;
            }


            return dValue.ToString("F2") + SpeedRateUnit[actunit];
        }
        public object ConvertBack(object value, Type targetType,
        object parameter, System.Globalization.CultureInfo culture)
        {
            /*No es necesario*/
            double dValue;
            string d = (string)value;
            d.Replace("%", "");
            double.TryParse(d, out dValue);
            return dValue;
            //Mes
        }
    }

    [ValueConversion(typeof(TimeSpan), typeof(string))]
    public class TimeSpanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            TimeSpan t = (TimeSpan)value;

            return t.ToString(@"hh\:mm\:ss");
        }
        public object ConvertBack(object value, Type targetType,
        object parameter, System.Globalization.CultureInfo culture)
        {
            /*No es necesario*/
            double dValue;
            string d = (string)value;
            d.Replace("%", "");
            double.TryParse(d, out dValue);
            return dValue;
            //Mes
        }
    }

    [ValueConversion(typeof(long), typeof(string))]
    public class LengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double dValue = Math.Abs((long)value);

            string[] SpeedRateUnit = new string[] { " B", " Kb", " Mb", " Gb"," Tb" };
            int actunit = 0;
            while (actunit < SpeedRateUnit.Length && dValue > 1024)
            {
                dValue /= 1024;
                actunit++;
            }


            return dValue.ToString("F2") + SpeedRateUnit[actunit];
        }
        public object ConvertBack(object value, Type targetType,
        object parameter, System.Globalization.CultureInfo culture)
        {
            /*No es necesario*/
            double dValue;
            string d = (string)value;
            d.Replace("%", "");
            double.TryParse(d, out dValue);
            return dValue;
            //Mes
        }
    }


    [ValueConversion(typeof(CopyState), typeof(bool))]
    public class LoadingConverterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            CopyState c =   (CopyState)value;

            bool b = c == CopyState.NoStartedYet;
            return b;
        }
        public object ConvertBack(object value, Type targetType,
        object parameter, System.Globalization.CultureInfo culture)
        {
            /*No es necesario*/
            double dValue;
            string d = (string)value;
            d.Replace("%", "");
            double.TryParse(d, out dValue);
            return dValue;
            //Mes
        }
    }
}
