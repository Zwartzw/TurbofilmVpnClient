using System;
using System.Collections;
using System.Globalization;
using System.Text;

namespace TurbofilmVpn.Converters
{
    public class IEnumerableToStringConverter : MarkupConverter
    {
        protected override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var sb = new StringBuilder();
            var valueCollection = value as IEnumerable;
            if (valueCollection != null)
            {
                foreach (var item in valueCollection)
                {
                    if (sb.Length != 0)
                        sb.Append(", ");

                    sb.Append(item);
                }
                return sb.ToString();
            }

            return value;
        }

        protected override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public IEnumerableToStringConverter() { }
    }
}
