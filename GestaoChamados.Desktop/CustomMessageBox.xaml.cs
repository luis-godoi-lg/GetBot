using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace GestaoChamados.Desktop;

public partial class CustomMessageBox : Window
{
    public enum MessageBoxType
    {
        Success,
        Error,
        Warning,
        Question,
        Info
    }

    public enum MessageBoxButton
    {
        OK,
        OKCancel,
        YesNo,
        YesNoCancel
    }

    public enum MessageBoxResult
    {
        None,
        OK,
        Cancel,
        Yes,
        No
    }

    public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

    private CustomMessageBox(string title, string message, MessageBoxType type, MessageBoxButton buttons)
    {
        InitializeComponent();

        TitleText.Text = title;
        MessageText.Text = message;

        ConfigureIcon(type);
        ConfigureButtons(buttons);

        // Configurar transform para animação
        RenderTransformOrigin = new Point(0.5, 0.5);
        RenderTransform = new ScaleTransform();

        // Iniciar animação ao carregar
        Loaded += (s, e) =>
        {
            var storyboard = (Storyboard)FindResource("ShowAnimation");
            storyboard.Begin(this);
        };
    }

    private void ConfigureIcon(MessageBoxType type)
    {
        switch (type)
        {
            case MessageBoxType.Success:
                IconText.Text = "✓";
                HeaderBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#28A745"));
                break;

            case MessageBoxType.Error:
                IconText.Text = "✕";
                HeaderBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC3545"));
                break;

            case MessageBoxType.Warning:
                IconText.Text = "⚠";
                HeaderBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFC107"));
                break;

            case MessageBoxType.Question:
                IconText.Text = "?";
                HeaderBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#17A2B8"));
                break;

            case MessageBoxType.Info:
            default:
                IconText.Text = "ℹ";
                HeaderBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0066CC"));
                break;
        }
    }

    private void ConfigureButtons(MessageBoxButton buttons)
    {
        // Esconder todos primeiro
        OkButton.Visibility = Visibility.Collapsed;
        YesButton.Visibility = Visibility.Collapsed;
        NoButton.Visibility = Visibility.Collapsed;
        CancelButton.Visibility = Visibility.Collapsed;

        switch (buttons)
        {
            case MessageBoxButton.OK:
                OkButton.Visibility = Visibility.Visible;
                break;

            case MessageBoxButton.OKCancel:
                OkButton.Visibility = Visibility.Visible;
                CancelButton.Visibility = Visibility.Visible;
                break;

            case MessageBoxButton.YesNo:
                YesButton.Visibility = Visibility.Visible;
                NoButton.Visibility = Visibility.Visible;
                break;

            case MessageBoxButton.YesNoCancel:
                YesButton.Visibility = Visibility.Visible;
                NoButton.Visibility = Visibility.Visible;
                CancelButton.Visibility = Visibility.Visible;
                break;
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        Result = MessageBoxResult.OK;
        Close();
    }

    private void YesButton_Click(object sender, RoutedEventArgs e)
    {
        Result = MessageBoxResult.Yes;
        Close();
    }

    private void NoButton_Click(object sender, RoutedEventArgs e)
    {
        Result = MessageBoxResult.No;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Result = MessageBoxResult.Cancel;
        Close();
    }

    // ==================== MÉTODOS ESTÁTICOS (API PÚBLICA) ====================

    /// <summary>
    /// Mostra mensagem de sucesso
    /// </summary>
    public static void ShowSuccess(string message, string title = "Sucesso!")
    {
        var msgBox = new CustomMessageBox(title, message, MessageBoxType.Success, MessageBoxButton.OK);
        msgBox.ShowDialog();
    }

    /// <summary>
    /// Mostra mensagem de erro
    /// </summary>
    public static void ShowError(string message, string title = "Erro")
    {
        var msgBox = new CustomMessageBox(title, message, MessageBoxType.Error, MessageBoxButton.OK);
        msgBox.ShowDialog();
    }

    /// <summary>
    /// Mostra mensagem de aviso
    /// </summary>
    public static void ShowWarning(string message, string title = "Atenção")
    {
        var msgBox = new CustomMessageBox(title, message, MessageBoxType.Warning, MessageBoxButton.OK);
        msgBox.ShowDialog();
    }

    /// <summary>
    /// Mostra mensagem de informação
    /// </summary>
    public static void ShowInfo(string message, string title = "Informação")
    {
        var msgBox = new CustomMessageBox(title, message, MessageBoxType.Info, MessageBoxButton.OK);
        msgBox.ShowDialog();
    }

    /// <summary>
    /// Mostra pergunta com Sim/Não
    /// </summary>
    public static bool ShowQuestion(string message, string title = "Confirmar")
    {
        var msgBox = new CustomMessageBox(title, message, MessageBoxType.Question, MessageBoxButton.YesNo);
        msgBox.ShowDialog();
        return msgBox.Result == MessageBoxResult.Yes;
    }

    /// <summary>
    /// Mostra confirmação personalizada
    /// </summary>
    public static MessageBoxResult Show(string message, string title, MessageBoxType type, MessageBoxButton buttons)
    {
        var msgBox = new CustomMessageBox(title, message, type, buttons);
        msgBox.ShowDialog();
        return msgBox.Result;
    }
}
