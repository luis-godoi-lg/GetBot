using GestaoChamados.Mobile.Helpers;
using GestaoChamados.Shared.Services;
using GestaoChamados.Mobile.Services;

namespace GestaoChamados.Mobile.Views;

public partial class DashboardPage : ContentPage
{
    private readonly ApiService _apiService;
    private System.Timers.Timer? _refreshTimer;

    public DashboardPage()
    {
        InitializeComponent();
        _apiService = new ApiService(Settings.ApiBaseUrl) { Token = Settings.Token };
        
        // User info
        UserEmailLabel.Text = Settings.UserEmail ?? "Usuário";
        UserRoleLabel.Text = GetRoleDisplay(Settings.UserRole);
        
        // Ocultar métricas de técnico se for usuário comum
        if (Settings.UserRole == "Usuario")
        {
            TecnicoMetricsContainer.IsVisible = false;
            VerFilaButton.IsVisible = false;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CarregarDashboard();
        IniciarRefreshAutomatico();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _refreshTimer?.Stop();
        _refreshTimer?.Dispose();
    }

    private string GetRoleDisplay(string? role)
    {
        return role switch
        {
            "Tecnico" => "Técnico",
            "Gerente" => "Gerente",
            "Admin" => "Administrador",
            _ => "Usuário"
        };
    }

    private void IniciarRefreshAutomatico()
    {
        _refreshTimer = new System.Timers.Timer(10000); // Atualiza a cada 10 segundos
        _refreshTimer.Elapsed += async (s, e) => await MainThread.InvokeOnMainThreadAsync(async () => await CarregarDashboard());
        _refreshTimer.AutoReset = true;
        _refreshTimer.Start();
    }

    private async Task CarregarDashboard()
    {
        try
        {
            // Usar endpoint correto: /api/dashboard (retorna DashboardDataDto com métricas específicas do técnico)
            var dashboardData = await _apiService.GetDashboardDataAsync();

            if (dashboardData == null) return;

            // Atualiza UI na thread principal
            MainThread.BeginInvokeOnMainThread(() =>
            {
                TotalChamadosLabel.Text = dashboardData.TotalChamados.ToString();
                ClientesFilaLabel.Text = dashboardData.ChamadosNaFila.ToString();
                EmAtendimentoLabel.Text = dashboardData.ChamadosEmAtendimento.ToString();
                ResolvidosLabel.Text = dashboardData.ChamadosResolvidos.ToString();

                // Legenda
                AbertosLegendaLabel.Text = dashboardData.ChamadosAbertos.ToString();
                EmAtendimentoLegendaLabel.Text = dashboardData.ChamadosEmAtendimento.ToString();
                ResolvidosLegendaLabel.Text = dashboardData.ChamadosResolvidos.ToString();
            });

            // Métricas de Técnico (dados já vêm da API específicos para o técnico logado)
            var isTecnico = Settings.UserRole == "Tecnico" || 
                           Settings.UserRole == "Gerente" || 
                           Settings.UserRole == "Admin";

            if (isTecnico)
            {
                // Taxa de resolução (já calculada pela API)
                var taxaResolucao = dashboardData.PercentualResolvidos;

                // Calcular meus resolvidos e em andamento pelos labels da API
                var meusResolvidos = 0;
                var meusEmAndamento = 0;

                if (dashboardData.MeuStatusLabels != null && dashboardData.MeuStatusCounts != null)
                {
                    for (int i = 0; i < dashboardData.MeuStatusLabels.Count; i++)
                    {
                        var status = dashboardData.MeuStatusLabels[i];
                        var count = dashboardData.MeuStatusCounts[i];

                        if (status == "Resolvido")
                            meusResolvidos = count;
                        else if (status == "Em Atendimento")
                            meusEmAndamento = count;
                    }
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    TaxaResolucaoLabel.Text = taxaResolucao.ToString("F1");
                    MeusResolvidosLabel.Text = meusResolvidos.ToString();
                    MeusEmAndamentoLabel.Text = meusEmAndamento.ToString();

                    // Nota de satisfação (já calculada pela API apenas para chamados do técnico)
                    if (dashboardData.TotalAvaliacoes > 0)
                    {
                        NotaSatisfacaoLabel.Text = dashboardData.NotaMediaSatisfacao.ToString("F1") + " ⭐";
                        TotalAvaliacoesLabel.Text = $"{dashboardData.TotalAvaliacoes} avaliações recebidas";
                    }
                    else
                    {
                        NotaSatisfacaoLabel.Text = "--";
                        TotalAvaliacoesLabel.Text = "Nenhuma avaliação ainda";
                    }
                });
            }
        }
        catch (Exception ex)
        {
            await CustomAlertService.ShowErrorAsync($"Erro ao carregar dashboard: {ex.Message}");
        }
    }

    private async void OnVerFilaClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new FilaAtendimentoView());
    }

    private async void OnVerChamadosClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//ChamadosPage");
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        bool answer = await CustomAlertService.ShowQuestionAsync("Deseja realmente sair?", "Confirmar");
        if (answer)
        {
            Settings.ClearAll();
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }
}
