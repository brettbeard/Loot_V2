using Microsoft.UI.Xaml.Data;

namespace Loot_V2.Helpers;

public class CurrencyConverter : IValueConverter
{
    public bool Accounting { get; set; }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not decimal d) return value?.ToString() ?? string.Empty;
        if (Accounting && d < 0)
            return $"({(-d).ToString("C")})";
        return d.ToString("C");
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
