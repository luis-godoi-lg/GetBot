using Microsoft.Maui.Controls;
using System.Threading.Tasks;
using GestaoChamados.Mobile.Controls;

namespace GestaoChamados.Mobile.Services
{
    public static class CustomAlertService
    {
        private static readonly Dictionary<Controls.AlertType, string> DefaultTitles = new()
        {
            { Controls.AlertType.Success, "Sucesso" },
            { Controls.AlertType.Error, "Erro" },
            { Controls.AlertType.Warning, "Atenção" },
            { Controls.AlertType.Info, "Informação" },
            { Controls.AlertType.Question, "Confirmar" }
        };

        public static async Task ShowSuccessAsync(string message, string? title = null)
        {
            await CustomAlert.ShowAsync(
                message, 
                title ?? DefaultTitles[Controls.AlertType.Success], 
                Controls.AlertType.Success
            );
        }

        public static async Task ShowErrorAsync(string message, string? title = null)
        {
            await CustomAlert.ShowAsync(
                message, 
                title ?? DefaultTitles[Controls.AlertType.Error], 
                Controls.AlertType.Error
            );
        }

        public static async Task ShowWarningAsync(string message, string? title = null)
        {
            await CustomAlert.ShowAsync(
                message, 
                title ?? DefaultTitles[Controls.AlertType.Warning], 
                Controls.AlertType.Warning
            );
        }

        public static async Task ShowInfoAsync(string message, string? title = null)
        {
            await CustomAlert.ShowAsync(
                message, 
                title ?? DefaultTitles[Controls.AlertType.Info], 
                Controls.AlertType.Info
            );
        }

        public static async Task<bool> ShowQuestionAsync(string message, string? title = null, string acceptButton = "Sim", string cancelButton = "Não")
        {
            return await CustomAlert.ShowQuestionAsync(
                message, 
                title ?? DefaultTitles[Controls.AlertType.Question], 
                acceptButton, 
                cancelButton
            );
        }
    }
}
