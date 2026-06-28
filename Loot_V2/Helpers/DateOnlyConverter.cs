using Microsoft.UI.Xaml.Data;

namespace Loot_V2.Helpers;

public class DateOnlyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is DateOnly d ? d.ToString("MM/dd/yyyy") : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
