using System.Globalization;

namespace GestaoChamados.Mobile.Helpers;

public class StatusToColorConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] is string status)
        {
            return status switch
            {
                "Aberto" => Color.FromArgb("#FFA500"), // Laranja
                "Em Atendimento" => Color.FromArgb("#0078D4"), // Azul
                "Aguardando Atendimento" => Color.FromArgb("#FFD700"), // Dourado
                "Resolvido" => Color.FromArgb("#28A745"), // Verde
                "Finalizado" => Color.FromArgb("#6C757D"), // Cinza
                _ => Color.FromArgb("#6C757D")
            };
        }
        return Color.FromArgb("#6C757D");
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class PrioridadeToColorConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] is string prioridade)
        {
            return prioridade switch
            {
                "Alta" => Color.FromArgb("#DC3545"), // Vermelho
                "Media" => Color.FromArgb("#FFC107"), // Amarelo
                "Baixa" => Color.FromArgb("#28A745"), // Verde
                _ => Color.FromArgb("#6C757D")
            };
        }
        return Color.FromArgb("#6C757D");
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
