using System.Collections;
using System.Globalization;
using System.Windows.Data;

namespace PaperCounter.Utility
{
    public class PageSetStatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var key = parameter as string;
            if (value is not IList<PageSet> lst)
                return "";

            var ps = lst.SingleOrDefault(a => a.Info.Name == key);
            if (ps==null)
                return "0";
            else
                return ps.Count;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
