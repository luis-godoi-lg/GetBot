using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Media;
using GestaoChamados.Shared.DTOs;
using Microsoft.AspNetCore.SignalR.Client;

namespace GestaoChamados.Desktop;

/// <summary>
/// Janela de fila de atendimento para técnicos
/// Exibe chamados aguardando atribuição e permite que técnicos assumam atendimentos
/// Recebe notificações em tempo real via SignalR quando novos chamados entram na fila
/// </summary>
public partial class FilaAtendimentoWindow : Window
{
    private ObservableCollection<ChamadoDto> _chamadosNaFila = new();
    private HubConnection? _hubConnection;

    public FilaAtendimentoWindow()
    {
        InitializeComponent();
        UserInfoTextBlock.Text = App.CurrentUserEmail;
        Loaded += async (s, e) => 
        {
            await CarregarFila();
            await InicializarSignalR();
        };
    }

    private async Task CarregarFila()
    {
        try
        {
            LoadingText.Visibility = Visibility.Visible;
            ChamadosItemsControl.Visibility = Visibility.Collapsed;
            NenhumChamadoText.Visibility = Visibility.Collapsed;

            var todosChamados = await App.ApiService.GetChamadosAsync();

            // Filtrar apenas chamados Abertos ou Aguardando Atendente
            var chamadosNaFila = todosChamados?
                .Where(c => c.Status == "Aberto" || c.Status == "Aguardando Atendente")
                .OrderBy(c => c.DataCriacao)
                .ToList();

            LoadingText.Visibility = Visibility.Collapsed;

            if (chamadosNaFila != null && chamadosNaFila.Any())
            {
                ChamadosItemsControl.ItemsSource = chamadosNaFila;
                ChamadosItemsControl.Visibility = Visibility.Visible;
                NenhumChamadoText.Visibility = Visibility.Collapsed;
            }
            else
            {
                ChamadosItemsControl.Visibility = Visibility.Collapsed;
                NenhumChamadoText.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            LoadingText.Visibility = Visibility.Collapsed;
            MessageBox.Show($"Erro ao carregar fila: {ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task InicializarSignalR()
    {
        try
        {
            var apiBaseUrl = App.ApiService.GetBaseUrl().Replace("/api", "");
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{apiBaseUrl}/supportHub", options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(App.CurrentUserToken);
                })
                .WithAutomaticReconnect()
                .Build();

            // Event: Novo chamado na fila
            _hubConnection.On<ChamadoDto>("NovoUsuarioNaFila", (chamado) =>
            {
                Dispatcher.Invoke(async () =>
                {
                    // Tocar som de notificação
                    try
                    {
                        SystemSounds.Beep.Play();
                    }
                    catch { }
                    
                    // Mostrar notificação
                    var result = MessageBox.Show(
                        $"🔔 Novo chamado na fila!\n\n" +
                        $"Protocolo: #{chamado.Id:D7}\n" +
                        $"Usuário: {chamado.UsuarioEmail}\n" +
                        $"Assunto: {chamado.Titulo}\n\n" +
                        $"Deseja atender agora?",
                        "Nova Solicitação",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        // Assumir e abrir chat
                        var sucesso = await App.ApiService.AssumirChamadoAsync(chamado.Id);
                        if (sucesso)
                        {
                            var chatWindow = new ChatAoVivoWindow(chamado.Id);
                            chatWindow.Show();
                            this.Close();
                        }
                    }
                    else
                    {
                        // Apenas recarregar a lista
                        await CarregarFila();
                    }
                });
            });

            await _hubConnection.StartAsync();
            await _hubConnection.InvokeAsync("JoinTechnicianGroup");
        }
        catch (Exception ex)
        {
            // SignalR falhou, mas não é crítico
            MessageBox.Show(
                $"Aviso: Notificações em tempo real não disponíveis.\n\n" +
                $"Use o botão 'Atualizar' para ver novos chamados.\n\n" +
                $"Erro: {ex.Message}",
                "SignalR Indisponível",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    protected override async void OnClosed(EventArgs e)
    {
        if (_hubConnection != null)
        {
            try
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
            }
            catch { }
        }
        base.OnClosed(e);
    }

    private async void AtenderButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is int chamadoId)
        {
            if (!CustomMessageBox.ShowQuestion(
                "Deseja atender este chamado?\n\n" +
                "Você será direcionado para o chat ao vivo com o cliente.",
                "Iniciar Atendimento"))
                return;

            try
            {
                // Assumir o chamado
                button.IsEnabled = false;
                button.Content = "⏳ Assumindo...";

                System.Diagnostics.Debug.WriteLine($"[FilaAtendimento] Tentando assumir chamado {chamadoId}");
                var sucesso = await App.ApiService.AssumirChamadoAsync(chamadoId);
                System.Diagnostics.Debug.WriteLine($"[FilaAtendimento] Resultado assumir: {sucesso}");

                if (sucesso)
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"[FilaAtendimento] Criando ChatAoVivoWindow para chamado {chamadoId}");
                        // Abrir janela de chat ao vivo
                        var chatWindow = new ChatAoVivoWindow(chamadoId);
                        System.Diagnostics.Debug.WriteLine($"[FilaAtendimento] ChatAoVivoWindow criado, mostrando janela");
                        chatWindow.Show();
                        System.Diagnostics.Debug.WriteLine($"[FilaAtendimento] Janela mostrada, fechando FilaAtendimento");
                        this.Close();
                    }
                    catch (Exception chatEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[FilaAtendimento] ERRO ao criar/mostrar ChatAoVivoWindow: {chatEx.Message}");
                        System.Diagnostics.Debug.WriteLine($"[FilaAtendimento] StackTrace: {chatEx.StackTrace}");
                        
                        CustomMessageBox.ShowError(
                            $"Erro ao abrir janela de chat:\n\n{chatEx.Message}\n\nDetalhes:\n{chatEx.InnerException?.Message}",
                            "Erro");

                        button.IsEnabled = true;
                        button.Content = "Atender";
                    }
                }
                else
                {
                    CustomMessageBox.ShowWarning(
                        "Erro ao assumir chamado.\n\n" +
                        "O chamado pode já ter sido assumido por outro técnico.",
                        "Atenção");

                    button.IsEnabled = true;
                    button.Content = "Atender";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FilaAtendimento] ERRO geral: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[FilaAtendimento] StackTrace: {ex.StackTrace}");
                
                CustomMessageBox.ShowError(
                    $"Erro ao assumir chamado:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}", 
                    "Erro");

                button.IsEnabled = true;
                button.Content = "Atender";
            }
        }
    }

    private void ChamadosButton_Click(object sender, RoutedEventArgs e)
    {
        var chamadosWindow = new ChamadosWindow();
        chamadosWindow.Show();
        this.Close();
    }

    private void DashboardButton_Click(object sender, RoutedEventArgs e)
    {
        var dashboardWindow = new DashboardWindow();
        dashboardWindow.Show();
        this.Close();
    }

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("Deseja realmente sair?", "Confirmar Logout",
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            App.CurrentUserName = null;
            App.CurrentUserEmail = null;
            App.CurrentUserRole = null;

            var loginWindow = new MainWindow();
            loginWindow.Show();
            this.Close();
        }
    }
}
