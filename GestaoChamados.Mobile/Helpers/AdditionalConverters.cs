using System.Globalization;

namespace GestaoChamados.Mobile.Helpers;

public class BoolToAlignmentConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isBot)
        {
            return isBot ? LayoutOptions.Start : LayoutOptions.End;
        }
        return LayoutOptions.Start;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class IsBotToColorConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] is bool isBot)
        {
            return isBot ? Color.FromArgb("#E3F2FD") : Color.FromArgb("#C8E6C9");
        }
        return Color.FromArgb("#F5F5F5");
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StatusToButtonVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string status)
        {
            return status == "Aberto" || status == "Em Atendimento";
        }
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
