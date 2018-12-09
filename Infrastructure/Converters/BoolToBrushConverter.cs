using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;


namespace Infrastructure.Converters
{
    public class BoolToBrushConverter : IValueConverter
    {
        public SolidColorBrush FalseBrush { get; set; }
        public SolidColorBrush TrueBrush { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var tmp = value as bool?;
            return tmp == true ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
