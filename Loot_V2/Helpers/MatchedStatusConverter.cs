using Microsoft.UI.Xaml.Data;

namespace Loot_V2.Helpers;

public class MatchedStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is true ? "Matched" : "Unmatched";

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
