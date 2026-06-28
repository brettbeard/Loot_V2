using Microsoft.UI.Xaml.Data;

namespace Loot_V2.Helpers;

public class CreditTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is true ? "Income" : "Expense";

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
