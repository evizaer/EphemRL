using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using EphemRL.Models;

namespace EphemRL.Converters
{
    public class ElementToColorConverter : IValueConverter
    {
        private static readonly Dictionary<ManaElement, SolidColorBrush> ManaColors = new Dictionary
            <ManaElement, SolidColorBrush>
        {
            {ManaElement.Earth, new SolidColorBrush(Colors.DarkGreen)},
            {ManaElement.Fire, new SolidColorBrush(Colors.Red)},
            {ManaElement.Water, new SolidColorBrush(Colors.Blue)},
            {ManaElement.Void, new SolidColorBrush(Colors.Purple)},
            {ManaElement.Life, new SolidColorBrush(Colors.WhiteSmoke)}
        };

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return ManaColors[(ManaElement) value];
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
