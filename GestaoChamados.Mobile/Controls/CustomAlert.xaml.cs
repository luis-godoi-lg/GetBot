using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using CommunityToolkit.Maui.Views;

namespace GestaoChamados.Mobile.Controls
{
    public partial class CustomAlert : Popup
    {
        public CustomAlert()
        {
            InitializeComponent();
        }

        public static Task ShowAsync(string message, string title, AlertType type, bool showCancel = false, string okText = "OK", string cancelText = "Não")
        {
            return ShowInternalAsync(message, title, type, showCancel, okText, cancelText);
        }

        public static Task<bool> ShowQuestionAsync(string message, string title, string acceptText = "Sim", string cancelText = "Não")
        {
            return ShowQuestionInternalAsync(message, title, acceptText, cancelText);
        }

        private static async Task ShowInternalAsync(string message, string title, AlertType type, bool showCancel, string okText, string cancelText)
        {
            Debug.WriteLine($"[CustomAlert] ShowInternalAsync chamado - tipo: {type}");
            
            var alert = new CustomAlert();
            alert.Configure(message, title, type, showCancel, okText, cancelText);
            
            var currentPage = Application.Current?.Windows[0]?.Page;
            if (currentPage == null)
            {
                Debug.WriteLine("[CustomAlert] ERRO: currentPage é null!");
                return;
            }

            Debug.WriteLine($"[CustomAlert] currentPage tipo: {currentPage.GetType().Name}");

            // Mostrar popup e aguardar resposta
            await currentPage.ShowPopupAsync(alert);
            
            Debug.WriteLine("[CustomAlert] Popup fechado");
        }

        private static async Task<bool> ShowQuestionInternalAsync(string message, string title, string acceptText, string cancelText)
        {
            Debug.WriteLine($"[CustomAlert] ShowQuestionInternalAsync chamado");
            
            var alert = new CustomAlert();
            alert.Configure(message, title, AlertType.Question, true, acceptText, cancelText);
            
            var currentPage = Application.Current?.Windows[0]?.Page;
            if (currentPage == null)
            {
                Debug.WriteLine("[CustomAlert] ERRO: currentPage é null!");
                return false;
            }

            Debug.WriteLine($"[CustomAlert] Question: currentPage tipo: {currentPage.GetType().Name}");

            // Mostrar popup e aguardar resposta
            var result = await currentPage.ShowPopupAsync(alert);
            
            Debug.WriteLine($"[CustomAlert] Resposta recebida: {result}");
            
            return result is bool boolResult && boolResult;
        }

        private void Configure(string message, string title, AlertType type, bool showCancel, string okText, string cancelText)
        {
            Debug.WriteLine($"[CustomAlert] Configure chamado - tipo: {type}, showCancel: {showCancel}");
            
            MessageLabel.Text = message;
            TitleLabel.Text = title;
            OkLabel.Text = okText;
            CancelLabel.Text = cancelText;
            
            Debug.WriteLine($"[CustomAlert] Labels configurados - Message: {message.Substring(0, Math.Min(20, message.Length))}...");
            
            // Configurar cores e ícones baseado no tipo
            switch (type)
            {
                case AlertType.Success:
                    HeaderBorder.BackgroundColor = Color.FromArgb("#28A745");
                    OkButtonBorder.BackgroundColor = Color.FromArgb("#28A745");
                    IconLabel.Text = "✓";
                    Debug.WriteLine("[CustomAlert] Tipo Success configurado");
                    break;
                    
                case AlertType.Error:
                    HeaderBorder.BackgroundColor = Color.FromArgb("#DC3545");
                    OkButtonBorder.BackgroundColor = Color.FromArgb("#DC3545");
                    IconLabel.Text = "✕";
                    Debug.WriteLine("[CustomAlert] Tipo Error configurado");
                    break;
                    
                case AlertType.Warning:
                    HeaderBorder.BackgroundColor = Color.FromArgb("#FFC107");
                    OkButtonBorder.BackgroundColor = Color.FromArgb("#FFC107");
                    IconLabel.Text = "⚠";
                    Debug.WriteLine("[CustomAlert] Tipo Warning configurado");
                    break;
                    
                case AlertType.Info:
                    HeaderBorder.BackgroundColor = Color.FromArgb("#0066CC");
                    OkButtonBorder.BackgroundColor = Color.FromArgb("#0066CC");
                    IconLabel.Text = "ℹ";
                    Debug.WriteLine("[CustomAlert] Tipo Info configurado");
                    break;
                    
                case AlertType.Question:
                    HeaderBorder.BackgroundColor = Color.FromArgb("#17A2B8");
                    OkButtonBorder.BackgroundColor = Color.FromArgb("#17A2B8");
                    IconLabel.Text = "?";
                    Debug.WriteLine("[CustomAlert] Tipo Question configurado");
                    break;
            }
            
            // Mostrar/ocultar botão cancelar
            CancelButtonBorder.IsVisible = showCancel;
            
            // Ajustar layout dos botões
            if (showCancel)
            {
                Grid.SetColumnSpan(OkButtonBorder, 1);
                Grid.SetColumn(OkButtonBorder, 1);
            }
            else
            {
                Grid.SetColumnSpan(OkButtonBorder, 2);
                Grid.SetColumn(OkButtonBorder, 0);
            }
            
            Debug.WriteLine("[CustomAlert] Configure concluído com sucesso");
        }

        private void OnOkTapped(object sender, EventArgs e)
        {
            Debug.WriteLine("[CustomAlert] OnOkTapped chamado");
            Close(true);
        }

        private void OnCancelTapped(object sender, EventArgs e)
        {
            Debug.WriteLine("[CustomAlert] OnCancelTapped chamado");
            Close(false);
        }
    }

    public enum AlertType
    {
        Success,
        Error,
        Warning,
        Info,
        Question
    }
}
