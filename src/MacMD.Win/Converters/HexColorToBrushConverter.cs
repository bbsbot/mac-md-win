using System.Globalization;
using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace MacMD.Win.Converters;

public sealed class HexColorToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string hex)
        {
            hex = hex.TrimStart('#');
            if (hex.Length == 6)
            {
                var r = byte.Parse(hex[0..2], NumberStyles.HexNumber);
                var g = byte.Parse(hex[2..4], NumberStyles.HexNumber);
                var b = byte.Parse(hex[4..6], NumberStyles.HexNumber);
                return new SolidColorBrush(Windows.UI.Color.FromArgb(255, r, g, b));
            }
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
