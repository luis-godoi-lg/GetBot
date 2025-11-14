using System.Windows;
using System.Collections.ObjectModel;
using Microsoft.AspNetCore.SignalR.Client;

namespace GestaoChamados.Desktop;

/// <summary>
/// Dashboard principal para técnicos, gerentes e administradores
/// Exibe estatísticas de chamados, métricas de desempenho e notificações em tempo real via SignalR
/// </summary>
public partial class DashboardWindow : Window
{
    private ObservableCollection<TopUsuarioViewModel> _topUsuarios = new();
    private HubConnection? _hubConnection;

    public DashboardWindow()
    {
        InitializeComponent();
        UserInfoTextBlock.Text = App.CurrentUserEmail;
        
        // Se não for técnico, gerente ou admin, mostra subtítulo diferente
        if (App.CurrentUserRole == "Usuario")
        {
            SubtitleTextBlock.Text = "Exibindo estatísticas dos seus chamados.";
        }
        
        Loaded += async (s, e) => 
        {
            await CarregarDashboard();
            
            // Inicializar SignalR apenas para técnicos
            var isTecnico = App.CurrentUserRole == "Tecnico" || 
                           App.CurrentUserRole == "Gerente" || 
                           App.CurrentUserRole == "Admin";
            if (isTecnico)
            {
                await InicializarSignalR();
            }
        };
    }

    private async Task CarregarDashboard()
    {
        try
        {
            var chamados = await App.ApiService.GetChamadosAsync();

            // Se for usuário comum, filtra apenas seus chamados
            if (App.CurrentUserRole == "Usuario")
            {
                chamados = chamados.Where(c => c.UsuarioEmail == App.CurrentUserEmail).ToList();
            }

            // Calcula estatísticas
            var total = chamados?.Count ?? 0;
            var abertos = chamados?.Count(c => c.Status == "Aberto") ?? 0;
            var aguardandoAtendente = chamados?.Count(c => c.Status == "Aguardando Atendente") ?? 0;
            var emAtendimento = chamados?.Count(c => c.Status == "Em Atendimento") ?? 0;
            var resolvidos = chamados?.Count(c => c.Status == "Resolvido") ?? 0;

            // Atualiza cartões
            TotalChamadosText.Text = total.ToString();
            ClientesFilaText.Text = aguardandoAtendente.ToString(); // ✅ Corrigido: Aguardando Atendente
            EmAtendimentoText.Text = emAtendimento.ToString();
            ResolvidosText.Text = resolvidos.ToString();

            // Atualiza legenda do gráfico
            AbertosLegendaText.Text = abertos.ToString();
            EmAtendimentoLegendaText.Text = emAtendimento.ToString();
            ResolvidosLegendaText.Text = resolvidos.ToString();

            // ==================== MÉTRICAS AVANÇADAS PARA TÉCNICO ====================
            var isTecnico = App.CurrentUserRole == "Tecnico" || 
                           App.CurrentUserRole == "Gerente" || 
                           App.CurrentUserRole == "Admin";

            if (isTecnico && chamados != null)
            {
                TecnicoMetricsGrid.Visibility = Visibility.Visible;
                
                // Filtrar chamados do técnico atual (comparar por nome ou email)
                var meusChamados = chamados.Where(c => 
                    c.TecnicoNome == App.CurrentUserName || 
                    c.TecnicoNome == App.CurrentUserEmail
                ).ToList();
                
                var meusResolvidos = meusChamados.Count(c => c.Status == "Resolvido");
                var meusEmAndamento = meusChamados.Count(c => c.Status == "Em Atendimento");
                
                // Taxa de resolução
                var taxaResolucao = meusChamados.Count > 0 
                    ? (meusResolvidos * 100.0 / meusChamados.Count) 
                    : 0;
                TaxaResolucaoText.Text = taxaResolucao.ToString("F1");
                
                // Meus chamados (badges)
                MeusResolvidosText.Text = meusResolvidos.ToString();
                MeusEmAndamentoText.Text = meusEmAndamento.ToString();
                
                // Nota de satisfação (buscar do dashboard stats)
                try
                {
                    var stats = await App.ApiService.GetDashboardStatsAsync();
                    if (stats != null && stats.TotalAvaliacoes > 0)
                    {
                        NotaSatisfacaoText.Text = stats.NotaMediaSatisfacao.ToString("F2");
                        TotalAvaliacoesText.Text = $"{stats.TotalAvaliacoes} avaliações recebidas";
                    }
                    else
                    {
                        NotaSatisfacaoText.Text = "--";
                        TotalAvaliacoesText.Text = "Nenhuma avaliação ainda";
                    }
                }
                catch
                {
                    NotaSatisfacaoText.Text = "--";
                    TotalAvaliacoesText.Text = "Dados não disponíveis";
                }
            }

            // Top Usuários por abertura de chamados
            if (chamados != null)
            {
                var topUsuarios = chamados
                    .GroupBy(c => c.UsuarioEmail)
                    .Select(g => new TopUsuarioViewModel
                    {
                        Email = g.Key ?? "Desconhecido",
                        Total = g.Count()
                    })
                    .OrderByDescending(u => u.Total)
                    .Take(5)
                    .ToList();

                if (topUsuarios.Any())
                {
                    TopUsuariosItemsControl.ItemsSource = topUsuarios;
                    NenhumUsuarioText.Visibility = Visibility.Collapsed;
                }
                else
                {
                    NenhumUsuarioText.Visibility = Visibility.Visible;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao carregar dashboard: {ex.Message}", "Erro", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task InicializarSignalR()
    {
        try
        {
            Console.WriteLine("[Dashboard] Inicializando SignalR...");
            var apiBaseUrl = App.ApiService.GetBaseUrl().Replace("/api", "");
            Console.WriteLine($"[Dashboard] URL do Hub: {apiBaseUrl}/supportHub");
            
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{apiBaseUrl}/supportHub", options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(App.CurrentUserToken);
                })
                .WithAutomaticReconnect()
                .Build();

            // Event: Novo chamado na fila - RECARREGAR do banco
            _hubConnection.On<dynamic>("NovoUsuarioNaFila", async (chamado) =>
            {
                Console.WriteLine($"[Dashboard] SignalR: NovoUsuarioNaFila recebido!");
                await Dispatcher.InvokeAsync(async () =>
                {
                    try
                    {
                        Console.WriteLine($"[Dashboard] Recarregando chamados do banco...");
                        // Recarregar todos os chamados do banco
                        var chamadosAtualizados = await App.ApiService.GetChamadosAsync();
                        
                        // Contar chamados aguardando atendente
                        var aguardando = chamadosAtualizados?.Count(c => 
                            c.Status == "Aguardando Atendente") ?? 0;
                        
                        Console.WriteLine($"[Dashboard] Chamados aguardando: {aguardando}");
                        ClientesFilaText.Text = aguardando.ToString();
                        
                        // Mostrar notificação visual
                        MessageBox.Show($"✅ Novo usuário na fila!\n\nChamado: {chamado.Titulo}\nUsuário: {chamado.Usuario}", 
                            "Nova Solicitação", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Dashboard] Erro ao recarregar: {ex.Message}");
                        // Fallback: incrementar
                        if (int.TryParse(ClientesFilaText.Text, out int currentCount))
                        {
                            ClientesFilaText.Text = (currentCount + 1).ToString();
                        }
                    }
                });
            });

            // Event: Chamado assumido - RECARREGAR do banco
            _hubConnection.On<int>("ChamadoAssumido", async (chamadoId) =>
            {
                Console.WriteLine($"[Dashboard] SignalR: ChamadoAssumido #{chamadoId}");
                await Dispatcher.InvokeAsync(async () =>
                {
                    try
                    {
                        // Recarregar estatísticas
                        await CarregarDashboard();
                    }
                    catch { }
                });
            });

            await _hubConnection.StartAsync();
            Console.WriteLine("[Dashboard] SignalR conectado!");
            
            await _hubConnection.InvokeAsync("JoinTechnicianGroup");
            Console.WriteLine("[Dashboard] Entrou no grupo 'Tecnicos'");
        }
        catch (Exception ex)
        {
            // SignalR falhou, mas não é crítico para o dashboard
            Console.WriteLine($"[Dashboard] ERRO SignalR: {ex.Message}");
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

    private void ChamadosButton_Click(object sender, RoutedEventArgs e)
    {
        var chamadosWindow = new ChamadosWindow();
        chamadosWindow.Show();
        this.Close();
    }

    private async void VerFilaButton_Click(object sender, RoutedEventArgs e)
    {
        // Abrir janela de Fila de Atendimento
        var filaWindow = new FilaAtendimentoWindow();
        filaWindow.Show();
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

public class TopUsuarioViewModel
{
    public string Email { get; set; } = string.Empty;
    public int Total { get; set; }
}
