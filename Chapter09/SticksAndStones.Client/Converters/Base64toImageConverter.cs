using System;
using System.Globalization;

namespace SticksAndStones.Converters;

public class Base64ToImageConverter : IValueConverter
{
    public object Convert(object value, Type targetType,
    object parameter, CultureInfo culture)
    {
        if (value is string)
        {
            var base64string = (string)value;
            var bytes = System.Convert.FromBase64String(base64string);
            var stream = new MemoryStream(bytes);
            return ImageSource.FromStream(() => stream);
        }
        else return value;
    }

    public object ConvertBack(object value, Type targetType,
    object parameter, CultureInfo culture)
    {
        var input = (Stream)value;
        using MemoryStream ms = new MemoryStream();
        input.CopyTo(ms);
        return System.Convert.ToBase64String(ms.ToArray());
    }
}

