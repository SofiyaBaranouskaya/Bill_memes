using System;
using System.Globalization;
using System.Windows.Data;

namespace Bill_memes
{
    public class BoolToLikeTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool liked)
                return liked ? "Убрать лайк" : "Лайк";
            return "Лайк";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
