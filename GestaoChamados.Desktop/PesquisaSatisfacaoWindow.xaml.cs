using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GestaoChamados.Desktop;

public partial class PesquisaSatisfacaoWindow : Window
{
    public bool Finalizado { get; private set; }
    public int? AvaliacaoSelecionada { get; private set; }
    private int _chamadoId;

    public PesquisaSatisfacaoWindow(int chamadoId)
    {
        InitializeComponent();
        _chamadoId = chamadoId;
        Finalizado = false;
    }

    private void AvaliacaoButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            // Resetar todos os botões
            ResetarBotoesAvaliacao();

            // Marcar o botão selecionado
            button.Opacity = 1.0;
            var border = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(25, 135, 84)),
                BorderThickness = new Thickness(3),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(5)
            };

            // Pegar a avaliação
            AvaliacaoSelecionada = int.Parse(button.Tag.ToString() ?? "3");

            // Atualizar texto
            var textos = new[] { "", "Muito Insatisfeito", "Insatisfeito", "Neutro", "Satisfeito", "Muito Satisfeito" };
            AvaliacaoSelecionadaText.Text = $"✓ Avaliação: {textos[AvaliacaoSelecionada.Value]}";
            AvaliacaoSelecionadaText.FontWeight = FontWeights.Bold;

            // Destacar o botão selecionado
            foreach (var child in AvaliacaoPanel.Children)
            {
                if (child is Button btn)
                {
                    btn.Opacity = btn == button ? 1.0 : 0.5;
                }
            }

            // Habilitar botão de finalizar
            BtnFinalizar.IsEnabled = true;
        }
    }

    private void ResetarBotoesAvaliacao()
    {
        foreach (var child in AvaliacaoPanel.Children)
        {
            if (child is Button btn)
            {
                btn.Opacity = 0.7;
            }
        }
    }

    private async void BtnFinalizar_Click(object sender, RoutedEventArgs e)
    {
        if (!AvaliacaoSelecionada.HasValue)
        {
            CustomMessageBox.ShowWarning("Por favor, selecione uma avaliação antes de finalizar.", "Avaliação Obrigatória");
            return;
        }

        try
        {
            // Desabilitar botões durante o processamento
            BtnFinalizar.IsEnabled = false;
            BtnContinuar.IsEnabled = false;
            BtnFinalizar.Content = "Finalizando...";

            // ✅ 1. Marcar como resolvido
            var sucesso = await App.ApiService.FinalizarChamadoAsync(_chamadoId);
            
            if (!sucesso)
            {
                CustomMessageBox.ShowError("Erro ao finalizar o chamado.", "Erro");
                BtnFinalizar.IsEnabled = true;
                BtnContinuar.IsEnabled = true;
                BtnFinalizar.Content = "✓ Sim, Finalizar Atendimento";
                return;
            }
            
            // ✅ 2. Enviar avaliação
            await Task.Delay(300); // Pequeno delay para garantir que a finalização foi processada
            var avaliacaoSucesso = await App.ApiService.AvaliarChamadoAsync(_chamadoId, AvaliacaoSelecionada.Value);
            
            if (!avaliacaoSucesso)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Aviso: Erro ao enviar avaliação, mas chamado foi finalizado");
            }

            CustomMessageBox.ShowSuccess(
                $"Chamado #{_chamadoId:D7} finalizado com sucesso!\n\nAvaliação: {AvaliacaoSelecionada.Value}/5 estrelas\n\nObrigado por utilizar nosso sistema!",
                "Atendimento Concluído");

            Finalizado = true;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            CustomMessageBox.ShowError($"Erro ao finalizar: {ex.Message}", "Erro");
            BtnFinalizar.IsEnabled = true;
            BtnContinuar.IsEnabled = true;
            BtnFinalizar.Content = "✓ Sim, Finalizar Atendimento";
        }
    }

    private void BtnContinuar_Click(object sender, RoutedEventArgs e)
    {
        Finalizado = false;
        DialogResult = false;
        Close();
    }
}
