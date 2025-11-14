using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GestaoChamados.Shared.DTOs;
using Microsoft.AspNetCore.SignalR.Client;

namespace GestaoChamados.Desktop;

public partial class DetalhesWindow : Window
{
    private ChamadoDto _chamado;
    
    public DetalhesWindow(ChamadoDto chamado)
    {
        InitializeComponent();
        _chamado = chamado;
        CarregarDetalhes(chamado);
    }

    private void CarregarDetalhes(ChamadoDto chamado)
    {
        // Título
        TitleTextBlock.Text = $"📋 Detalhes do Chamado #{chamado.Id:D7}";

        // Protocolo
        ProtocoloText.Text = chamado.Id.ToString("D7");

        // Data de Abertura
        DataAberturaText.Text = chamado.DataCriacao.ToString("dd/MM/yyyy HH:mm");

        // Assunto
        AssuntoText.Text = chamado.Titulo;

        // Status
        StatusText.Text = GetStatusTexto(chamado.Status);
        StatusBadge.Background = new System.Windows.Media.SolidColorBrush(
            GetStatusColorBrush(chamado.Status));

        // Criado Por
        CriadoPorText.Text = chamado.UsuarioEmail ?? chamado.UsuarioNome ?? "Desconhecido";

        // Técnico Responsável
        TecnicoText.Text = string.IsNullOrEmpty(chamado.TecnicoNome) 
            ? "Não atribuído" 
            : chamado.TecnicoNome;

        // Descrição
        if (!string.IsNullOrEmpty(chamado.Descricao))
        {
            // Se a descrição contém histórico do chatbot
            if (chamado.Descricao.Contains("=== PROBLEMA RESOLVIDO PELO CHATBOT ==="))
            {
                DescricaoText.Text = chamado.Descricao;
            }
            else
            {
                DescricaoText.Text = chamado.Descricao;
            }
        }
        else
        {
            DescricaoText.Text = "Sem descrição detalhada.";
        }

        // Mostrar botão "Assumir Chamado" apenas para técnico/gerente/admin
        // e quando o chamado estiver Aberto ou Aguardando Atendente
        var isTecnico = App.CurrentUserRole == "Tecnico" || 
                       App.CurrentUserRole == "Gerente" || 
                       App.CurrentUserRole == "Admin";
        
        var podeAssumir = chamado.Status == "Aberto" || 
                         chamado.Status == "Aguardando Atendente" ||
                         string.IsNullOrEmpty(chamado.TecnicoNome) ||
                         chamado.TecnicoNome == "Não atribuído";

        if (isTecnico && podeAssumir)
        {
            AssumirButton.Visibility = Visibility.Visible;
        }
        else
        {
            AssumirButton.Visibility = Visibility.Collapsed;
        }

        // Mostrar botão Finalizar apenas para técnico em atendimento
        var podeResolver = isTecnico && chamado.Status == "Em Atendimento";
        FinalizarButton.Visibility = podeResolver ? Visibility.Visible : Visibility.Collapsed;
        
        // Mostrar botão "Abrir Chat" se o chamado está Em Atendimento (para todos os envolvidos)
        if (chamado.Status == "Em Atendimento")
        {
            AbrirChatButton.Visibility = Visibility.Visible;
        }
        else
        {
            AbrirChatButton.Visibility = Visibility.Collapsed;
        }
    }

    private string GetStatusTexto(string status)
    {
        return status switch
        {
            "Aberto" => "Aberto",
            "EmAndamento" => "Em Atendimento",
            "Resolvido" => "Resolvido",
            "Fechado" => "Fechado",
            _ => status
        };
    }

    private System.Windows.Media.Color GetStatusColorBrush(string status)
    {
        return status switch
        {
            "Aberto" => System.Windows.Media.Color.FromRgb(220, 53, 69),      // #DC3545 Vermelho
            "EmAndamento" => System.Windows.Media.Color.FromRgb(255, 193, 7), // #FFC107 Amarelo
            "Resolvido" => System.Windows.Media.Color.FromRgb(40, 167, 69),   // #28A745 Verde
            "Fechado" => System.Windows.Media.Color.FromRgb(108, 117, 125),   // #6C757D Cinza
            _ => System.Windows.Media.Color.FromRgb(108, 117, 125)
        };
    }

    private async void FinalizarButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Deseja finalizar este chamado?\n\nO chamado será marcado como Resolvido.",
            "Finalizar Chamado",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
            return;

        try
        {
            FinalizarButton.IsEnabled = false;
            FinalizarButton.Content = "⏳ Finalizando...";

            // Marcar como resolvido
            var sucesso = await App.ApiService.MarcarComoResolvidoAsync(_chamado.Id);

            if (sucesso)
            {
                // Fechar janela
                _chamado.Status = "Resolvido";
                CarregarDetalhes(_chamado);
                
                // Fechar janela
                this.Close();
            }
            else
            {
                MessageBox.Show("Erro ao finalizar chamado.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                FinalizarButton.IsEnabled = true;
                FinalizarButton.Content = "✓ Finalizar Atendimento";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            FinalizarButton.IsEnabled = true;
            FinalizarButton.Content = "✓ Finalizar Atendimento";
        }
    }

    private async void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void AbrirChatButton_Click(object sender, RoutedEventArgs e)
    {
        // Abrir janela de chat ao vivo
        var chatWindow = new ChatAoVivoWindow(_chamado.Id);
        chatWindow.Show();
    }

    private async void AssumirButton_Click(object sender, RoutedEventArgs e)
    {
        var resultado = MessageBox.Show(
            $"Deseja assumir o atendimento do chamado #{_chamado.Id:D7}?\n\n" +
            $"Assunto: {_chamado.Titulo}\n" +
            $"Solicitante: {_chamado.UsuarioEmail}",
            "Assumir Chamado",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (resultado != MessageBoxResult.Yes)
            return;

        try
        {
            // Desabilitar botão durante processamento
            AssumirButton.IsEnabled = false;
            AssumirButton.Content = "⏳ Assumindo...";

            var sucesso = await App.ApiService.AssumirChamadoAsync(_chamado.Id);

            if (sucesso)
            {
                // ✅ Abrir chat ao vivo com o usuário
                MessageBox.Show(
                    $"✅ Chamado #{_chamado.Id:D7} assumido com sucesso!\n\n" +
                    $"Abrindo chat com o usuário...",
                    "Sucesso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                
                // Abrir janela de chat ao vivo
                var chatWindow = new ChatAoVivoWindow(_chamado.Id);
                chatWindow.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show(
                    "Erro ao assumir chamado.\n\n" +
                    "O chamado pode já ter sido assumido por outro técnico.",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                AssumirButton.IsEnabled = true;
                AssumirButton.Content = "✓ Assumir Chamado";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Erro ao assumir chamado: {ex.Message}",
                "Erro",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            AssumirButton.IsEnabled = true;
            AssumirButton.Content = "✓ Assumir Chamado";
        }
    }
}
